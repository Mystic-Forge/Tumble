using Nessie.Udon.Movement;

using UnityEngine;


public class JumpPose : Pose {
    public  float jumpForce     = 3;
    public  float jumpDuration  = 0.5f;
    
    public AudioSource jumpSound;
    public AudioSource airJumpSound;

    private NUMovement _movement;
    private float _lastJumpTime;

    private void Start() { _movement = GetComponentInParent<Universe>().movement; }

    public override void OnPoseEnter() {
        if (_movement._IsPlayerGrounded())
            jumpSound.Play();
        else
            airJumpSound.Play();

        _lastJumpTime = Time.time;
        PoseUpdate();
    }

    public override void PoseUpdate() {
        if (Time.time - _lastJumpTime > jumpDuration) return;

        var velocity = _movement._GetVelocity();
        _movement._SetVelocity(new Vector3(velocity.x, jumpForce, velocity.z));
    }
}