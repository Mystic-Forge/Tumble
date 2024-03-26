using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


public class ZeroGPose : ModifierPose {
    public override void OnApplyModifier(Structure structure) {
        structure.useGravity = !structure.useGravity;
        structure.velocity   = Vector3.zero;
    }
}