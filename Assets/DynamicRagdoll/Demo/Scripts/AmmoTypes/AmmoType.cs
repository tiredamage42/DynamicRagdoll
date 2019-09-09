using UnityEngine;

namespace Game.Combat {


    public class AmmoTypeCoroutineRunner : MonoBehaviour { }
    public abstract class AmmoType : ScriptableObject
    {
        const string coroutineRunnerName = "__AmmoTypeCoroutineRunner__";
        static AmmoTypeCoroutineRunner _coroutineRunner;
        protected static AmmoTypeCoroutineRunner coroutineRunner {
            get {
                if (_coroutineRunner == null) {
                    _coroutineRunner = GameObject.FindObjectOfType<AmmoTypeCoroutineRunner>();
                    if (_coroutineRunner == null) {
                        _coroutineRunner = new GameObject(coroutineRunnerName).AddComponent<AmmoTypeCoroutineRunner>();
                    }
                }
                return _coroutineRunner;
            }
        }


        public float baseDamage = Mathf.Infinity;

        public abstract GameObject FireAmmo (IDamager damager, Ray damageRay, LayerMask layerMask, float damageMultiplier);
    }
}

