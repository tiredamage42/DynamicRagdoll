using System.Collections.Generic;
using UnityEngine;

namespace DynamicRagdoll {

    /*
        complex object made up of two rigidibodies, to easily facilitate
        
        joint motion that actually corresponds to the base transform
    */
    public class GrabPoint : MonoBehaviour {
        static PrefabPool<GrabPoint> pool = new PrefabPool<GrabPoint>();
        public static GrabPoint GetGrabPoint() {
            return pool.GetPrefabInstance(grabPointPrefab);
        }
        public static void ReleaseGrabPoint (GrabPoint grabPoint) {
            if (grabPoint != null) {
                RagdollPhysics.DetachRigidbody(grabPoint.baseRigidbody, null, true);
                ReesetGrabPoint(grabPoint);
                grabPoint.gameObject.SetActive(false);   
            }
        }

        static GrabPoint _grabPointPrefab;
        static GrabPoint grabPointPrefab {
            get {
                if (_grabPointPrefab == null) _grabPointPrefab = BuildDefaultGrabPoint();
                return _grabPointPrefab;
            }
        }


        static GrabPoint BuildDefaultGrabPoint () {

            GameObject baseG = new GameObject("RagdollPhysicsGrabPoint");
            GrabPoint p = baseG.AddComponent<GrabPoint>();

            p.baseRigidbody = baseG.AddComponent<Rigidbody>();
            p.childRigidbody = new GameObject("RagdollPhysicsGrabPoint2").AddComponent<Rigidbody>();
            p.baseJoint = p.childRigidbody.gameObject.AddComponent<ConfigurableJoint>();
            ReesetGrabPoint(p);
            return p;
        }

        static void ReesetGrabPoint (GrabPoint p) {
            // detach our grab point, if it was attached at all
            RagdollPhysics.DetachRigidbody(p.baseRigidbody, null, true);
            
            p.baseRigidbody.isKinematic = true;
            p.baseRigidbody.useGravity = false;

            p.childRigidbody.transform.SetParent(p.baseRigidbody.transform);
            p.childRigidbody.transform.localPosition = Vector3.zero;
            p.childRigidbody.transform.localRotation = Quaternion.identity;

            RagdollPhysics.MakeJointDefault ( p.baseJoint, freeRotation: false, Vector3.zero, Vector3.zero, p.baseRigidbody );
        }
        
        public Rigidbody baseRigidbody, childRigidbody;
        public ConfigurableJoint baseJoint;

        public void Detach () {
            RagdollPhysics.DetachRigidbody(baseRigidbody, null, true);
            ReesetGrabPoint(this);
            gameObject.SetActive(false);   
        }
    }

    public class GrabbedBody {
        
        public GrabPoint grabPoint;
        PhysicsHit physicsHit;
        List<ConfigurableJoint> connectedJoints = new List<ConfigurableJoint>();

        public void Detach () {
            grabPoint. Detach ();
            for (int x =0; x < physicsHit.hitElements.Count; x++) {
                RagdollPhysics.DetachRigidbody(physicsHit.hitElements[x].rigidbody, connectedJoints[x], false);
            }   
            connectedJoints.Clear();
        }

        public GrabbedBody(PhysicsHit physicsHit, Quaternion rotation){
            this.physicsHit = physicsHit;

            grabPoint = GrabPoint.GetGrabPoint();
            grabPoint.transform.position = physicsHit.averagePosition;
            grabPoint.transform.rotation = rotation;
                        
            for (int x =0; x < physicsHit.hitElements.Count; x++) {
                connectedJoints.Add(RagdollPhysics.GrabRigidbody(physicsHit.hitElements[x].rigidbody, grabPoint.childRigidbody, false));
            }
        }
    }

    public static partial class RagdollPhysics {

        public static void DetachGrabbedBodies (List<GrabbedBody> bodies) {
            for (int i = 0; i < bodies.Count; i++) {
                bodies[i].Detach();
            }
            bodies.Clear();
        }


        /*
            raycasts through until a static collider hit (or kinematic rigidbody)

            and "grabs" all the rigidbodies in between
        */
            
        public static Vector3 ShootMultiGrabRay (Ray ray, float radius, LayerMask shootMask, float maxDist, List<GrabbedBody> grabbedBodies, List<PhysicsHit> physicsHits, out float endPointDistance, out bool hitStatic) {
            grabbedBodies.Clear();
            physicsHits.Clear();
            hitStatic = false;

            Quaternion lookRotation = Quaternion.LookRotation(ray.direction);
            
            List<PhysicsHit> hits = SphereCastAll ( ray, radius, shootMask, maxDist);

            Vector3 endPoint = ray.origin + ray.direction * maxDist;

            endPointDistance = maxDist;
            
            // each ragdoll has it's own hit group, to avoid having multiple moving joints attached to 
            // ragdolls (this improves stability)
            for (int i = 0; i < hits.Count; i++) {
            
                PhysicsHit hit = hits[i];
                hitStatic = hit.hitStatic;
                if (!hitStatic) {
                    grabbedBodies.Add(new GrabbedBody(hit, lookRotation));
                    physicsHits.Add(hit);
                }
                // hit static, we're done
                else {
                    endPoint = hit.averagePosition;
                    endPointDistance = hit.hitDistance;
                    break;
                }
            }
            
            return endPoint;
        }

        public static ConfigurableJoint MakeJointDefault (ConfigurableJoint j, bool freeRotation, Vector3 connectedAnchor, Vector3 anchor, Rigidbody connectedBody) {
            j.autoConfigureConnectedAnchor = false;
            j.enablePreprocessing = false;
            j.breakForce = Mathf.Infinity;
            j.projectionMode = JointProjectionMode.PositionAndRotation;
            j.xMotion = j.yMotion = j.zMotion = ConfigurableJointMotion.Locked;
            j.angularXMotion = j.angularYMotion = j.angularZMotion = (freeRotation ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked);

            j.connectedAnchor = connectedAnchor;
            j.anchor = anchor;
            j.connectedBody = connectedBody;
            return j;
        }


        static Dictionary<int, ConfigurableJoint> grabbedRBs = new Dictionary<int, ConfigurableJoint>();

        public static ConfigurableJoint GrabRigidbody (Rigidbody rb, Rigidbody grabPoint, bool freeRotation) {
            return GrabRigidbody(rb, grabPoint, rb.transform.InverseTransformPoint(grabPoint.transform.position), freeRotation);
        }
        public static ConfigurableJoint GrabRigidbody (Rigidbody rb, Rigidbody grabPoint, Vector3 anchorOffset, bool freeRotation) {
            int id = rb.GetInstanceID();
            
            if (RigidbodyGrabbed(rb))
                DetachRigidbody(rb, grabbedRBs[id], false);

            // if we grabbed a ragdoll, make sure the controller (if any), goes ragdoll
            // with a full decay over all the bones
            RagdollBone grabbedBone = rb.GetComponent<RagdollBone>();
            if (grabbedBone) {
                if (grabbedBone.ragdoll.hasController) {
                    grabbedBone.ragdoll.controller.AddBoneDecay(1);
                    grabbedBone.ragdoll.controller.GoRagdoll("From grab");
                }
            }
                        
            ConfigurableJoint j = MakeJointDefault ( rb.gameObject.AddComponent<ConfigurableJoint>(), freeRotation, Vector3.zero, anchorOffset, grabPoint );
            
            grabbedRBs.Add(id, j);
            
            return j;
        }
        

        public static bool RigidbodyGrabbed (Rigidbody rb) {
            return grabbedRBs.ContainsKey(rb.GetInstanceID());
        }

        public static void DetachRigidbody (Rigidbody rb, Joint joint, bool forceDetach) {
            int id = rb.GetInstanceID();
            if (grabbedRBs.ContainsKey(id)) {
                if (grabbedRBs[id] == joint || forceDetach) {
                    MonoBehaviour.Destroy(grabbedRBs[id]);
                    grabbedRBs.Remove(id);
                }
            }
        }
    }
}
