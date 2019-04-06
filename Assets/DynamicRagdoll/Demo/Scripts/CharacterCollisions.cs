using UnityEngine;
using System.Collections.Generic;
namespace DynamicRagdoll.Demo {

    /*
        Add this script to the main character to enable character collisions.
        
            1. adds collision forces to rigidbodies colliding with our CharacterController component

            2. checks for incoming rigidbody collisions, our when we run into something,
                with enough force, if they're above the threshold, we go ragdoll

            3. when ragdolling, we check for collisions on the bones to add bone decay
                (through the attached RagdollController) based on teh magnitude of the collisions

        objects that shoulve been hitting the bones were bouncing off teh character controller
        (and using the character controller for ragdoll deciding wasnt working, since teh capsule shape
        had empty space that was triggering ragdolls)

        for that reason we have an external collision checkers that let us know about collisions
        that are incoming, if they're eitehr the character or immenent colliding rigidbody
        are traveling fast enough, the character controller ignores collisions with it for a small
        period of time.

        that way it gives the bones a chance to collide and decide whether or not to go rigidbody

        the trigger detector mentioned above is wider and taller than the character capsule, 
        in order to avoid them bouncing off if we set the ignore too late

        then we worry about adding decay to the bones that are hit, while we're falling and 
        going ragdoll, 

        the characterController's hiehgt is set to the world position
        of the top of the ragdoll head collider in case our animation crouches down or whatever, 
    
        NOTE:
            using the Character Controller itself to detect the outgoing collisions was making 
            the character controller stop movement by the time we got the collision callback.
    */

    [RequireComponent(typeof(RagdollController))]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterCollisions : MonoBehaviour
    {
        /*
            use to temporarily ignore collisions between two colliders
        */
        struct ColliderIgnorePair {
            public Collider collider1, collider2;
            public float ignoreTime;
            public float timeSinceIgnore { get { return Time.time - ignoreTime; } }

            public ColliderIgnorePair(Collider collider1, Collider collider2) {
                this.collider1 = collider1;
                this.collider2 = collider2;
                this.ignoreTime = Time.time;

                Physics.IgnoreCollision(collider1, collider2, true);
            }

            public void EndIgnore () {
                Physics.IgnoreCollision(collider1, collider2, false);
            }
        }


        [Header("Ragdoll Detection")]
        [Tooltip("When we run into something.\nHow fast do we have to be going to go ragdoll")]
        public float outgoingMagnitudeThreshold = 5;
        
        [Tooltip("When we get hit with an object.\nHow fast does it have to be going to go ragdoll")]
        public float incomingMagnitudeThreshold = 5;
        
        [Tooltip("How much space around the character controller to pre check for rigidbodies.\n\nIf the character controller is blocking rigidbody objects that should be ragdolling us, inrease this value.")]
        public float incomingCheckOffset = .25f;
        
        [Tooltip("How long teh character controller ignores rigidbody objects (travelling above 'incomingMagnitudeThreshold' velocity), to give them a chance to hit the bones")]
        public float characterControllerIgnoreTime = 2f;

        [Tooltip("Objects touching us from above, that are over this mass, will trigger ragdoll")]
        public float crushMass = 20;
        [Tooltip("How far down from the top of our head should we consider a crush contact")]
        public float crushMassTopOffset = .25f;
                
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
			Primary collison detector
            (if it registers a collision large enough, we make the controller ignore its collider)
		*/
		TriggerDetector inCollisionDetector;
        
        RagdollController ragdollController;
        CharacterController characterController;

        //ignore pairs for blending bones ignorign static colliders
        HashSet<ColliderIgnorePair> blendBonesIgnoreStaticColliders = new HashSet<ColliderIgnorePair>();
        
        // ignore pairs for character controller ignoring rigidbodies that might hit our ragdoll bones
        List<ColliderIgnorePair> charControllerIgnorePairs = new List<ColliderIgnorePair>();
        
        //contacts generated by ragdoll collision enter
        ContactPoint[] contacts = new ContactPoint[5];
        int contactCount;

        
        //calculate the character height based on the distance between the top of our head
        //and out feet
        float calculateCharHeight {
            get {

                //get head bone (should be teleporting to master anyways)
                Ragdoll.Bone headBone = ragdollController.ragdoll.GetPhysicsBone(HumanBodyBones.Head);
                
                //get it's shpere collider
                SphereCollider sphere = (SphereCollider)headBone.collider;
                
                Vector3 headCenterWorldPos = headBone.transform.position + (headBone.transform.rotation * sphere.center);
                
                //the height is the distanc form teh top of the head collider to our character feet
                return (headCenterWorldPos.y + sphere.radius) - transform.position.y;
            }
        }

        // planar velocity (so we dont ragdoll on steps)
        Vector3 controllerVelocity {
            get {
                Vector3 velocity = characterController.velocity;
                velocity.y = 0;
                return velocity;
            }
        }
        

        void Awake () 
		{
            characterController = GetComponent<CharacterController>();
            ragdollController = GetComponent<RagdollController>();
		}
        
		void Start () {
            //make ragdoll bones ignore character controller
            ragdollController.ragdoll.IgnoreCollisions(characterController, true);
			
            //subscribe to receive a callback on ragdoll bone collision
			ragdollController.ragdoll.AddCollisionEnterCallback (OnRagdollCollisionEnter);
			
            //initialize trigger detector
            inCollisionDetector = BuildCollisionDetector(name+"_incomingCollisionDetector", OnIncomingCollision);
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
                   
        void FixedUpdate () {
            
            CheckForIgnoreExpires();

            // check for ignore trigger only when the character controller is enabled
            inCollisionDetector.enabled = characterController.enabled;

            if (characterController.enabled) {
                UpdateCapsules();
            }
        }
        
        void UpdateCapsules () {
            float charHeight = calculateCharHeight;
            if (charHeight > 0) {
                UpdateCharacterController(charHeight);
                UpdateIncomingTriggerCapsule(charHeight);
            }    
        }

        /*
            adjust character controller height
        */
        void UpdateCharacterController (float charHeight) {
            characterController.height = charHeight;
            characterController.center = new Vector3(0, charHeight * .5f, 0);
        }

        /*
            adjust incoming trigger chekc height and radius
        */
        void UpdateIncomingTriggerCapsule (float charHeight) {
            CapsuleCollider capsule = inCollisionDetector.capsule;
            float h = charHeight + incomingCheckOffset;
            capsule.height = h;
            capsule.radius = characterController.radius + incomingCheckOffset;
            capsule.center = new Vector3(0, capsule.height * .5f, 0);
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
            rb.velocity = pushDir * hit.controller.velocity.magnitude;
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
            
            // check for a rigidbody 
            // (dont want character controller to ignore static geometry)
            
            Rigidbody rigidbody = other.attachedRigidbody;
            if (rigidbody == null || rigidbody.isKinematic)
                return;
            
            //check if it's above either of our thresholds
            float incomingThreshold = incomingMagnitudeThreshold * incomingMagnitudeThreshold;
            float outgoingThreshold = outgoingMagnitudeThreshold * outgoingMagnitudeThreshold;

            if (rigidbody.velocity.sqrMagnitude < incomingThreshold 
                && controllerVelocity.sqrMagnitude < outgoingThreshold
                //&& rigidbody.mass < crushMass
            )
                return;
            

            //already ignoring
            if (ControllerIsIgnoringCollider(other))
                return;

            charControllerIgnorePairs.Add(new ColliderIgnorePair(characterController, other));
        }       

        void CheckForIgnoreExpires () {

            //unignore the bones and static colliders that were ignored while blending
            if (ragdollController.state != RagdollControllerState.BlendToAnimated) {
                
                if (blendBonesIgnoreStaticColliders.Count > 0) {
                
                    foreach(var p in blendBonesIgnoreStaticColliders) {
                        p.EndIgnore();
                    }
                    blendBonesIgnoreStaticColliders.Clear();
                }
            }
            
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

        /*
            when blending to animation, ignore collisons wiht static colliders that bones 
            come in contact with (this eliminates jitters from bone teleport trying to go through
            the static collider).  

            bone rigidbodies cannot be kinematic though, or else collisions wont exert forces in time
            (when ragdollign on collision)
        */
        bool CheckForBlendIgnoreStaticCollision (RagdollBone bone, Collision collision) {
            // if wee're blending
            if (ragdollController.state == RagdollControllerState.BlendToAnimated) {

                //if it's static
                if (collision.collider.attachedRigidbody == null){   

                    blendBonesIgnoreStaticColliders.Add(new ColliderIgnorePair(bone.col, collision.collider));
                    return true;
                }
            }
            return false;
        }


        bool CollisionIsAboveStepOffset (Collision collision, float buffer) {
            // if it's below our step offset (plus a buffer)
            // ignore it, we can just step on top of it

            float offsetThreshold = characterController.stepOffset + buffer;
            contactCount = collision.GetContacts(contacts);

            for (int i = 0; i < contactCount; i++) {

                float yOffset = contacts[i].point.y - transform.position.y;
                
                if (yOffset > offsetThreshold){
                    return true;
                }
            }
            return false;
        }

        bool CollisionIsAboveCrushOffset (Collision collision) {
            float charHeight = calculateCharHeight;
            for (int i = 0; i < contactCount; i++) {
                float yOffset = contacts[i].point.y - transform.position.y;

                if (charHeight - yOffset < crushMassTopOffset) {
                    if (Vector3.Dot(Vector3.down, contacts[i].normal) > .75f) {

                        return true;
                    }
                }
            }
            return false;
        }
        bool CollisionHasCrushMass (Collision collision) {
            return collision.collider.attachedRigidbody != null && collision.collider.attachedRigidbody.mass >= crushMass;
        }
        
        /*
			callback called when ragdoll bone gets a collision

            then apply bone decay to those bones
		*/    
		void OnRagdollCollisionEnter(RagdollBone bone, Collision collision)
		{
			/*
				collisions on bones are only registered to add decay, 
				so we only care if we're hit when falling....
			*/
            //maybe add warp to master
            bool checkForRagdoll = ragdollController.state == RagdollControllerState.Animated || ragdollController.state == RagdollControllerState.BlendToAnimated;
            
            bool isFalling = ragdollController.state == RagdollControllerState.Falling;
			if (isFalling && !checkForRagdoll)
				return;

            if (CheckForBlendIgnoreStaticCollision(bone, collision))
                return;
            
            if (checkForRagdoll) {

                //if we're getting up, knock us out regardless
                if (!ragdollController.isGettingUp) {

                    // if it's below our step offset (plus a buffer)
                    // ignore it, we can just step on top of it
                    if (!CollisionIsAboveStepOffset(collision, .1f))
                        return;
                }
            }
            else {
                //check for and ignore self ragdoll collsion (only happens when falling)
                if (ragdollController.ragdoll.ColliderIsPartOfRagdoll(collision.collider))
                    return;
            }

			float collisionMagnitude2 = collision.relativeVelocity.sqrMagnitude;
            
            if (checkForRagdoll) {

                string message = "incoming";

                // if the collision is above our incoming threhsold, 
                bool goRagdoll = collisionMagnitude2 >= incomingMagnitudeThreshold * incomingMagnitudeThreshold;
                
                // else check if we're travelling fast enough to go ragdoll
                if (!goRagdoll){
                    message = "outgoing";
                    collisionMagnitude2 = controllerVelocity.sqrMagnitude;
                    goRagdoll = collisionMagnitude2 >= outgoingMagnitudeThreshold * outgoingMagnitudeThreshold;
                }

                //else check if we're being crushed
                if (!goRagdoll) {
                    message = "crush";
                    goRagdoll = CollisionIsAboveCrushOffset(collision) && CollisionHasCrushMass(collision);
                }
                    
                if (!goRagdoll)
                    return;

                //Debug.Log( message + "/" + bone.name + " went ragdoll cuae of " + collision.collider.name + "/" + Mathf.Sqrt(collisionMagnitude2));
                ragdollController.GoRagdoll();
            }
            HandleBoneDecayOnCollision(collisionMagnitude2, bone, collision);
		}

        void HandleBoneDecayOnCollision (float collisionMagnitude2, RagdollBone bone, Collision collision) {

            if (CollisionIsAboveCrushOffset(collision) && CollisionHasCrushMass(collision)) {
                
                //Debug.LogWarning(bone + " / " + collision.transform.name + " CrUSHED");
                
                ragdollController.SetBoneDecay(bone.bone, 1, neighborDecayMultiplier);
            }

            // if the magnitude is above the minimum threshold for adding decay
            else if (collisionMagnitude2 >= decayMagnitudeRange.x * decayMagnitudeRange.x) {
                float magnitude = Mathf.Sqrt(collisionMagnitude2);

                //linearly interpolate decay between 0 and 1 base on collision magnitude
                float linearDecay = (magnitude - decayMagnitudeRange.x) / (decayMagnitudeRange.y -  decayMagnitudeRange.x);

                //Debug.Log(bone + " / " + collision.transform.name + " mag: " + magnitude + " decay " + linearDecay);
                
                ragdollController.SetBoneDecay(bone.bone, linearDecay, neighborDecayMultiplier);
            }
        }
    }
}