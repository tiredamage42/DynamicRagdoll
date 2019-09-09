using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {

    public class PrefabPool<T> where T : MonoBehaviour
    {
        
        Dictionary<int, List<T>> pool = new Dictionary<int, List<T>>();

        T GetAvaialableInstance (List<T> allInstances) {
            for (int i =0 ; i < allInstances.Count; i++) {
                if (!allInstances[i].gameObject.activeSelf) {
                    allInstances[i].gameObject.SetActive(true);
                    allInstances[i].transform.SetParent(null);
                    return allInstances[i];
                }
            }
            return null;
        }
        T MakeNewInstance (List<T> allInstances, T prefab) {
            T s = GameObject.Instantiate(prefab);
            allInstances.Add(s);
            return s;
        }

        public T GetPrefabInstance (T prefab) {
            int id = prefab.GetInstanceID();
            
            List<T> allInstances;
            if (pool.ContainsKey(id)) {
                allInstances = pool[id];
            }
            else {
                allInstances = new List<T>();
                pool[id] = allInstances;
            }
            T r = GetAvaialableInstance(allInstances);
            if (r == null) r = MakeNewInstance (allInstances, prefab);
            return r;
        }
    }
}
