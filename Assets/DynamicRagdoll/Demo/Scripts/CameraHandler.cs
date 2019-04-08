using UnityEngine;

namespace DynamicRagdoll.Demo {

	/*
		a simple camera follow script
	*/
	public class CameraHandler : MonoBehaviour
	{
		public float freeMoveSpeed = 1.0f;
		public float freeMoveTurn = 1;
		public UpdateMode updateMode = UpdateMode.LateUpdate;

		public float moveSpeed = 1.5f;			
		public float turnSpeed = 7f;			
		public float distance = 4.0f;
		public Transform target;
		Vector3 startDir;
 
		void UpdateFreeCam () {

			float turnAxisY = 0;
			if (Input.GetKey(KeyCode.LeftArrow))
				turnAxisY -= 1;
			if (Input.GetKey(KeyCode.RightArrow))
				turnAxisY += 1;
			float turnAxisX = 0;
			if (Input.GetKey(KeyCode.UpArrow)) 
				turnAxisX += 1;
			if (Input.GetKey(KeyCode.DownArrow)) 
				turnAxisX -= 1;
			float depthAxis = 0;
			if (Input.GetKey(KeyCode.E))
				depthAxis -= 1;
			if (Input.GetKey(KeyCode.Q))
				depthAxis += 1;

			float moveAxisY = 0;
			if (Input.GetKey(KeyCode.S))
				moveAxisY -= 1;
			if (Input.GetKey(KeyCode.W))
				moveAxisY += 1;
			float moveAxisX = 0;
			if (Input.GetKey(KeyCode.A)) 
				moveAxisX -= 1;
			if (Input.GetKey(KeyCode.D)) 
				moveAxisX += 1;
			
			transform.position += (transform.right * moveAxisX + transform.forward * moveAxisY + transform.up * depthAxis) * freeMoveSpeed;
			transform.eulerAngles += new Vector3(turnAxisX, turnAxisY, 0) * freeMoveTurn;
		}
		
		void Awake ()
		{
			if (!target) {
				Debug.LogWarning("The lookAtTransform is not assigned on " + name); 
				return;
			}
			// Setting the relative position as the initial relative position of the camera in the scene.
			startDir = (-transform.position).normalized;	
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
			
			if (modeCheck != updateMode) {
				return;
			}
			if (!target) {	
				UpdateFreeCam();
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