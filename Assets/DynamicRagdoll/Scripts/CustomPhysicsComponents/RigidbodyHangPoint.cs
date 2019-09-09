using UnityEngine;

namespace DynamicRagdoll {

    /*
        hang or grab a rigidbody by this transform
    */
    public class RigidbodyHangPoint : MonoBehaviour
    {        
        public Rigidbody rigidbodyToHang;
        public bool calculateOffset = true;
        public bool freeRotation = true;
        public Vector3 anchorOffset = Vector3.zero;
        
        void Start () {
            if (rigidbodyToHang) {
                if (calculateOffset) {
                    RagdollPhysics.GrabRigidbody(rigidbodyToHang, GetComponent<Rigidbody>(), freeRotation);            
                }
                else {
                    RagdollPhysics.GrabRigidbody(rigidbodyToHang, GetComponent<Rigidbody>(), anchorOffset, freeRotation);            
                }
            }
        }        
    }
}
