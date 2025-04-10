
using UdonSharp;
using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AxisGizmo : LevelEditorGizmo {
    public int min;
    public int max;

    public bool grabbing;
    public int lastValue;
    
    // Returns t which represents the distance along v1 from p1 to the intersection point
    private float LineLineIntersection(Vector3 p1, Vector3 v1, Vector3 p2, Vector3 v2) {
        var v3 = p2 - p1;
        var v4 = Vector3.Cross(v1, v2);
        var v5 = Vector3.Cross(v3, v2);
        return Vector3.Dot(v5, v4) / v4.sqrMagnitude;
    }

    public override void OnClickGizmo(Ray ray, Vector3 point) {
        grabbing  = true;
                
        lastValue = GetDistance(ray);
    }
    
    public override void OnDragGizmo(Ray ray) {
        if (!grabbing) return;
        
        var dist = GetDistance(ray);
        var delta = dist - lastValue;
        lastValue = dist;
        
        transform.localPosition += transform.forward * delta;
    }

    private int GetDistance(Ray ray) {
        var t     = LineLineIntersection(transform.parent.position, transform.forward, ray.origin, ray.direction);
        var value = Mathf.Clamp(Mathf.RoundToInt(t), min, max);
        return value;
    }
    
    public override void OnReleaseGizmo(Ray ray) {
        grabbing = false;
    }
}
