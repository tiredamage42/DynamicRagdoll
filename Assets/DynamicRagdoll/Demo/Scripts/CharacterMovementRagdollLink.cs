using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DynamicRagdoll.Demo {

    /*
		a demo script that shows how to interact with 
		the ragdoll controller component, and link it to any character movement
        script		
	*/

    [RequireComponent(typeof(CharacterMovement))]
    public class CharacterMovementRagdollLink : MonoBehaviour
    {
        public bool adjustCapsuleHeightToModel;
        public float minCapsuleHeight = .25f;
        /*
			fall speeds to set for the ragdoll controller
		*/
		[Header("Fall Decay Speeds")]
		public float[] fallDecaySpeeds = new float[] { 5, 2, 2 };
        CharacterMovement characterMovement;
        RagdollController ragdollController;
        CharacterController characterController;


        void Awake () {
            characterMovement = GetComponent<CharacterMovement>();
            characterController = GetComponent<CharacterController>();

            ragdollController = GetComponent<RagdollController>();
            // collisionRagdoller = GetComponent<CollisionRagdoller>();
        }
        void FixedUpdate () {
            if (adjustCapsuleHeightToModel && characterController.enabled)
                UpdateCapsuleHeight();

            ragdollController.charHeight = characterController.height;
            ragdollController.charStepOffset = characterController.stepOffset;

            // ragdoll on collision
            CheckForIgnoreExpires();
            UpdateIncomingTriggerCapsule(characterController.enabled);
        }
            

        //calculate the character height based on the distance between the top of our head
        //and out feet
        float calculateCharHeight {
            get {
                //get head bone (should be teleporting to master anyways)
                RagdollTransform headBone = ragdollController.ragdoll.GetBone(HumanBodyBones.Head);
                
                //get its shpere collider
                SphereCollider sphere = (SphereCollider)headBone.collider;
                
                Vector3 headCenterWorldPos = headBone.transform.position + (headBone.transform.rotation * sphere.center);
                
                //the height is the distanc form teh top of the head collider to our character feet
                return Mathf.Max(minCapsuleHeight, (headCenterWorldPos.y + sphere.radius) - transform.position.y);
            }
        }
        
        void UpdateCapsuleHeight () {
            float charHeight = calculateCharHeight;
            characterController.height = charHeight;
            characterController.center = new Vector3(0, charHeight * .5f, 0);       
        }
            
        void Update () {
            if (characterMovement.freeFalling) {
				ragdollController.GoRagdoll("free fall");
			}
			
            // if we're falling, dont let us go "up"
            // ragdoll was falling up stairs...
            characterMovement.preventUpwardsGroundedMotion = ragdollController.state == RagdollControllerState.Falling; 
            
            // dont accept any movement changes from player input, or ai input
            characterMovement.disableExternalMovement = ragdollController.state != RagdollControllerState.Animated || ragdollController.isGettingUp; 
                    
            /*
                skip moving the main transform if we're completely ragdolled, or waiting to reorient
                the main transform via the ragdoll controller
            */
            characterMovement.disableAllMovement = ragdollController.state == RagdollControllerState.Ragdolled || ragdollController.state == RagdollControllerState.TeleportMasterToRagdoll; 


            /*
                when animated or blending to animation
                    use character controller movement 
                    
                    it has less step offset jitter than the normal transform movement
                    especially when getting up 

                else when falling:
                        
                    use normal transform stuff (dont want the character controller collisions messing stuff up)
                    for falling /calculating fall ( we need all exterion collisions to reach ragdol bones)
                    and teh characer chontroller acts as a 'protective shell' when it's enabled
            */
            
            characterMovement.usePhysicsForMove = ragdollController.state == RagdollControllerState.Animated || ragdollController.state == RagdollControllerState.BlendToAnimated; 


            //cehck if we started getting up
			if (ragdollController.state == RagdollControllerState.BlendToAnimated) {
				//set zero speed
				if (characterMovement.currentSpeed != 0) {
					characterMovement.SetMovementSpeed(0);
				}
			}

            int currentSpeed = (int)characterMovement.currentSpeed;

            if (currentSpeed < 0 || currentSpeed >= fallDecaySpeeds.Length) {
                Debug.LogError("current speed: " + currentSpeed + " :: out of range for fall decays");
            }
            else {
                //set the ragdolls fall speed based on our speed
                ragdollController.SetFallSpeed(fallDecaySpeeds[(int)characterMovement.currentSpeed]);
            }
        }

        void OnRevive () {
            characterMovement.SetMovementSpeed(0);
        }



        /*
            NOTE FOR GOING RAGDOLL ON COLLISIONS::

            objects that shoulve been hitting the bones were bouncing off teh character controller
            (and using the character controller for ragdoll deciding wasnt working, since teh capsule shape
            had empty space that was triggering ragdolls)
            also, using the Character Controller itself to detect the outgoing collisions was making 
            the character controller stop movement by the time we got the collision callback.

            for that reason we have an external collision checker that lets us know about collisions
            that are incoming, if eitehr the character or immenent colliding rigidbody
            are traveling fast enough, the character controller ignores collisions with it for a small
            period of time.

            that way it gives the bones a chance to collide and decide whether or not to go rigidbody

            the trigger detector mentioned above is wider and taller than the character capsule, 
            in order to avoid them bouncing off if we set the ignore too late

        */

        [Header("Ragdoll On Collision Options:")]
        [Tooltip("How much space around the character controller to pre check for rigidbodies.\n\nIf the character controller is blocking rigidbody objects that should be ragdolling us, inrease this value.")]
        public float incomingCheckOffset = .5f;
        
        [Tooltip("How long teh character controller ignores rigidbody objects (travelling above 'incomingMagnitudeThreshold' velocity), to give them a chance to hit the bones")]
        public float characterControllerIgnoreTime = 2f;

        /*

			Primary collison detector
            (if it registers a collision large enough, we make the controller ignore its collider)
		*/
		TriggerDetector inCollisionDetector;
        
        // ignore pairs for character controller ignoring rigidbodies that might hit our ragdoll bones
        List<ColliderIgnorePair> charControllerIgnorePairs = new List<ColliderIgnorePair>();
        
		void Start () {
            //make ragdoll bones ignore character controller
            ragdollController.ragdoll.IgnoreCollisions(characterController, true);
            ragdollController.ragdollOnCollision.SetGetVelocityCallback(GetCharVelocity);
		}

        Vector3 GetCharVelocity () {
            return characterController.velocity;
        }


        TriggerDetector BuildCollisionDetector () {
        	
            TriggerDetector detector = new GameObject(name + "_incomingCollisionDetector").AddComponent<TriggerDetector>();
			detector.onTriggerEnter += OnIncomingCollision;

            // set our transform as the parent so the detectors move with us
            detector.transform.SetParent(transform);
			detector.transform.localPosition = Vector3.zero;
			detector.transform.rotation = Quaternion.identity;

            //make ragdoll bones ignore collisions with the trigger detection capsule
			ragdollController.ragdoll.IgnoreCollisions(detector.capsule, true);

            //make character controller ignore trigger detectors
            Physics.IgnoreCollision(characterController, detector.capsule, true);
            
			return detector;
		}
        
        /*
            adjust incoming trigger chekc height and radius
        */
        void UpdateIncomingTriggerCapsule (bool enabled) {

            enabled = enabled && ragdollController.ragdollOnCollision.enabled;

            if (enabled && inCollisionDetector == null) {
                //initialize trigger detector
            
                inCollisionDetector = BuildCollisionDetector();                
            }
                
            
            // check for ignore trigger only when the character controller is enabled
            inCollisionDetector.enabled = enabled;
                
            if (enabled) {
                float height = characterController.height + incomingCheckOffset;
                float radius = characterController.radius + incomingCheckOffset;

                CapsuleCollider capsule = inCollisionDetector.capsule;
                capsule.height = height;
                capsule.radius = radius;
                capsule.center = new Vector3(0, height * .5f, 0);
            }
        }

        bool ControllerIsIgnoringCollider (Collider other) {

            for (int i = 0; i < charControllerIgnorePairs.Count; i++) {
                var pair = charControllerIgnorePairs[i];
                if (pair.collider2 == other) {
                    pair.ignoreTime = Time.time;
                    return true;
                }
            }
            return false;
        }
                
        /*
            trigger checks if something invaded our space

            then we check if it's going fast enough to ragdoll us,
            make our character controller ignore it for a little bit, 
            so it has a chance to hit our bones
        */
        void OnIncomingCollision (Collider other) {
            if (!ragdollController.ragdollOnCollision.enabled) {
                return;
            }
            
            // check for a rigidbody 
            // (dont want character controller to ignore static geometry)
            
            Rigidbody rigidbody = other.attachedRigidbody;
            if (rigidbody == null || rigidbody.isKinematic)
                return;
            
            //check if it's above either of our thresholds
            float incomingThreshold = ragdollController.profile.incomingMagnitudeThreshold * ragdollController.profile.incomingMagnitudeThreshold;
            float outgoingThreshold = ragdollController.profile.outgoingMagnitudeThreshold * ragdollController.profile.outgoingMagnitudeThreshold;

            if (rigidbody.velocity.sqrMagnitude < incomingThreshold && characterController.velocity.sqrMagnitude < outgoingThreshold)
                return;
            
            //already ignoring
            if (ControllerIsIgnoringCollider(other))
                return;

            charControllerIgnorePairs.Add(new ColliderIgnorePair(characterController, other));
        }       

        void CheckForIgnoreExpires () {

            // unignore the character controller with colliders that couldve ragdolled us
            for (int i = charControllerIgnorePairs.Count - 1; i >= 0; i--) {
                ColliderIgnorePair p = charControllerIgnorePairs[i];
                
                // if enough time has passed
                if (p.timeSinceIgnore >= characterControllerIgnoreTime) {
                    p.EndIgnore();
                    charControllerIgnorePairs.Remove(p);
                }
            }
        }
    }






}

