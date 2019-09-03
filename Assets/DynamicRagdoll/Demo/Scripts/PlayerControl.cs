using UnityEngine;
using System.Collections;

using Game.Combat;

namespace DynamicRagdoll.Demo {
    public class PlayerControl : MonoBehaviour, IDamager {


        /*
			switch camera to follow ragdoll	or animated hips based on whichever model is active
		*/
		void CheckCameraTarget () {

            if (!controlledCharacter) {
                camFollow.target = null;
                return;
            }

            //switch camera to follow ragdoll (or animated hips)
            if (ragdollController.ragdollRenderersEnabled == cameraTargetIsAnimatedHips) {

                cameraTargetIsAnimatedHips = !cameraTargetIsAnimatedHips;

                RagdollTransform hipBone = ragdollController.ragdoll.RootBone();
                camFollow.target = cameraTargetIsAnimatedHips ? hipBone.followTarget.transform : hipBone.transform;
                camFollow.updateMode = cameraTargetIsAnimatedHips ? UpdateMode.Update : UpdateMode.FixedUpdate;
            }
		}




        public CharacterMovement controlledCharacter;
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
		
        // Shooting shooting;
        CameraHandler camFollow;
        Camera cam;
        CannonBall cannonBall;

        void Awake () {
            // shooting = GetComponent<Shooting>();
            camFollow = GetComponent<CameraHandler>();
            cam = GetComponent<Camera>();

            cannonBall = GameObject.FindObjectOfType<CannonBall>();			

            origFixedDelta = Time.fixedDeltaTime;
            Cursor.visible = false;
        }

        void Start () {
            AttachToCharacter(controlledCharacter);
        }

        RagdollController ragdollController;

        void AttachToCharacter (CharacterMovement character) {
            if (controlledCharacter != null) {
                //enable ai for the last controlled character
                controlledCharacter.GetComponent<AIControl>().enabled = true;    
            }

            controlledCharacter = character;

            if (controlledCharacter != null) {
                //disable ai for our new controlled character
                controlledCharacter.GetComponent<AIControl>().enabled = false;

                ragdollController = controlledCharacter.GetComponent<RagdollController>();


                RagdollTransform hipBone = ragdollController.ragdoll.RootBone();
                camFollow.target = hipBone.followTarget.transform;
                camFollow.updateMode = UpdateMode.Update;
            }
        }
        
        void Update() {
            UpdateRagdollGrabberPosition();

            CheckCameraTarget();
            UpdateSloMo();

            /* shoot from the clicked position */
            if (Input.GetMouseButtonDown(0)) {
                Shoot(cam.ScreenPointToRay(Input.mousePosition));
			}
            if (Input.GetMouseButtonDown(1)) {
                StartCoroutine(CheckForRagdollGrab(cam.ScreenPointToRay(Input.mousePosition)));
            }

            /* launch the ball from the camera */
            if (Input.GetKeyDown(KeyCode.B)) {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                cannonBall.Launch(ray.origin, ray.origin + ray.direction * 50, ballScale, ballMass, ballVelocity);
            }

            if (controlledCharacter) {
                
                
                /* drop teh ball on the controlled character */
                if (Input.GetKeyDown(KeyCode.U)) {
                    Vector3 ragRootBonePosition = ragdollController.ragdoll.RootBone().transform.position;
                    cannonBall.Launch(ragRootBonePosition + Vector3.up * 25, ragRootBonePosition, ballScale, ballMass, 0);
                }
                
                /* manually ragdoll the controlled character */
                if (Input.GetKeyDown(KeyCode.R)) {
                    ragdollController.GoRagdoll("manual");
                }

                /* moved the controlled character */
                if (!controlledCharacter.disableExternalMovement)
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
                /* look for character to control */
                // if (Input.GetMouseButtonDown(1)) {
                if (Input.GetKeyDown(KeyCode.P)) {
                
                    StartCoroutine(CheckForCharacter(cam.ScreenPointToRay(Input.mousePosition)));
                }
            }
        }

        RagdollBone grabbedBone;

        Rigidbody _ragdollGrabberAnchor;
        Rigidbody ragdollGrabberAnchor {
            get {
                if (_ragdollGrabberAnchor == null) {
                    _ragdollGrabberAnchor = new GameObject("ragdollGrabber").AddComponent<Rigidbody>();
                    _ragdollGrabberAnchor.isKinematic = true;
                }
                return _ragdollGrabberAnchor;
            }
        }


        
        void UpdateRagdollGrabberPosition () {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            ragdollGrabberAnchor.transform.position = ray.origin + ray.direction * 5;
        }




        IEnumerator CheckForRagdollGrab (Ray ray){
            if (grabbedBone) {
                if (grabbedBone.ragdoll.hasController) {
                    grabbedBone.ragdoll.controller.disableGetUp = false;
                }
                
                RagdollPhysics.DetachRigidbody(grabbedBone.GetComponent<Rigidbody>());
                grabbedBone = null;
            }
            else {
                yield return new WaitForFixedUpdate();

                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 100f, shootMask, QueryTriggerInteraction.Ignore))
                {
                    grabbedBone = hit.transform.GetComponent<RagdollBone>();
                    if (grabbedBone) {

                        if (grabbedBone.ragdoll.hasController) {
                            if (grabbedBone.ragdoll.controller == ragdollController) {
                                yield break;    
                            }
                            grabbedBone.ragdoll.controller.GoRagdoll("from grab");
                            grabbedBone.ragdoll.controller.disableGetUp = true;
                        }
                        RagdollPhysics.HangRigidbody(grabbedBone.GetComponent<Rigidbody>(), ragdollGrabberAnchor);

                    }				
                }
                

            }
        }
        IEnumerator CheckForCharacter (Ray ray){
			yield return new WaitForFixedUpdate();

            RaycastHit hit;
			
			if (Physics.Raycast(ray, out hit, 100f, shootMask, QueryTriggerInteraction.Ignore))
            {
				Damageable damageable = hit.transform.GetComponent<Damageable>();
				if (damageable) {
                    AttachToCharacter(damageable.damageableRoot.GetComponent<CharacterMovement>());
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

        public LayerMask shootMask;
        public float bulletForce = 25f;

		// needed for slo motion or forces are too small
		public float modifiedBulletForce { get { return bulletForce / Time.timeScale; } }

        public void Shoot (Ray ray) {
            StartCoroutine(ShootBullet(ray));
        }

		IEnumerator ShootBullet (Ray ray){
			yield return new WaitForFixedUpdate();
			
			RaycastHit hit;
			
			if (Physics.Raycast(ray, out hit, 100f, shootMask, QueryTriggerInteraction.Ignore))
            {
				Damageable damageable = hit.transform.GetComponent<Damageable>();
				if (damageable) {
					damageable.SendDamage(new DamageMessage(this, 50f));
				}
				
				Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
				if (rb) {
					rb.AddForceAtPosition(ray.direction.normalized * modifiedBulletForce, hit.point, ForceMode.VelocityChange);
				}
			}
		}


        public void DamageDealtCallback (Actor actor, float damageDone, float newHealth) {
            damageShowTime = Time.time;
            damageShowAmount = damageDone;
        }
        public void DamageDeathCallback (Actor actor) {
            xpShowTime = Time.time;
        }


        /*
			GUI STUFF
		*/
		void OnGUI () {
			DrawCrosshair(Input.mousePosition);
            DrawDamageCounter(Input.mousePosition);
            DrawXPCounter();
			DrawTutorialBox();
		}

		void DrawTutorialBox () {
			GUI.Box(new Rect(5, 5, 200, 140), "Left Mouse = Shoot\nB = Launch ball\nU = Drop ball from above\nRight Mouse = Grab Ragdoll\nP = FreeCam / Character toggle\nN = Slow motion\nR = Go Ragdoll\nMove With Arrow Keys\nor WASD");
		}

        const float xpShowDuration = 2;
        float xpShowTime;
        void DrawXPCounter () {
            if (Time.time - xpShowTime <= xpShowDuration) {
    			GUI.Box(new Rect(5, Screen.height * .5f, 100, 32), "XP +10");
            }
        }

        const float damageShowDuration = 1f;

        float damageShowTime;
        float damageShowAmount;
        void DrawDamageCounter(Vector2 mousePos) {
            if (Time.time - damageShowTime <= damageShowDuration) {

                Rect crosshairRect = new Rect(mousePos.x, (Screen.height - mousePos.y) - crossHairSize, 100, 32);
    			GUI.Box(crosshairRect, "-"+damageShowAmount);
            }
        }

        const float crossHairSize = 40;
		void DrawCrosshair (Vector2 mousePos) {
			Rect crosshairRect = new Rect(mousePos.x - crossHairSize * .5f, (Screen.height - mousePos.y) - crossHairSize * .5f, crossHairSize, crossHairSize);
			GUI.DrawTexture(crosshairRect, crosshairTexture, ScaleMode.ScaleToFit, true);
		}
    }
}