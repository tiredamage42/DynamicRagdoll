using UnityEngine;

namespace DynamicRagdoll {
    /*
        moves the ragdoll after grabbing it by a specified bone
    */

    [RequireComponent(typeof(Rigidbody))]
    public class JointLimitsTester : MonoBehaviour
    {
        public float moveSpeed = 5;
        public float moveDistance = 5;
        
        public Vector3 rotateWeights = Vector3.forward;
        public float rotateSpeed = 1;


        [Header("Hang Point Options")]
        public Ragdoll testRagdoll;
        public HumanBodyBones boneToHang;
        public bool calculateOffset = true;
        public bool freeRotation = true;
        public Vector3 anchorOffset = Vector3.zero;

        void Awake () {
            GetComponent<Rigidbody>().isKinematic = true;
        }
        
        void Start () {
            if (testRagdoll != null) {
                if (Ragdoll.BoneIsPhysicsBone(boneToHang)) {
                    
                    RigidbodyHangPoint hangPoint = gameObject.AddComponent<RigidbodyHangPoint>();

                    hangPoint.calculateOffset = calculateOffset;
                    hangPoint.freeRotation = freeRotation;
                    hangPoint.anchorOffset = anchorOffset;
                    hangPoint.rigidbodyToHang = testRagdoll.GetBone(boneToHang).rigidbody;
                }
                    
                else {
                    Debug.LogError(boneToHang + " Is not a ragdoll physics bone");
                }
            }
        }
        
        
        void Update()
        {
            Vector3 pos = transform.position;
            transform.position = new Vector3(Mathf.Sin(Time.time * moveSpeed) * moveDistance, pos.y, pos.z);
            transform.Rotate(rotateWeights * rotateSpeed, Space.World);
        }
    }
}
