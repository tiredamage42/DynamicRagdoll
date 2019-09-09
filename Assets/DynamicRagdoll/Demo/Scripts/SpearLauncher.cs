using System.Collections;
using System.Collections.Generic;
using UnityEngine;


using Game.Combat;
using DynamicRagdoll;


public class SpearLauncher : MonoBehaviour
{

    public Vector2 shootArea = new Vector2(10, 5);
    public int buckShot = 10;
    public LayerMask shootMask;
    public float damageMultiplier = 1;
    public AmmoType ammoType;
    
    
    public float launchFrequency = 1;
    public float launchDelay = 1;

    GameObject[] firedAmmoInstances;
    public void FireWeapon () {


        if (ammoType != null) {
            firedAmmoInstances = new GameObject[buckShot];
            
            for (int i =0 ; i < buckShot; i++) {
                Vector3 firePosition = GetRandomLaunchPoint();
                Vector3 fireDirection = transform.forward;

                firedAmmoInstances[i] = ammoType.FireAmmo(null, new Ray(firePosition, fireDirection), shootMask, damageMultiplier);
            }
        }
    }

    Vector3 GetRandomLaunchPoint () {
        if (shootArea == Vector2.zero) {
            return transform.position;
        }
        return transform.TransformPoint(new Vector3(Random.Range(-shootArea.x*.5f, shootArea.x*.5f), Random.Range(-shootArea.y*.5f, shootArea.y*.5f), 0));
    }
    
        
    void OnEnable () {
        StartCoroutine(LaunchSequence());
    }

    IEnumerator LaunchSequence () {
        yield return new WaitForSeconds(launchDelay);
        while (true) {
            FireWeapon();
            yield return new WaitForSeconds(launchFrequency);
        }
    }

    void OnDrawGizmos () {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-shootArea.x*.5f, -shootArea.y*.5f, 0)), transform.TransformPoint(new Vector3(shootArea.x*.5f, -shootArea.y*.5f, 0)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-shootArea.x*.5f, shootArea.y*.5f, 0)), transform.TransformPoint(new Vector3(shootArea.x*.5f, shootArea.y*.5f, 0)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(-shootArea.x*.5f, -shootArea.y*.5f, 0)), transform.TransformPoint(new Vector3(-shootArea.x*.5f, shootArea.y*.5f, 0)));
        Gizmos.DrawLine(transform.TransformPoint(new Vector3(shootArea.x*.5f, -shootArea.y*.5f, 0)), transform.TransformPoint(new Vector3(shootArea.x*.5f, shootArea.y*.5f, 0)));
    }
}