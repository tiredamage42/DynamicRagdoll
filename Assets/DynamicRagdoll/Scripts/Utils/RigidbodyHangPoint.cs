using UnityEngine;

namespace DynamicRagdoll {
    public class RigidbodyHangPoint : MonoBehaviour
    {
        
        public Rigidbody rigidbodyToHang;
        public bool calculateOffset = false;
        public Vector3 connectedAnchorOffset = Vector3.zero;
        
        void Awake () {
            if (rigidbodyToHang) {
                if (calculateOffset) {
                    RagdollPhysics.HangRigidbody(rigidbodyToHang, GetComponent<Rigidbody>());            
                }
                else {
                    RagdollPhysics.HangRigidbody(rigidbodyToHang, GetComponent<Rigidbody>(), connectedAnchorOffset);            
                }
            }
        }        
    }
}
