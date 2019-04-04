using UnityEngine;

namespace DynamicRagdoll.Collisions {

    /*
        Add this script to the main character to enable character collisions.
        
        
        1. adds collision forces to rigidbodies colliding with our CharacterController component

        2. checks for incoming rigidbody collisions, our when we run into something,
            with enough force, if they're above the threshold, we go ragdoll

        3. when ragdolling, we check for collisions on the bones to add bone decay
            (through the attached RagdollController) based on teh magnitude of the collisions
        

        since the ragdoll needs a frame or two to go down, objects were bouncing off 
        immediately wihtout affecting the bones

        for that reason we have 2 external collision checkers that let us know about collisions
            
        optimally they'll be set up to a wider and taller area than the character, 
        so the ragdoll bone colliders have some 'breathing room' between going ragdoll 
        and actually being hit by the object

        then we worry about adding decay to the bones that are hit, while we're falling and 
        going ragdoll, 

        INCOMING COLLISIONS:
            the larger, outer capsule detects incoming rigidbodies that are about to hit our character
            if the rigidbody's velocity is large enough we go ragdoll

        OUTGOING COLLISIONS:
            the smaller inner capsule detects any "outgoing" collisions, if we've hit anything
            and out velocity is large enough


        we need them to be differnt size, so they're seperate colliders

        the characterController and inner trigger detector capsule's hiehgt is set to the world position
        of the top of the ragdoll head collider in case our animation crouches down or whatever, 
        
    
        NOTE:
            although the outgoing collisions detector capsule has similar values to the 
            character controller,
            
            using the Character Controller itself to detect the outgoing collisions was making 
            the character controller stop movement by the time we got the collision callback.

            the radius of the CharacterController should be slightly lower than the radius
            of outgoing collision checks.                            
    */


    [RequireComponent(typeof(RagdollController))]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterCollisions : MonoBehaviour
    {
        [System.Serializable] public class DetectorInfo {
    
            [Tooltip("Collisions above this magnitude will trigger ragdolling")] 
            public float magnitudeThreshold = 10;
            [Tooltip("How much wider is the check from the Character Controller")]
            public float radiusOffset = .5f;
        
            public DetectorInfo(float magnitudeThreshold, float radiusOffset) {
                this.magnitudeThreshold = magnitudeThreshold;
                this.radiusOffset = radiusOffset;
            }
        }

        [Header("Detectors")]
        [Tooltip("When we run into something")]
        public DetectorInfo outgoing = new DetectorInfo(2, .1f);

        [Tooltip("When we get hit with an object")]
        public DetectorInfo incoming = new DetectorInfo(5, .25f);
        
        [Tooltip("How much taller is the incoming check from the top of the character's head")]
        public float incomingHeightOffsest = .25f;
                
        //public UpdateMode calculateVelocityMode = UpdateMode.Update;

        
        /*
            this wide range means it doesnt necessarily decay any bones completely
            but adds some noise to the decay, that way the more we collide the more we slow down
            ...not in one fell swoop 
        */
        [Header("Bone Decay")]
        [Tooltip("Collisions magnitude range for linearly interpolating the bone decay set by collisions.\nIf magnitude == x, then bone decay = 0\nIf magnitude == y, then bone decay = 1")] 
        public Vector2 decayMagnitudeRange = new Vector2(10, 50);

        [Tooltip("Multiplier for decay set by collisions on neighbors of collided bones")]
        [Range(0,1)] public float neighborDecayMultiplier = .75f;


        /*
			Primary collison detectors 
            (if they register collisions large enough, we make the controller go ragdoll)
		*/
		TriggerDetector inCollisionDetector, outCollisionDetector;		
        RagdollController ragdollController;
        CharacterController characterController;
        //bool wasDetectingOutgoing;
        //VelocityTracker velocityTracker;
        

        //calculate the character height based on the distance between the top of our head
        //and out feet
        float calculateCharHeight {
            get {

                //get head bone (should be teleporting to master anyways)
                Ragdoll.Bone headBone = ragdollController.ragdoll.GetPhysicsBone(HumanBodyBones.Head);
                
                //get it's shpere collider
                SphereCollider sphere = (SphereCollider)headBone.collider;
                
                Vector3 headCenterWorldPos = headBone.position + (headBone.rotation * sphere.center);
                
                //the height is the distanc form teh top of the head collider to our character feet
                return (headCenterWorldPos.y + sphere.radius) - transform.position.y;
            }
        }

        void Awake () 
		{
            characterController = GetComponent<CharacterController>();
            ragdollController = GetComponent<RagdollController>();
			
            //velocityTracker = new VelocityTracker(transform, Vector3.zero);
		}
        
		void Start () {
            //make ragdoll bones ignore character controller
            ragdollController.ragdoll.IgnoreCollisions(characterController, true);
			
            //subscribe to receive a callback on ragdoll bone collision
			ragdollController.ragdoll.AddCollisionCallback (OnRagdollCollision);
			
            //initialize base collision detectors
            inCollisionDetector = BuildCollisionDetector(name+"_incomingCollisionDetector", OnIncomingCollision);
			outCollisionDetector = BuildCollisionDetector(name+"_outgoingCollisionDetector", OnOutgoingCollision);
			
            //UpdateCollisionTrackerValues();
		}

        TriggerDetector BuildCollisionDetector (string name, System.Action<Collider> onCollision) {		
        	
            TriggerDetector detector = new GameObject(name).AddComponent<TriggerDetector>();
			detector.SubscribeToTrigger(onCollision);

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

        // void Update () {
        //     CalculateSelfVelocity(UpdateMode.Update, Time.deltaTime);
		// }
		// void LateUpdate () {
		// 	CalculateSelfVelocity(UpdateMode.LateUpdate, Time.deltaTime);
		// }

           
		void UpdateTriggerTrackers () {
            /*
			    set enabled, 
                check for collisions only when we're animating or blending to animation
            */
            bool detectOutgoing = ragdollController.state == RagdollControllerState.Animated;
			
            // if (detectOutgoing) {
            //     if (!wasDetectingOutgoing) {
            //         velocityTracker.Reset();
            //     }
            // }
            // wasDetectingOutgoing = detectOutgoing;

            outCollisionDetector.enabled = detectOutgoing;
            inCollisionDetector.enabled = detectOutgoing || ragdollController.state == RagdollControllerState.BlendToAnimated;
		}


        
        void FixedUpdate () {
            UpdateTriggerTrackers();
           
            UpdateCapsules();
            
           // CalculateSelfVelocity(UpdateMode.FixedUpdate, Time.fixedDeltaTime);
        }


        // void CalculateSelfVelocity (UpdateMode modeCheck, float deltaTime) {
        //     bool detectOutgoing = ragdollController.state == RagdollControllerState.Animated;
        //     if (!detectOutgoing) {
        //         return;
        //     }
        //     if (modeCheck != calculateVelocityMode) {
		// 		return;
		// 	}
        //     //calculate our transform's velocity
        //     //velocityTracker.TrackVelocity(1f / deltaTime, true);
        // }
        
        
        void UpdateCapsules () {

            float charHeight = calculateCharHeight;
            
            if (charHeight > 0) {
                UpdateCharacterController(charHeight);
                UpdateOutgoingTriggerCapsule(charHeight);
                UpdateIncomingTriggerCapsule(charHeight);
            }    
        }

        /*
            adjust character controller height and radius (inner most smallest capsule)
        */
        void UpdateCharacterController (float charHeight) {
            characterController.center = new Vector3(0, charHeight * .5f, 0);
            characterController.height = charHeight;
        }

        /*
            adjust outgoing trigger chekc height and radius (second largest capsule)
        */
        void UpdateOutgoingTriggerCapsule (float charHeight) {

            CapsuleCollider capsule = outCollisionDetector.capsule;
            
            /*
                offset for step height, so steps dont make us go ragdoll
            */
            float mid = (charHeight + characterController.stepOffset) * .5f;
            capsule.center = new Vector3(0, mid, 0);
            capsule.height = charHeight - characterController.stepOffset;
            capsule.radius = characterController.radius + outgoing.radiusOffset;
        }

        /*
            adjust incoming trigger chekc height and radius (largest capsule)
        */
        void UpdateIncomingTriggerCapsule (float charHeight) {
            CapsuleCollider capsule = inCollisionDetector.capsule;
            float h = charHeight + incomingHeightOffsest;
            capsule.height = h;
            capsule.center = new Vector3(0, capsule.height * .5f, 0);
            capsule.radius = characterController.radius + incoming.radiusOffset;
        }


        /*
            let character controller move rigidbodies
        */
        void OnControllerColliderHit(ControllerColliderHit hit) {

            // We dont want to push objects below us
            if (hit.moveDirection.y < -0.3) {
                return;
            }

            //check for rigidbody            
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic) {
                return;
            }
            
            // Calculate push direction from move direction,
            // we only push objects to the sides never up and down
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

            // Apply the push
            //rb.velocity = pushDir.normalized * hit.controller.velocity.magnitude;
            rb.velocity = pushDir * hit.controller.velocity.magnitude;
        }


        /*
            check if a velocity is above the threshold
        */
        void CheckVelocityCollision (Vector3 velocity, float threshold, Collider other, string message) {

            // check if our velocity is large enough
            if (velocity.sqrMagnitude >= threshold * threshold) {   
                Debug.Log(message + " went ragdoll cuae of " + other.name + "/" + velocity.magnitude);
                ragdollController.GoRagdoll();
            }
            Debug.Log("has " + message + " hit but not velocity " + other.name + "/" + velocity.magnitude);
        }

        /*
            when the smaller inner capsule triggers, we've run into something
        */
        void OnOutgoingCollision (Collider other) {
            //CheckVelocityCollision(velocityTracker.velocity, outgoing.magnitudeThreshold, other, "outgoing");
            CheckVelocityCollision(characterController.velocity, outgoing.magnitudeThreshold, other, "outgoing");
        }

        /*
            when the outer larger capsule triggers, something has invaded our spac
        */
        void OnIncomingCollision (Collider other) {
            
            //check for a rigidbody
            Rigidbody rigidbody = other.attachedRigidbody;
            if (rigidbody == null || rigidbody.isKinematic) {
                return;
            }
            CheckVelocityCollision(rigidbody.velocity, incoming.magnitudeThreshold, other, "incoming");
        }

        /*
			callback called when ragdoll bone gets a collision
		*/
		void OnRagdollCollision(RagdollBone bone, Collision collision)
		{
			/*
				collisions on bones are only registered to add decay, 
				so we only care if we're hit when falling....
			*/
			if (ragdollController.state != RagdollControllerState.Falling)
				return;
			
			//ignore floor
			if (collision.transform.CompareTag("Floor")) {
				return;
			}

			//check for and ignore self ragdoll collsion (MOVE TO RAGDOLL SCRIPT)
			for (int i = 0; i < Ragdoll.physicsBonesCount; i++) {
				if (collision.transform == ragdollController.ragdoll.GetPhysicsBone(Ragdoll.phsysicsHumanBones[i]).transform) {
					return;
				}
			}
			
			float magnitude = collision.relativeVelocity.magnitude;
			
			// if the magnitude is above the minimum threshold for adding decay
			// based on collisions
			if (magnitude >= decayMagnitudeRange.x) {

				//linearly interpolate decay between 0 and 1 base on collision magnitude
				float linearDecay = (magnitude - decayMagnitudeRange.x) / (decayMagnitudeRange.y -  decayMagnitudeRange.x);
				
                //clamp
                linearDecay = Mathf.Clamp01(linearDecay);

				Debug.Log(bone + " / " + collision.transform.name + " mag: " + magnitude + " decay " + linearDecay);
				
				ragdollController.SetBoneDecay(bone.bone, linearDecay, linearDecay * neighborDecayMultiplier);
			}
		}
    }
}