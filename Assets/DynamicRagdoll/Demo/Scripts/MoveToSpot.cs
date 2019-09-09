using UnityEngine;

using DynamicRagdoll;
public class MoveToSpot : MonoBehaviour
{
    public float speed = 1;
    public Transform target;
    RagdollController controller;

    void Awake () {
        controller = GetComponent<RagdollController>();
    }
    void Update()
    {
        if (controller.state == RagdollControllerState.Animated) {
            RagdollPhysics.MovePossibleCharacterController(transform, Vector3.Lerp(transform.position, target.position, Time.deltaTime * speed));
        }
    }
}
