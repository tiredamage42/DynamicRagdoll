using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll.Demo {
    public class DemoSceneController : MonoBehaviour
    {
        public float gravityModifier = 2;
        public float maxRainObjects = 10;
        public Vector2 rainSizeRange = new Vector2(1, 10);
        public GameObject rainObject;
        public float rainFrequency = 3;
        List<Transform> currentRain = new List<Transform>();

        public GameObject spawn;
        public int maxSpawn = 5;
        public float playArea = 50;
        public float spawnFrequency = 3;
        int currentSpawned;
        float lastSpawn, lastRain;

        void Awake () {
            Physics.gravity *= gravityModifier;
        }

        void SpawnBot () {
            GameObject g = Instantiate(spawn, Vector3.zero, Quaternion.identity);
            g.GetComponentInChildren<AIControl>().playArea = playArea;
            currentSpawned++;
        }

        void Rain () {

            float halfPlayArea = playArea * .5f;  
            Vector3 position = new Vector3(Random.Range(-halfPlayArea, halfPlayArea), Random.Range(20, 40), Random.Range(-halfPlayArea, halfPlayArea));
            Quaternion rotation = Quaternion.Euler(Random.value * 360f, Random.value * 360f, Random.value * 360f);
            
            if (currentRain.Count < maxRainObjects) {
                currentRain.Add(Instantiate(rainObject, position, rotation).transform);
            }
            else {
                if (maxRainObjects > 0) {

                    Transform t = currentRain[Random.Range(0, currentRain.Count)];
                    t.position = position;
                    t.rotation = rotation;

                    t.localScale = Vector3.one * Random.Range(rainSizeRange.x, rainSizeRange.y);
                    t.GetComponent<Rigidbody>().mass = t.localScale.x * 100;
                    
                    //fast movign object
                    t.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    
                }

            }
        }

        void Update()
        {
            if (currentSpawned < maxSpawn) {
                if (Time.time - lastSpawn >= spawnFrequency) {
                    SpawnBot();
                    lastSpawn = Time.time;
                }
            }

            if (Time.time - lastRain >= rainFrequency) {
                Rain();
                lastRain = Time.time;
            }
        }
    }
}
