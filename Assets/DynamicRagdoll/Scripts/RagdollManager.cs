using UnityEngine;

namespace DynamicRagdoll {

    public class RagdollManager : MonoBehaviour
    {   
        [Header("Unity Default: 6")]
        public int defaultSolverIterations = 10;
        public float fixedTimeStepMultiplier = 2.0f;
    
        void Awake () {
            Physics.defaultSolverIterations = defaultSolverIterations;

            DynamicRagdoll.Demo.GameTime.SetFixedTimeStep(DynamicRagdoll.Demo.GameTime.fixedTimeStep / fixedTimeStepMultiplier);
        }
    }
}
