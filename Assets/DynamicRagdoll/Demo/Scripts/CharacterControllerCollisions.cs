using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DynamicRagdoll.Demo {
    /*
        adds collision forces to rigidbodies colliding with our CharacterController component
    */

    [RequireComponent(typeof(CharacterController))]
    public class CharacterControllerCollisions : MonoBehaviour
    {
        /*
            let character controller move rigidbodies
        */
        void OnControllerColliderHit(ControllerColliderHit hit) {

            // We dont want to push objects below us
            if (hit.moveDirection.y < -0.3)
                return;

            //check for rigidbody            
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb == null || rb.isKinematic)
                return;
            
            // Calculate push direction from move direction,
            // we only push objects to the sides never up and down
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

            // Apply the push
            rb.velocity = pushDir * hit.controller.velocity.magnitude;
        }
    }
}
