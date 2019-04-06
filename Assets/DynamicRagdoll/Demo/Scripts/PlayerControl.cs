using UnityEngine;
using System.Collections;

namespace DynamicRagdoll.Demo {
    public class PlayerControl : MonoBehaviour {
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
        CameraHandler camFollow;
        Camera cam;
        CannonBall cannonBall;

        void Awake () {
            shooting = GetComponent<Shooting>();
            camFollow = GetComponent<CameraHandler>();
            cam = GetComponent<Camera>();

            cannonBall = GameObject.FindObjectOfType<CannonBall>();			

            origFixedDelta = Time.fixedDeltaTime;
            Cursor.visible = false;

        }
        void Start () {

            AttachToCharacter(controlledCharacter);
        }

        void AttachToCharacter (Character character) {
            if (controlledCharacter != null) {
                //enable ai for the last controlled character
                controlledCharacter.GetComponent<AIControl>().enabled = true;    
            }

            controlledCharacter = character;

            if (controlledCharacter != null) {
                //disable ai for our new controlled character
                controlledCharacter.GetComponent<AIControl>().enabled = false;

                Ragdoll.Bone hipBone = controlledCharacter.ragdollController.ragdoll.RootBone();
                camFollow.target = hipBone.followTarget.transform;
                camFollow.updateMode = UpdateMode.Update;
            }
        }
        
        void Update() {

            CheckCameraTarget();

            UpdateSloMo();

            /*
                shoot from the clicked position
            */
            if (Input.GetMouseButtonDown(0))
			{
                shooting.Shoot(cam.ScreenPointToRay(Input.mousePosition));
			}
            /*
                launch the ball from the camera
            */
            if (Input.GetKeyDown(KeyCode.B)) 
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                cannonBall.Launch(ray.origin, ray.origin + ray.direction * 50, ballScale, ballMass, ballVelocity);
            }

            if (controlledCharacter) {

                RagdollController ragdollController = controlledCharacter.ragdollController;

                Vector3 ragRootBonePosition = ragdollController.ragdoll.RootBone().transform.position;


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
                if (!controlledCharacter.overrideControl)
                {
                    //do turning
                    controlledCharacter.transform.Rotate(0f, Input.GetAxis("Horizontal") * turnSpeed * Time.deltaTime, 0f);
                    
                    //set speed
                    controlledCharacter.SetMovementSpeed(Input.GetAxis("Vertical") * (Input.GetKey(KeyCode.LeftShift) ? 2 : 1));
                }

                //disable char control
                if (Input.GetKeyDown(KeyCode.P)) {

                    AttachToCharacter(null);
                }
            }
            else {
                /*
                    look for character to control
                */

                if (Input.GetMouseButtonDown(1))
                {
                    StartCoroutine(CheckForCharacter(cam.ScreenPointToRay(Input.mousePosition)));
                }
                
            }
        }
        IEnumerator CheckForCharacter (Ray ray){
			yield return new WaitForFixedUpdate();
			
			RaycastHit hit;
			
			if (Physics.Raycast(ray, out hit, 100f, shooting.shootMask, QueryTriggerInteraction.Ignore))
            {
				//check if we hit a ragdoll bone
				RagdollBone ragdollBone = hit.transform.GetComponent<RagdollBone>();
				
                if (ragdollBone) {
					// check if the ragdoll has a controller
					if (ragdollBone.ragdoll.hasController) {
						AttachToCharacter(ragdollBone.ragdoll.controller.GetComponent<Character>());
					}
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
		
            //switch camera to follow ragdoll (or animated hips)
            if (ragdollController.ragdollRenderersEnabled == cameraTargetIsAnimatedHips) {

                cameraTargetIsAnimatedHips = !cameraTargetIsAnimatedHips;

                Ragdoll.Bone hipBone = ragdollController.ragdoll.RootBone();
                camFollow.target = cameraTargetIsAnimatedHips ? hipBone.followTarget.transform : hipBone.transform;
                camFollow.updateMode = cameraTargetIsAnimatedHips ? UpdateMode.Update : UpdateMode.FixedUpdate;
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
			GUI.Box(new Rect(5, 5, 200, 140), "Left Mouse = Shoot\nB = Launch ball\nU = Drop ball from above\nRight Mouse = Control character\nP = Detach character\nN = Slow motion\nR = Go Ragdoll\nMove With Arrow Keys\nor WASD");
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