using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll.Demo {

    /*
        spawns the characters in teh scene
        and rains cubes on top of them...
    */
    public class DemoSceneController : MonoBehaviour
    {

        public GameObject[] ammoScenes;

        void InitializeScenes () {
            for (int i =1 ; i < ammoScenes.Length; i++) {
                ammoScenes[i].SetActive(false);
            }
        }
        int activeScene;

        public void SwitchActiveScene() {
            ammoScenes[activeScene].SetActive(false);
            activeScene++;
            if (activeScene >= ammoScenes.Length) {
                activeScene = 0;
            }
            ammoScenes[activeScene].SetActive(true);
        }


        public float maxRainObjects = 10;
        public Vector2 rainSizeRange = new Vector2(1, 10);
        public GameObject rainObject;
        public float rainFrequency = 3;
        List<Transform> currentRain = new List<Transform>();

        public GameObject spawn;
        public int maxSpawn = 5;
        public float playRadius = 50;
        public float spawnFrequency = 3;
        int currentSpawned;
        float lastSpawn, lastRain;


        void Awake () {
            InitializeScenes();
        }
        void SpawnBot () {
            GameObject g = Instantiate(spawn, Vector3.zero, Quaternion.identity);
            g.GetComponentInChildren<AIControl>().playRadius = playRadius;
            currentSpawned++;
        }

        void Rain () {

            Vector3 position = new Vector3(Random.Range(-playRadius, playRadius), Random.Range(20, 40), Random.Range(-playRadius, playRadius));
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
                    
                    Rigidbody rb = t.GetComponent<Rigidbody>();
                    rb.mass = t.localScale.x * 100;
                    //fast movign object
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    
                }

            }
        }

        void Update()
        {
            if (currentSpawned < maxSpawn) {
                if (Time.time - lastSpawn >= spawnFrequency) {
                    // SpawnBot();
                    lastSpawn = Time.time;
                }
            }

            if (Time.time - lastRain >= rainFrequency) {
                // Rain();
                lastRain = Time.time;
            }
        }
    }
}
