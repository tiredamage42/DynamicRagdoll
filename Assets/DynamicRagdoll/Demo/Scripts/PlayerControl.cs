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
        
		bool cameraTargetIsAnimatedHips;
		
        CameraHandler camFollow;
        Camera cam;
        
        void Awake () {
            camFollow = GetComponent<CameraHandler>();
            cam = GetComponent<Camera>();

        }

        void Start () {
            Cursor.visible = false;
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

        public AmmoType[] ammoTypes;
        public int ammotypeIndex;

        void ToggleAmmoType () {
            ammotypeIndex ++;
            if (ammotypeIndex >= ammoTypes.Length) {
                ammotypeIndex = 0;
            }

        }
        AmmoType currentAmmoType {
            get {
                if (ammotypeIndex >= 0 && ammotypeIndex < ammoTypes.Length) {
                    return ammoTypes[ammotypeIndex];
                }
                return null;
            }
        }

        
        void Update() {
            UpdateRagdollGrabberPosition();

            CheckCameraTarget();
            UpdateSloMo();

            /* shoot from the clicked position */
            if (Input.GetMouseButtonDown(0)) {
                if (currentAmmoType != null) {
                    currentAmmoType.FireAmmo(this, cam.ScreenPointToRay(Input.mousePosition), shootMask, 1);
                }
			}
            if (Input.GetMouseButtonDown(1)) {
                StartCoroutine(CheckForRagdollGrab(cam.ScreenPointToRay(Input.mousePosition)));
            }

            /* launch the ball from the camera */
            if (Input.GetKeyDown(KeyCode.B)) {
                GameObject.FindObjectOfType<DemoSceneController>().SwitchActiveScene();
            }
            /* drop teh ball on the controlled character */
            if (Input.GetKeyDown(KeyCode.E)) {
                ToggleAmmoType();
            }

            if (controlledCharacter) {
                
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
                if (Input.GetKeyDown(KeyCode.P)) {
                
                    StartCoroutine(CheckForCharacter(cam.ScreenPointToRay(Input.mousePosition)));
                }
            }
        }

        RagdollBone grabbedBone;

        GrabPoint _ragdollGrabberAnchor;
        GrabPoint ragdollGrabberAnchor {
            get {
                if (_ragdollGrabberAnchor == null) {
                    _ragdollGrabberAnchor = GrabPoint.GetGrabPoint();
                    
                    GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    Destroy(sphere.GetComponent<Collider>());
                    sphere.transform.localScale = Vector3.one * .2f;
                    sphere.transform.SetParent(_ragdollGrabberAnchor.transform);
                    sphere.transform.localPosition = Vector3.zero;
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
                RagdollPhysics.DetachRigidbody(grabbedBone.GetComponent<Rigidbody>(), hangJoint, false);
                grabbedBone = null;
            }
            else {
                yield return new WaitForFixedUpdate();

                RaycastHit hit;
                
                if (Physics.Raycast(ray, out hit, 100f, shootMask, QueryTriggerInteraction.Ignore))
                {
                    grabbedBone = hit.transform.GetComponent<RagdollBone>();
                    if (grabbedBone) {
                        ragdollGrabberAnchor.transform.position = hit.point;
                        hangJoint = RagdollPhysics.GrabRigidbody(grabbedBone.rb, ragdollGrabberAnchor.childRigidbody, true);
                    }				
                }
            }
        }

        Joint hangJoint;
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
                if (GameTime.timeDilated) {
                    GameTime.ResetTimeDilation(.1f);
                }
                else {
                    GameTime.SetTimeDilation(slowTime, .1f, -1, 0);
                }
				// Time.timeScale = slomo ? 1 : slowTime;
				// Time.fixedDeltaTime = origFixedDelta * Time.timeScale;
				// slomo = !slomo;
			}
		}

        public LayerMask shootMask;
        
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
			// GUI.Box(new Rect(5, 5, 200, 140), "Left Mouse = Shoot\nB = Launch ball\nU = Drop ball from above\nRight Mouse = Grab Ragdoll\nP = FreeCam / Character toggle\nN = Slow motion\nR = Go Ragdoll\nMove With Arrow Keys\nor WASD");
            
            string currentAmmoTypeName = currentAmmoType != null ? currentAmmoType.name : "None";
            GUI.Box(new Rect(5, 5, 200, 140), "Left Mouse = Shoot\nE = Toggle Ammo (Current: " + currentAmmoTypeName + "\nRight Mouse = Grab Ragdoll\nB = Toggle Active Scene\nP = FreeCam / Character toggle\nN = Slow motion\nR = Go Ragdoll\nMove With Arrow Keys\nor WASD");
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