using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{

    public float speed = 5;
    public float distance = 5;
    
    // Update is called once per frame
    void Update()
    {
        Vector3 pos = transform.position;
        transform.position = new Vector3(Mathf.Sin(Time.time * speed) * pos.y, pos.z);
    }
}
