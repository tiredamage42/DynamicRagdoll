using UnityEngine;

namespace DynamicRagdoll.Collisions {

    /*
        Add this script to the main character to enable ragdolling when we've collided
        with enough force (based on the variables specified on this ocmponent)

        it also adds bone decay to ragdoll bones (on the attached RagdollController) based 
        on the magnitude of their collisions, when the ragdoll is in it's falling state
        

        Collision Checking:
            since the ragdoll needs a frame or two to go down, 
            objects were bouncing off immediately wihtout affecting the bones

            for that reason we have external collision checkers that let us know 
            when an incoming collider triggers them above a certain velocity threshold.
            thats when we go ragdoll

            optimally they'll be set up to a wider and taller area than the character, 
            so the ragdoll bone colliders have some 'breathing room' between going ragdoll 
            and actually being hit by the object

            then we worry about adding decay to the bones that are hit, 
            while we're falling and going ragdoll, 
    */

    [RequireComponent(typeof(RagdollController))]
    public class RagdollCollisions : MonoBehaviour
    {
        [System.Serializable] public class DetectorInfo {

            [Tooltip("Collisions above this magnitude will trigger ragdolling")] 
            public float magnitudeThreshold = 10;
            public float height = 2.25f;
            public float radius = .5f;

            public DetectorInfo(float magnitudeThreshold, float height, float radius) {
                this.magnitudeThreshold = magnitudeThreshold;
                this.height = height;
                this.radius = radius;
            }
        }

        [Header("Detectors")]
        [Tooltip("When we get hit with an object")]
        public DetectorInfo incoming = new DetectorInfo(5, 2.25f, .75f);
        [Tooltip("When we run into something")]
        public DetectorInfo outgoing = new DetectorInfo(2, 2f, .35f);
        
        public CollisionDetector.CalculateVelocityMode calculateVelocityMode = CollisionDetector.CalculateVelocityMode.Update;

        
        /*
            this wide range means it doesnt necessarily decay any bones completely

            but adds some noise to the decay, that way the more we collide the more we slow down

            not in one fell swoop 
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
		CollisionDetector inCollisionDetector, outCollisionDetector;		
        RagdollController ragdollController;

        void Awake () 
		{
            ragdollController = GetComponent<RagdollController>();
			
			//subscribe to receive a callback on ragdoll bone collision
			ragdollController.ragdoll.AddCollisionCallback (OnRagdollCollision);

            //initialize base collision detectors
            inCollisionDetector = BuildCollisionDetector(name+"_incomingCollisionDetector", false, OnCollisionDetectorTrigger);
			outCollisionDetector = BuildCollisionDetector(name+"_outgoingCollisionDetector", true, OnCollisionDetectorTrigger);
			
            UpdateCollisionTrackerValues();
		
		}

		
		/*
			called by collision detectors when they're triggered

            we go ragdoll
		*/
		void OnCollisionDetectorTrigger (Collider collider) {
			ragdollController.GoRagdoll();
		}

        void SetCollisionDetectorValues(CollisionDetector detector, DetectorInfo detectorInfo, bool enabled) {
			detector.velocityThreshold = detectorInfo.magnitudeThreshold;
			detector.radius = detectorInfo.radius;
			detector.height = detectorInfo.height;
			detector.enabled = enabled;
            detector.updateMode = calculateVelocityMode;
		}

        CollisionDetector BuildCollisionDetector (string name, bool checkingSelf, System.Action<Collider> onCollision) {
			CollisionDetector detector = new GameObject(name).AddComponent<CollisionDetector>();
			detector.SubscribeToCollision(onCollision);

            detector.checkingSelf = checkingSelf;
            detector.velocity2D = checkingSelf;

            // set our transform as the parent so the detectors move with us
            detector.transform.SetParent(transform);
			detector.transform.localPosition = Vector3.zero;
			detector.transform.rotation = Quaternion.identity;
			
			return detector;
		}

        void Update () {
			UpdateCollisionTrackerValues();
		}

		void UpdateCollisionTrackerValues () {
            /*
			    set enabled, 
                check for collisions only when we're animating or blending to animation
            */
			bool enabled = ragdollController.state == RagdollController.RagdollState.Animated || ragdollController.state == RagdollController.RagdollState.BlendToAnimated;
			
            SetCollisionDetectorValues(inCollisionDetector, incoming, enabled);
            SetCollisionDetectorValues(outCollisionDetector, outgoing, enabled);
		}

		void Start () {
			//make ragdoll bones ignore collisions with the collision detection capsules
			ragdollController.ragdoll.IgnoreCollisions(inCollisionDetector.capsule, true);
			ragdollController.ragdoll.IgnoreCollisions(outCollisionDetector.capsule, true);
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
			if (ragdollController.state != RagdollController.RagdollState.Falling) {
				return;
			}

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