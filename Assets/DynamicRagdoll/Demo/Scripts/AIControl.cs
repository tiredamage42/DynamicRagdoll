using UnityEngine;

/*
    just wanders around random points on our map
*/
namespace DynamicRagdoll.Demo {

    [RequireComponent(typeof(CharacterMovement))]
    public class AIControl : MonoBehaviour
    {
        public float playRadius = 500;
        public float turnSpeed = 5f;
        Vector3 destination;
        CharacterMovement characterMovement;

        void Awake () {
            characterMovement = GetComponent<CharacterMovement>();
        }
            
        void Start () {
            //maybe run, maybe walk
            characterMovement.SetMovementSpeed (Random.value < .5f ? 1 : 2);
            destination = new Vector3(Random.Range(-playRadius, playRadius), 0, Random.Range(-playRadius, playRadius));
        }

        void Update () {
            if (!characterMovement.disableExternalMovement) {
                if (characterMovement.currentSpeed == 0) {
                    Start();
                }
                
                CheckForArrival();
                TurnToDestination();
            }
        }

        void TurnToDestination () {
            Vector3 lookDir = destination - transform.position;
            lookDir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), turnSpeed * Time.deltaTime);
        }

        void CheckForArrival () {
            if (Vector3.SqrMagnitude(transform.position - destination) <= .25f) {
                Start();
            }
        }
    }
}
