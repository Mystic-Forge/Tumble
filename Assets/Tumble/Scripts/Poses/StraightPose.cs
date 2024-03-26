using UnityEngine;

using VRC.SDKBase;


public class StraightPose : ModifierPose {
    public Vector3 force;

    public override void OnApplyModifier(Structure structure) {
        var globalForce = transform.TransformVector(force);
        structure.velocity += globalForce / structure.mass;
    }
}