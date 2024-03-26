using Nessie.Udon.Movement;

using UnityEngine;

using VRC.SDKBase;


public class DashPose : Pose {
    public  float   dashForce     = 3;
    public  float   dashDuration  = 0.5f;
    private float   _lastDashTime = 0;
    private Vector3 _dashDirection;

    private NUMovement _movement;

    public override bool BlockOtherPoses => Time.time - _lastDashTime < dashDuration;

    private void Start() { _movement = GetComponentInParent<Universe>().movement; }

    public override void OnPoseEnter() {
        var headTracking = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        _dashDirection = Vector3.ProjectOnPlane(headTracking.rotation * Vector3.forward, Vector3.up).normalized;

        _lastDashTime = Time.time;
        PoseUpdate();
    }

    public override void PoseUpdate() {
        var dashing = Time.time - _lastDashTime < dashDuration;
        _movement.forcePlayerUnGrounded = dashing;
        if(!dashing) return;

        GetComponentInParent<PoseManager>().groundReset = false;

        var velocity = _movement._GetVelocity();
        _movement._SetVelocity(new Vector3(_dashDirection.x * dashForce, velocity.y < 0 ? 0 : velocity.y, _dashDirection.z * dashForce));
    }
}