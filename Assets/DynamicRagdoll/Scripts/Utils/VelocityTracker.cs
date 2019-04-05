using UnityEngine;

namespace DynamicRagdoll {

    /*
        tracks the velocity of a given transform
    */
    public class VelocityTracker
    {
        public Vector3 velocity;

        Transform trackTransform;   
        Vector3 localOffset, lastPosition;
        Vector3 actualPosition { 
            get { 
                return localOffset != Vector3.zero ? trackTransform.position + (trackTransform.rotation * localOffset) : trackTransform.position; 
            } 
        }

        /*
            local offset could be center of mass
        */
        public VelocityTracker(Transform trackTransform, Vector3 localOffset) {
            this.trackTransform = trackTransform;
            this.localOffset = localOffset;
            Reset();
        }

        public void Reset () {
            lastPosition = actualPosition;
        }

        public Vector3 TrackVelocity (float reciprocalDeltaTime, bool track2D) {
            // the new position of the transform,
            Vector3 position = this.actualPosition;
            		
            Vector3 distance = position - lastPosition;
            
            if (track2D) {
                distance.y = 0;
            }
									
            /*
                velocity = distance / time
            
                but multiplying by reciprocal is faster
            */
            velocity = distance * reciprocalDeltaTime;

            lastPosition = position;
			
            return velocity;
        }
    }
}