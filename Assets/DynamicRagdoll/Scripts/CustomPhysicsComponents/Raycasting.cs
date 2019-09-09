using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DynamicRagdoll {

    /*
        Raycast that organizes hits by distance, 
        then organizes hits, so multiple hits on the same ragdoll are contained within the same hit


        PhysicsHit is the container for a group of PhysicsHitElements 
        
        (PhysicsHitElement is basically the same as Unity's RaycastHit)
    */
    public class PhysicsHitElement {

        public bool hitStatic { get { return collider.gameObject.isStatic || rigidbody == null || rigidbody.isKinematic; } }
        public BoxCollider box { get { return collider as BoxCollider; } }
        public CapsuleCollider capsule { get { return collider as CapsuleCollider; } }
        public SphereCollider sphere { get { return collider as SphereCollider; } }

        public Ragdoll ragdoll { get { return ragdollBone != null ? ragdollBone.ragdoll : null; } }
        public Transform transform { get { return collider.transform; } }
        public Collider collider;
        public RagdollBone ragdollBone;
        public Rigidbody rigidbody;     
        public Vector3 hitPoint;
        public float hitDistance;
        
        public PhysicsHitElement (RaycastHit hit) {
            this.hitPoint = hit.point;
            this.collider = hit.collider;
            this.hitDistance = hit.distance;
            this.rigidbody = collider.attachedRigidbody;
            ragdollBone = collider.GetComponent<RagdollBone>();
        }
    }

    public class PhysicsHit {

        public Rigidbody mainRigidbody { get { return hitElements[0].rigidbody; } }
        public Collider mainCollider { get { return hitElements[0].collider; } }
        public Transform mainTransform { get { return hitElements[0].transform; } }


        public bool hitStatic { get { return !isRagdoll && hitElements[0].hitStatic; } }
        public List<PhysicsHitElement> hitElements = new List<PhysicsHitElement>();
        public bool isRagdoll { get { return ragdoll != null; } }
        public Ragdoll ragdoll { get { return hitElements[0].ragdoll; } }
        public Vector3 averagePosition;
        public float hitDistance;

        public PhysicsHit(List<PhysicsHitElement> hitElements) {
            this.hitElements = hitElements;
        }

        public void InitializeHit (Vector3 rayOrigin) {
            averagePosition = AveragePosition();
            hitDistance = Vector3.Distance(rayOrigin, averagePosition);
        }
        Vector3 AveragePosition () {
            if (hitElements.Count == 1) 
                return hitElements[0].hitPoint;
            
            Vector3 p = Vector3.zero;
            for (int i = 0; i < hitElements.Count; i++) {
                p += hitElements[i].hitPoint;
            }
            return p / hitElements.Count;
        }
    }

    public static partial class RagdollPhysics {

        public static void MakePhysicsHitsIgnoreCollider(List<PhysicsHit> physicsHits, Collider collider, List<ColliderIgnorePair> ignorePairsPopulate) {
            if (collider == null) return;
            for (int i = 0; i < physicsHits.Count; i++) {
                for (int x = 0; x < physicsHits[i].hitElements.Count; x++) {
                    ignorePairsPopulate.Add(new ColliderIgnorePair(collider, physicsHits[i].hitElements[x].collider));
                }
            }
        }
        public static void EndPhysicsHitsIgnoreCollider(List<ColliderIgnorePair> ignorePairsPopulate) {
            for (int i = 0; i < ignorePairsPopulate.Count; i++)
                ignorePairsPopulate[i].EndIgnore();
            ignorePairsPopulate.Clear();
        }

        public static PhysicsHit SphereCast (Ray ray, float radius, LayerMask shootMask, float maxDist) {
            RaycastHit hit;
            if (Physics.SphereCast(ray, radius, out hit, maxDist, shootMask, QueryTriggerInteraction.Ignore)) {
                PhysicsHit r = new PhysicsHit(new List<PhysicsHitElement>() { new PhysicsHitElement(hit) });
                r.InitializeHit(ray.origin);   
                return r;
            }
            return null;
        }

        static RaycastHit[] grabRayHits;
        public static List<PhysicsHit> SphereCastAll (Ray ray, float radius, LayerMask shootMask, float maxDist) {
            
            // shoot ray
            grabRayHits = Physics.SphereCastAll(ray, radius, maxDist, shootMask, QueryTriggerInteraction.Ignore);
            // sort from nearest to farthest
            System.Array.Sort(grabRayHits, (x,y) => x.distance.CompareTo(y.distance));

            List<PhysicsHit> allHits = new List<PhysicsHit>();
            
            // each ragdoll has it's own hit group, to avoid having multiple moving joints attached to 
            // ragdolls (this improves stability)
            Dictionary<Ragdoll, PhysicsHit> ragdoll2HitGroup = new Dictionary<Ragdoll, PhysicsHit>();
            
            for (int i = 0; i < grabRayHits.Length; i++) {
            
                RaycastHit hit = grabRayHits[i];
                PhysicsHitElement hitElement = new PhysicsHitElement(hit);

                if (hitElement.ragdollBone != null) {
                    if (ragdoll2HitGroup.ContainsKey(hitElement.ragdollBone.ragdoll)) {
                        ragdoll2HitGroup[hitElement.ragdollBone.ragdoll].hitElements.Add(hitElement);
                    }
                    else {
                        PhysicsHit newGroup = new PhysicsHit(new List<PhysicsHitElement>() { hitElement });
                        allHits.Add(newGroup);
                        ragdoll2HitGroup[hitElement.ragdollBone.ragdoll] = newGroup;
                    }               
                }
                else {
                    allHits.Add(new PhysicsHit(new List<PhysicsHitElement>() { hitElement }));
                }
            }
            
            // initialize the groups, (must be done after loop, so ragdoll groups are complete)
            for (int i = 0; i < allHits.Count; i++) {
                allHits[i].InitializeHit(ray.origin);
            }

            return allHits;
        }

    }
}
