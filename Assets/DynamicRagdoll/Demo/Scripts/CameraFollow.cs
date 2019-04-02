using UnityEngine;

namespace DynamicRagdoll.Demo {

	/*
		a simple camera follow script
	*/
	public class CameraFollow : MonoBehaviour
	{
		public enum UpdateMode { Update, FixedUpdate, LateUpdate };

		public UpdateMode updateMode = UpdateMode.LateUpdate;

		public float moveSpeed = 1.5f;			
		public float turnSpeed = 7f;			
		public float distance = 4.0f;
		public Transform target;
		Vector3 startDir;
		void Awake ()
		{
			if (!target) {
				Debug.LogWarning("The lookAtTransform is not assigned on " + name); 
				return;
			}
			// Setting the relative position as the initial relative position of the camera in the scene.
			startDir = (target.position - transform.position).normalized;	
		}
		
		void Update () 
		{
			UpdateLoop(UpdateMode.Update, Time.deltaTime);
		}
		void FixedUpdate () 
		{
			UpdateLoop(UpdateMode.FixedUpdate, Time.fixedDeltaTime);
		}
		void LateUpdate () 
		{
			UpdateLoop(UpdateMode.LateUpdate, Time.deltaTime);
		}

		void UpdateLoop (UpdateMode modeCheck, float deltaTime) {
			
			if (!target) {
				return;
			}
			if (modeCheck != updateMode) {
				return;
			}

			Vector3 camPos = transform.position;
			Quaternion camRot = transform.rotation;
			Vector3 targetPos = target.position;

			transform.position = Vector3.Lerp(camPos, targetPos - startDir * distance, moveSpeed * deltaTime);
			transform.rotation = Quaternion.Slerp(camRot, Quaternion.LookRotation(targetPos - camPos), turnSpeed * deltaTime);
		}		
	}
}