using UnityEngine;

namespace DynamicRagdoll.Demo {
    public class Rotator : MonoBehaviour
    {
        public Vector3 axisWeights = Vector3.forward;
        public float speed = 1;
        void Update()
        {
            transform.Rotate(axisWeights * speed, Space.World);
        }
    }
}
