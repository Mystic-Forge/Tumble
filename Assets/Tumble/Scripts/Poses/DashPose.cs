using Nessie.Udon.Movement;

using UnityEngine;

using VRC.SDKBase;


public class DashPose : Pose {
    public  float   dashForce     = 3;
    public  float   dashDuration  = 0.5f;

    public AudioSource dashSound;

    private NUMovement _movement;
    private Vector3 _dashDirection;
    private float   _lastDashTime;

    public override bool BlockOtherPoses => Time.time - _lastDashTime < dashDuration;

    public override void OnPoseEnter() {
        var headTracking = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        _dashDirection = Vector3.ProjectOnPlane(headTracking.rotation * Vector3.forward, Vector3.up).normalized;
        
        dashSound.Play();

        _lastDashTime = Time.time;
        PoseUpdate();
    }

    public override void PoseUpdate() {
        if(_movement == null) _movement = GetComponentInParent<Universe>().movement;
        
        var dashing                     = Time.time - _lastDashTime < dashDuration;
        _movement.forcePlayerUnGrounded = dashing;
        if(!dashing) return;

        GetComponentInParent<PoseManager>().groundReset = false;

        var velocity = _movement._GetVelocity();
        _movement._SetVelocity(new Vector3(_dashDirection.x * dashForce, velocity.y < 0 ? 0 : velocity.y, _dashDirection.z * dashForce));
    }
}