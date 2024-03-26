using Nessie.Udon.Movement;

using UnityEngine;


public class JumpPose : Pose {
    public  float jumpForce     = 3;
    public  float jumpDuration  = 0.5f;
    private float _lastJumpTime;

    private NUMovement _movement;

    private void Start() { _movement = GetComponentInParent<Universe>().movement; }

    public override void OnPoseEnter() {
        _lastJumpTime = Time.time;
        PoseUpdate();
    }

    public override void PoseUpdate() {
        if (Time.time - _lastJumpTime > jumpDuration) return;

        var velocity = _movement._GetVelocity();
        _movement._SetVelocity(new Vector3(velocity.x, jumpForce, velocity.z));
    }
}