using UnityEngine;

namespace DynamicRagdoll.Demo {

    public class PlayerControl : MonoBehaviour
    {
        public Character controlledCharacter;

        public float turnSpeed = 500f;
		
        [Header("Slow Time")]
        public float slowTime = .3f;

        [Header("Shooting")]
        public Texture crosshairTexture;
        
        [Header("Cannon Ball")]
        public float ballVelocity = 20f;
		public float ballMass = 40f;
		public float ballScale = .4f;
		


        float origFixedDelta;
        bool slomo, cameraTargetIsAnimatedHips;
		
		
        Shooting shooting;
        CameraFollow camFollow;
        Camera cam;
        CannonBall cannonBall;

        void Awake () {
            shooting = GetComponent<Shooting>();
            camFollow = GetComponent<CameraFollow>();
            cam = GetComponent<Camera>();

            cannonBall = GameObject.FindObjectOfType<CannonBall>();			

            origFixedDelta = Time.fixedDeltaTime;
            Cursor.visible = false;
        }


        // Update is called once per frame
        void Update()
        {
            CheckCameraTarget();

            UpdateSloMo();

            /*
                shoot from the clicked position
            */
            if (Input.GetMouseButtonDown(0))
			{
                shooting.Shoot(cam.ScreenPointToRay(Input.mousePosition));
			}

            if (controlledCharacter) {

                RagdollController ragdollController = controlledCharacter.ragdollController;

                Vector3 ragRootBonePosition = ragdollController.ragdoll.RootBone().position;

                /*
                    launch the ball from the camera to the controleld character
                */
                if (Input.GetKeyDown(KeyCode.B)) 
                {
                    cannonBall.Launch(camFollow.transform.position, ragRootBonePosition, ballScale, ballMass, ballVelocity);
                }

                /*
                    drop teh ball on the controlled character
                */
                if (Input.GetKeyDown(KeyCode.U)) 
                {
                    cannonBall.Launch(ragRootBonePosition + Vector3.up * 25, ragRootBonePosition, ballScale, ballMass, 0);
                }
                
                /*
                    manually ragdoll the controlled character 
                */
                if (Input.GetKeyDown(KeyCode.R)) {
                    ragdollController.GoRagdoll();
                }


                /*
                    moved the controlled character 
                */
                if (ragdollController.state == RagdollControllerState.Animated && !ragdollController.isGettingUp) {
				
                    //do turning
                    controlledCharacter.transform.Rotate(0f, Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime, 0f);
                    
                    //set speed
                    controlledCharacter.SetMovementSpeed(Input.GetAxis("Vertical") * (Input.GetKey(KeyCode.LeftShift) ? 2 : 1));
                }

            }


        }

        void UpdateSloMo () {
			if (Input.GetKeyDown(KeyCode.N)) {
				Time.timeScale = slomo ? 1 : slowTime;
				Time.fixedDeltaTime = origFixedDelta * Time.timeScale;
				slomo = !slomo;
			}
		}




        /*
			switch camera to follow ragdoll	or animated hips based on ragdoll state
		*/
		void CheckCameraTarget () {

            if (!controlledCharacter) {
                camFollow.target = null;
                return;
            }

            RagdollController ragdollController = controlledCharacter.ragdollController;


			if (ragdollController.ragdollRenderersEnabled) {
				if (cameraTargetIsAnimatedHips) {

					//switch camera to follow ragdoll	
					camFollow.target = ragdollController.ragdoll.RootBone().transform;
                    camFollow.updateMode = UpdateMode.FixedUpdate;
					
					cameraTargetIsAnimatedHips = false;
				}
			}
			else {
				if (!cameraTargetIsAnimatedHips) {
                    
					//switch cameara to follow our characetr
					camFollow.target = ragdollController.ragdoll.RootBone().followTarget.transform;
                    camFollow.updateMode = UpdateMode.Update;
					
					cameraTargetIsAnimatedHips = true;
				}
			}
		}

        /*
			GUI STUFF
		*/
		void OnGUI () {
			DrawCrosshair();
			DrawTutorialBox();
		}

		void DrawTutorialBox () {
			GUI.Box(new Rect(5, 5, 160, 120), "Fire = Left mouse\nB = Launch Ball\nN = Slow motion\nR = Go Ragdoll\nMove With Arrow Keys\nor WASD");
		}

		void DrawCrosshair () {
			float crossHairSize = 40;
			float halfSize = crossHairSize / 2;
			Vector2 mousePos = Input.mousePosition;
			Rect crosshairRect = new Rect(mousePos.x - halfSize, Screen.height - mousePos.y - halfSize, crossHairSize, crossHairSize);
			GUI.DrawTexture(crosshairRect, crosshairTexture, ScaleMode.ScaleToFit, true);
		}
    }
}