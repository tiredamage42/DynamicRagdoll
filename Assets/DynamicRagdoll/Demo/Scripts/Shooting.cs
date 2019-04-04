using System.Collections;
using UnityEngine;

namespace DynamicRagdoll.Demo {

	/*
	 
		script for showing how to "shoot" the ragdolls

	*/
    public class Shooting : MonoBehaviour
    {
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
				//check if we hit a ragdoll bone
				RagdollBone ragdollBone = hit.transform.GetComponent<RagdollBone>();
				
                if (ragdollBone) {
					
					// treat it like a rigidbody or collider
					ragdollBone.AddForceAtPosition(ray.direction.normalized * modifiedBulletForce, hit.point, ForceMode.VelocityChange);

					// check if the ragdoll has a controller
					if (ragdollBone.ragdoll.hasController) {
						RagdollController controller = ragdollBone.ragdoll.controller;

						// set bone decay for the hit bone, so the physics will affect it
						// slightly lower for neighbor bones

						float mainDecay = 1;
						float neighborDecay = .75f;
						controller.SetBoneDecay(ragdollBone.bone, mainDecay, neighborDecay);
						
						//make it go ragdoll
						controller.GoRagdoll();					
					}
				}
				else {

					// shoot normally

					Rigidbody rb = hit.transform.GetComponent<Rigidbody>();
					
                    if (rb) {
						rb.AddForceAtPosition(ray.direction.normalized * modifiedBulletForce, hit.point, ForceMode.VelocityChange);
					}
				}
			}
		}
    }
}
