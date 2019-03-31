using UnityEngine;

namespace FootIK
{
	public class HumanoidFootIK : MonoBehaviour
	{	
		public LayerMask layerMask;
		[Range(4f, 20f)] public float raycastLength = 5f; // Character must not be higher above ground than this.
		[Range(.2f, .9f)] public float maxStepHeight = .5f;
		[Range(0f, 1f)] public float footIKWeight = 1f;
		[Range(1f, 100f)] public float ikTargetSpeed = 40f;
		[Range(0f, 1f)] public float maxIncline = .8f; // Foot IK not aktiv on inclines steeper than arccos(maxIncline);

		Transform leftToe, leftFoot, leftCalf, leftThigh, rightToe, rightFoot, rightCalf, rightThigh;
		Vector3 lastLeftFootTargetPos, lastLeftFootTargetNormal, lastRightFootTargetPos, lastRightFootTargetNormal;
		float lastLeftY, lastRightY, footHeight, thighLength, calfLength, reciDenominator, maxLegTargetLength;



		Vector3 up = Vector3.up;
		public void SetUpDirection(Vector3 up) {
			this.up = up;
		}

		void Awake()
		{
			Animator anim = GetComponent<Animator>();

			leftToe =   anim.GetBoneTransform(HumanBodyBones.LeftToes);
			leftFoot =  anim.GetBoneTransform(HumanBodyBones.LeftFoot);
			leftCalf =  anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
			leftThigh = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
			
			rightToe =   anim.GetBoneTransform(HumanBodyBones.RightToes);
			rightFoot =  anim.GetBoneTransform(HumanBodyBones.RightFoot);
			rightCalf =  anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
			rightThigh = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
			
			thighLength = (rightThigh.position - rightCalf.position).magnitude;
			calfLength = (rightCalf.position - rightFoot.position).magnitude;
			reciDenominator = -.5f / calfLength / thighLength;

			// Character should be spawned upright (line from feets to head points as vector3.up)
			footHeight = (rightFoot.position.y + leftFoot.position.y) * .5f - transform.position.y;

			maxLegTargetLength = calfLength + thighLength - .01f;
		}

		void LateUpdate()
		{
			UpdateLoop(Time.deltaTime);
		}

		void UpdateLoop(float deltaTime)
		{	
			RaycastHit rightHit, leftHit;
			ShootIKRays(deltaTime, out rightHit, out leftHit);
			PositionFeet(deltaTime, rightHit, leftHit);
		}

		void ShootIKRays (float lastY, Transform foot, Transform toe, out RaycastHit hit) {
			Vector3 footPosition = new Vector3(foot.position.x, lastY, foot.position.z);
			
			// Shoot ray to determine where the feet should be placed.
			Ray ray = new Ray(footPosition + up * maxStepHeight, -up);
			//Debug.DrawRay(ray.origin, ray.direction * raycastLength, Color.green);
			if (!Physics.Raycast(ray, out hit, raycastLength, layerMask))
			{
				hit.normal = up;
				hit.point = foot.position - up * raycastLength;
			}

			Vector3 footForward = toe.position - footPosition;
			footForward.y = 0;
			
			footForward = Quaternion.FromToRotation(up, hit.normal) * footForward;
			
			RaycastHit toeHit;
			if (Physics.Raycast(footPosition + footForward + up * maxStepHeight, -up, out toeHit, maxStepHeight * 2f, layerMask))
			{	

				if(hit.point.y < toeHit.point.y - footForward.y)
					hit.point = new Vector3(hit.point.x, toeHit.point.y - footForward.y, hit.point.z);
				
				// Put avgNormal in foot normal
				hit.normal = (hit.normal + toeHit.normal).normalized;
			}
			// Do not tilt feet if on to steep an angle
			if (hit.normal.y < maxIncline)
			{
				hit.normal = Vector3.RotateTowards(up, hit.normal, Mathf.Acos(maxIncline), 0f);
			}
		}

		void ShootIKRays(float deltaTime, out RaycastHit hitRight, out RaycastHit hitLeft)
		{		
			ShootIKRays(lastRightY, rightFoot, rightToe, out hitRight);
			ShootIKRays(lastLeftY, leftFoot, leftToe, out hitLeft);
		}

		void PositionFoot (Transform foot, Transform lowerLeg, Transform upperLeg, Transform toe, RaycastHit hit, float deltaTime, ref Vector3 lastFootTargetNormal, ref Vector3 lastFootTargetPos, ref float lastY) {
			
			// Save before PositionFeet
			Quaternion footRotation = foot.rotation;

			Vector3 upperLegPos = upperLeg.position;
			Vector3 lowerLegPos = lowerLeg.position;
			Vector3 footPos = foot.position;


			float leftFootElevationInAnim = Vector3.Dot(footPos - transform.position, up) - footHeight;
			
			Vector3 footTargetNormal = Vector3.Lerp(up, hit.normal, footIKWeight);
			footTargetNormal = Vector3.Lerp(lastFootTargetNormal, footTargetNormal, ikTargetSpeed * deltaTime);
			lastFootTargetNormal = footTargetNormal;
			
			Vector3 footTargetPos = hit.point;
			footTargetPos = Vector3.Lerp(lastFootTargetPos, footTargetPos, ikTargetSpeed * deltaTime);
			lastFootTargetPos = footTargetPos;
			
			footTargetPos = Vector3.Lerp(footPos, footTargetPos + footTargetNormal * footHeight + leftFootElevationInAnim * up, footIKWeight);
			
			float legTargLength = Mathf.Clamp((footTargetPos - upperLegPos).magnitude, .2f, maxLegTargetLength);
			float kneeAngle = Mathf.Acos(((legTargLength * legTargLength) - (calfLength * calfLength) - (thighLength * thighLength)) * reciDenominator) * Mathf.Rad2Deg;
			
			float currKneeAngle;
			Vector3 currKneeAxis;
			Quaternion currKneeRotation = Quaternion.FromToRotation(lowerLegPos - upperLegPos, footPos - lowerLegPos);
			currKneeRotation.ToAngleAxis(out currKneeAngle, out currKneeAxis);
			
			if (currKneeAngle > 180f)
			{
				currKneeAngle = 360f - currKneeAngle;
				currKneeAxis *= -1f;
			}
			
			lowerLeg.Rotate(currKneeAxis, 180f - kneeAngle - currKneeAngle, Space.World);

			upperLeg.rotation = Quaternion.FromToRotation(footPos - upperLegPos, footTargetPos - upperLegPos) * upperLeg.rotation;
			
			foot.rotation = Quaternion.FromToRotation(up, footTargetNormal) * footRotation;

			lastY = foot.position.y; 
		}

		void PositionFeet(float deltaTime, RaycastHit hitRight, RaycastHit hitLeft)
		{
			PositionFoot(rightFoot, rightCalf, rightThigh, rightToe, hitRight, deltaTime, ref lastRightFootTargetNormal, ref lastRightFootTargetPos, ref lastRightY);
			PositionFoot(leftFoot, leftCalf, leftThigh, leftToe, hitLeft, deltaTime, ref lastLeftFootTargetNormal, ref lastLeftFootTargetPos, ref lastLeftY);
		}
	}
}