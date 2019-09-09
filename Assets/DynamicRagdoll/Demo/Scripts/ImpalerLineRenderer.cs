using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DynamicRagdoll;
public class ImpalerLineRenderer : MonoBehaviour
{
    LineRenderer lineRenderer;
    Impaler impaler;
    void Awake () {
        impaler = GetComponent<Impaler>();
        lineRenderer = GetComponent<LineRenderer>();
    }
    void LateUpdate () {
        lineRenderer.SetPosition(0, impaler.impalerOrigin);
        lineRenderer.SetPosition(1, impaler.currentImpalerEndPoint);
        lineRenderer.startWidth = impaler.impalerRadius * 2;
        lineRenderer.endWidth = impaler.impalerRadius * 2;
    }
}
