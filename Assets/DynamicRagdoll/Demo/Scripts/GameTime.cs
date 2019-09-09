using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DynamicRagdoll.Demo {


    [System.Serializable] public class GameTimeParameters {
        
        [Range(0,2)] public float timeDilation = 1.0f;

        [Header("Unity Default: .02")]
        public float fixedTimeStep = .02f;
        [Header("Unit Default: .1")]
        public float maxTimeStep = .1f;
    }

    public class GameTime : MonoBehaviour
    {
        public static T GetInstance <T> (ref T instance) where T : Component {
            if (instance == null) {
                instance = GameObject.FindObjectOfType<T>();
                if (instance == null) {
                    Debug.LogError("No " + typeof(T).ToString() + " instance in the scene");
                }
            }
            return instance;
        }

        
        static GameTime _instance;
        public static GameTime instance { get { return GetInstance<GameTime>(ref _instance); } }
        public GameTimeParameters parameters;  

        public static float timeDilation { get { return instance.parameters.timeDilation; } }
        public static float fixedTimeStep { get { return instance.parameters.fixedTimeStep; } }


        void Start () {
            UpdateScales(parameters.timeDilation);
        }          

        void Update () {
            if (timeDilationInProgress) {
                UpdateTimeDilation(Time.unscaledDeltaTime);
            }
            UpdateScales(parameters.timeDilation);
        }

        public static bool timeDilated { get { return timeDilationInProgress; } }

        static bool timeDilationInProgress;
        static Vector3 timeDilationSpeeds;
        static int timeDilationPhase;
        static float originalTimeDilation, timeDilationTarget;
        
        static void UpdateScales (float dilation) {
            Time.timeScale = dilation;
            Time.fixedDeltaTime = instance.parameters.fixedTimeStep * dilation;
            Time.maximumDeltaTime = instance.parameters.maxTimeStep * dilation;  
		}

        public static void ResetTimeDilation (float speed = 0) {
            if (timeDilationInProgress) {
                timeDilationSpeeds = new Vector3(speed, speed, speed);
                EndTimeDilationPhase(2);
            }
        }
        
        //set duration < 0 for permanent time dilation
        public static void SetTimeDilation (float timeDilation, float beginTime, float duration, float endTime) {
            if (timeDilationInProgress) {
                Debug.LogWarning("Already using time dilation");
                return;
            }
            timeDilationInProgress = true;
            timeDilationPhase = 0;
            timeT = 0;

            timeDilationSpeeds = new Vector3(beginTime, duration, endTime);
            timeDilationTarget = timeDilation;

            originalTimeDilation = instance.parameters.timeDilation;
        }

        public static void SetFixedTimeStep (float fixedTimeStep) {
            instance.parameters.fixedTimeStep = fixedTimeStep;
            UpdateScales(instance.parameters.timeDilation);
        }
        // static float timeV, timeT;
        static float timeT;
        
        static bool SmoothTime (float orig, float target, float unscaledDeltaTime){//, float epsilonThreshold = .999f) {
            if (timeDilationSpeeds[timeDilationPhase] <= 0) {
                timeT = 1.0f;
            }
            else {
                timeT += unscaledDeltaTime * (1.0f / timeDilationSpeeds[timeDilationPhase]);
                if (timeT > 1.0f) {
                    timeT = 1.0f;
                }
                // timeT = Mathf.SmoothDamp(timeT, 1.0f, ref timeV, timeDilationSpeeds[timeDilationPhase]);
                // if (timeT >= epsilonThreshold) {
                //     timeT = 1.0f;
                // }
            }
            instance.parameters.timeDilation = Mathf.Lerp(orig, target, timeT);
            return timeT >= 1.0f;
        }


        static void EndTimeDilationPhase(int nextPhase) {
            instance.parameters.timeDilation = timeDilationTarget;
            timeDilationPhase = nextPhase;
            timeT = 0;
        }

        static void EndTimeDilation () {
            instance.parameters.timeDilation = originalTimeDilation;
            timeDilationInProgress = false;
        }


        public static void UpdateTimeDilation (float unscaledDeltaTime) {
            //going towards time dilation
            if (timeDilationPhase == 0) {
                if (SmoothTime (originalTimeDilation, timeDilationTarget, unscaledDeltaTime)) {
                    EndTimeDilationPhase(1);
                }        
            }
            //countint duration
            else if (timeDilationPhase == 1) {
                // anything less than 0 is permanenet
                if (timeDilationSpeeds[timeDilationPhase] >= 0) {
                    timeT += unscaledDeltaTime;
                    if (timeT >= timeDilationSpeeds[timeDilationPhase]) {
                        EndTimeDilationPhase(2);
                    }
                }
            }
            //going to normal
            else if (timeDilationPhase == 2) {
                if (SmoothTime (timeDilationTarget, originalTimeDilation, unscaledDeltaTime)) {
                    EndTimeDilation();
                }
            }
        }
    }

}