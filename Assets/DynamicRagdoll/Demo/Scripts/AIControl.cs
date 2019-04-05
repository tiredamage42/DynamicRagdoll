using UnityEngine;

/*
    just wanders around random points on our map
*/
namespace DynamicRagdoll.Demo {

    [RequireComponent(typeof(Character))]
    public class AIControl : MonoBehaviour
    {
        public float playArea = 500;

        public float turnSpeed = 5f;
        const float minTravelDistance = 5f;
        Vector3 destination;
        Character character;

        void Awake () {
            character = GetComponent<Character>();
        }

            
        void Start () {
            //maybe run, maybe walk
            character.SetMovementSpeed (Random.value < .5f ? 1 : 2);
            destination = GetRandomPoint();
        }

        Vector3 GetRandomPoint() {
            float halfPlayArea = playArea * .5f;  
            return new Vector3(Random.Range(-halfPlayArea, halfPlayArea), 0, Random.Range(-halfPlayArea, halfPlayArea));
        }


        void Update () {
            if (!character.overrideControl) {
                CheckForArrival();
                TurnToDestination();

                if (character.currentSpeed == 0) {
                    //maybe run, maybe walk
                    character.SetMovementSpeed (Random.value < .5f ? 1 : 2);
                }
            }
        }

        void TurnToDestination () {
            Vector3 lookDir = destination - transform.position;
            lookDir.y = 0;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), turnSpeed);
        }

        
        void CheckForArrival () {
            if (Vector3.Distance(transform.position, destination) <= .5f) {

                Start();
                
            }
        }
    }
}
