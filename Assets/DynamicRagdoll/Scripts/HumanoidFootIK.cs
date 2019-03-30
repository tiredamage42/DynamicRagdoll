using UnityEngine;

namespace FootIK
{
	public class HumanoidFootIK : MonoBehaviour
	{	
		public LayerMask layerMask;
		[Range(4f, 20f)] public float raycastLength = 5f; // Character must not be higher above ground than this.
		[Range(.2f, .9f)] public float maxStepHeight = .5f;
		[Range(0f, 1f)] public float footIKWeight = 1f;
		[Range(1f, 100f)] public float footNormalLerp = 40f; // Lerp smoothing of foot normals
		[Range(1f, 100f)] public float footTargetLerp = 40f; // Lerp smoothing of foot position
		[Range(0f, 1f)] public float maxIncline = .8f; // Foot IK not aktiv on inclines steeper than arccos(maxIncline);

		Transform leftToe, leftFoot, leftCalf, leftThigh, rightToe, rightFoot, rightCalf, rightThigh;
		Vector3 lastLeftFootTargetPos, lastLeftFootTargetNormal, lastRightFootTargetPos, lastRightFootTargetNormal;
		float lastLeftY, lastRightY;
		float footHeight, thighLength, calfLength, reciDenominator;
		

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
			Ray ray = new Ray(footPosition + Vector3.up * maxStepHeight, Vector3.down);
			Debug.DrawRay(ray.origin, Vector3.down * raycastLength, Color.green);
			
			if (!Physics.Raycast(ray, out hit, raycastLength, layerMask))
			{
				hit.normal = Vector3.up;
				hit.point = foot.position - raycastLength * Vector3.up;
			}

			Vector3 footForward = toe.position - foot.position;
			footForward = new Vector3(footForward.x, 0f, footForward.z);

			footForward = Quaternion.FromToRotation(Vector3.up, hit.normal) * footForward;
			
			RaycastHit toeHit;
			if (Physics.Raycast(footPosition + footForward + Vector3.up * maxStepHeight, Vector3.down, out toeHit, maxStepHeight * 2f, layerMask))
			{	
				if(hit.point.y < toeHit.point.y - footForward.y)
					hit.point = new Vector3(hit.point.x, toeHit.point.y - footForward.y, hit.point.z);
				
				// Put avgNormal in foot normal
				hit.normal = (hit.normal + toeHit.normal).normalized;
			}
			// Do not tilt feet if on to steep an angle
			if (hit.normal.y < maxIncline)
			{
				hit.normal = Vector3.RotateTowards(Vector3.up, hit.normal, Mathf.Acos(maxIncline), 0f);
			}
		}

		void ShootIKRays(float deltaTime, out RaycastHit hitRight, out RaycastHit hitLeft)
		{		
			ShootIKRays(lastRightY, rightFoot, rightToe, out hitRight);
			ShootIKRays(lastLeftY, leftFoot, leftToe, out hitLeft);
		}

		void PositionFoot (Transform foot, Transform lowerLeg, Transform upperLeg, Transform toe, float footNormalSpeed, float footTargetSpeed, RaycastHit hit, float deltaTime, ref Vector3 lastFootTargetNormal, ref Vector3 lastFootTargetPos) {
			
			// Save before PositionFeet
			Quaternion footRotation = foot.rotation;
			float leftFootElevationInAnim = Vector3.Dot(foot.position - transform.position, transform.up) - footHeight;
			
			Vector3 footTargetNormal = Vector3.Lerp(Vector3.up, hit.normal, footIKWeight);
			footTargetNormal = Vector3.Lerp(lastFootTargetNormal, footTargetNormal, footNormalSpeed * deltaTime);
			lastFootTargetNormal = footTargetNormal;
			
			Vector3 footTargetPos = hit.point;
			footTargetPos = Vector3.Lerp(lastFootTargetPos, footTargetPos, footTargetSpeed * deltaTime);
			lastFootTargetPos = footTargetPos;
			
			footTargetPos = Vector3.Lerp(foot.position, footTargetPos + footTargetNormal * footHeight + leftFootElevationInAnim * Vector3.up, footIKWeight);
			
			float leftLegTargetLength = Mathf.Min((footTargetPos - upperLeg.position).magnitude, calfLength + thighLength - .01f);
			leftLegTargetLength = Mathf.Max(leftLegTargetLength, .2f);
			float leftKneeAngle = Mathf.Acos((Mathf.Pow(leftLegTargetLength, 2f) - (calfLength * calfLength) - (thighLength * thighLength)) * reciDenominator);
			leftKneeAngle *= Mathf.Rad2Deg;
			float currKneeAngle;
			Vector3 currKneeAxis;
			Quaternion currKneeRotation = Quaternion.FromToRotation(lowerLeg.position - upperLeg.position, foot.position - lowerLeg.position);
			currKneeRotation.ToAngleAxis(out currKneeAngle, out currKneeAxis);
			if (currKneeAngle > 180f)
			{
				currKneeAngle = 360f - currKneeAngle;
				currKneeAxis *= -1f;
			}
			lowerLeg.Rotate(currKneeAxis, 180f - leftKneeAngle - currKneeAngle, Space.World);
			upperLeg.rotation = Quaternion.FromToRotation(foot.position - upperLeg.position, footTargetPos - upperLeg.position) * upperLeg.rotation;
			foot.rotation = Quaternion.FromToRotation(transform.up, footTargetNormal) * footRotation;
		}

		void PositionFeet(float deltaTime, RaycastHit hitRight, RaycastHit hitLeft)
		{
			PositionFoot(rightFoot, rightCalf, rightThigh, rightToe, footNormalLerp, footTargetLerp, hitRight, deltaTime, ref lastRightFootTargetNormal, ref lastRightFootTargetPos);
			PositionFoot(leftFoot, leftCalf, leftThigh, leftToe, footNormalLerp, footTargetLerp, hitLeft, deltaTime, ref lastLeftFootTargetNormal, ref lastLeftFootTargetPos);
			
			lastLeftY = leftFoot.position.y; 
			lastRightY = rightFoot.position.y;			
		}
	}
}