using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;


public class Pose : UdonSharpBehaviour {
    public bool requiresReset;
    public Transform leftHand;
    public Transform rightHand;
    public float lastPoseTime;
    public GroundResetMode groundResetMode;
    
    public KeyCode poseKey = KeyCode.BackQuote;
    
    public virtual bool BlockOtherPoses => false;

    public virtual void PoseUpdate() {
        
    }
    
    public virtual void OnPoseEnter() {
        
    }
    
    public virtual void OnPoseExit() {
        
    }
}

public enum GroundResetMode {
    Always,
    WhenGrounded,
    WhenHasGroundReset,
    WhenNotGrounded,
}
