using Nessie.Udon.Movement;

using UdonSharp;

using UnityEngine;
using UnityEngine.EventSystems;

using VRC.SDKBase;
using VRC.Udon.Common;

using Vector3 = UnityEngine.Vector3;


public class PoseManager : UdonSharpBehaviour {
    public float maxDistance = 0.1f;
    public float maxAngle    = 15f;
    public float height      = 1.6f;

    public float HeightRatio => height / 1.6f;

    public float HeightDeviation => height - 1.6f;

    private Pose[] _poses;
    private Pose   _activePose;
    private bool   _hasReset;
    public  bool   groundReset;

    public AudioSource poseHitSound;
    public AudioClip[] poseHitClips;

    private float _resetTime;

    private Universe   _universe;
    private NUMovement _movement;
    private float      _lastJumpTime = 0;
    private bool       _initialized;
    private bool       _desktopJumping;

    private void Start() {
        if (_initialized) return;

        _poses    = GetComponentsInChildren<Pose>(includeInactive: true);
        _universe = GetComponentInParent<Universe>();
        _movement = _universe.movement;
        foreach (var pose in _poses) pose.gameObject.SetActive(false);
        _initialized = true;
    }

    public override void PostLateUpdate() {
        if (_universe.BlockInputs || _universe.flyMovement.isActive || _movement.MenuOpen) return;
            
        height = Networking.LocalPlayer.GetAvatarEyeHeightAsMeters();
        foreach (var p in _poses) p.PoseUpdate();

        var grounded              = _movement._IsPlayerGrounded();
        if (grounded) groundReset = true;

        if (Time.time - _resetTime > 1) _hasReset = false;

        var headTracking   = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        var originTracking = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
        transform.position = new Vector3(headTracking.position.x, originTracking.position.y + 1.6f, headTracking.position.z);
        transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(headTracking.rotation * Vector3.forward, Vector3.up).normalized, Vector3.up);

        var newPose                  = GetActivePose(false);
        // if (newPose == null && Networking.LocalPlayer.IsUserInVR()) newPose = GetActivePose(true);
        _movement._SetJumpHeight(0);

        // Check for cancel
        var cancel                                                                       = false;
        if (Time.time - _lastJumpTime < 0.2f && !_universe.modifiers.NoCooldowns) cancel = true;

        if (!_universe.modifiers.NoCooldowns)
            foreach (var pose in _poses)
                if (pose.BlockOtherPoses) {
                    cancel = true;
                    break;
                }

        if (Networking.LocalPlayer.IsUserInVR() && !cancel) _movement._SetJumpHeight(2);

        if (_activePose != newPose) {
            if (_activePose != null) _activePose.OnPoseExit();
            _activePose = newPose;

            if (!cancel
                && _activePose != null
                && (Time.time - _activePose.lastPoseTime > 0.3f || _universe.modifiers.NoCooldowns)
                && (!_activePose.requiresReset || _hasReset)
               ) {
                var passed = false;

                if (_activePose.groundResetMode == GroundResetMode.Always)
                    passed = true;
                else if (_activePose.groundResetMode == GroundResetMode.WhenGrounded && grounded)
                    passed = true;
                else if (_activePose.groundResetMode == GroundResetMode.WhenHasGroundReset && (groundReset || _universe.modifiers.InfiniteAirActions))
                    passed                                                                                   = true;
                else if (_activePose.groundResetMode == GroundResetMode.WhenNotGrounded && !grounded) passed = true;

                if (passed) {
                    if (_activePose.name == "jump" || _activePose.name == "dash") {
                        _lastJumpTime = Time.time;
                        _movement._SetJumpHeight(0);
                    }

                    _activePose.OnPoseEnter();
                    _activePose.lastPoseTime = Time.time;
                    _hasReset                = false;
                    Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Left,  0.2f, 0.3f, 0.1f);
                    Networking.LocalPlayer.PlayHapticEventInHand(VRC_Pickup.PickupHand.Right, 0.2f, 0.3f, 0.1f);

                    poseHitSound.clip = poseHitClips[Random.Range(0, poseHitClips.Length)];
                    poseHitSound.Play();

                    if (_activePose.groundResetMode == GroundResetMode.WhenHasGroundReset) groundReset = false;
                }
            }
        }

        if (_activePose != null && _activePose.gameObject.name == "reset") {
            _hasReset  = true;
            _resetTime = Time.time;
        }

        // if (_activePose != null) _activePose.gameObject.SetActive(true);
    }

    public Pose GetPose(string poseName) {
        Start();

        foreach (var pose in _poses)
            if (pose.name == poseName)
                return pose;

        return null;
    }

    public override void InputJump(bool value, UdonInputEventArgs args) {
        if (!Networking.LocalPlayer.IsUserInVR()) {
            _desktopJumping = value;
            return;
        }

        groundReset   = false;
        _lastJumpTime = Time.time;
    }

    private Pose GetActivePose(bool flip) {
        var leftHandTracking  = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
        var rightHandTracking = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);

        foreach (var pose in _poses) {
            if (Input.GetKey(pose.poseKey)) return pose;

            if (pose.name == "jump" && _desktopJumping) return pose;

            pose.transform.localPosition = Vector3.up * HeightDeviation;
            pose.transform.localScale    = new Vector3(flip ? -1 : 1, 1, 1);

            var leftHandDistance = Vector3.Distance(pose.transform.TransformPoint(pose.leftHand.localPosition * HeightRatio), leftHandTracking.position);
            var leftHandAngle    = Quaternion.Angle(pose.leftHand.rotation, leftHandTracking.rotation);

            var rightHandDistance = Vector3.Distance(pose.transform.TransformPoint(pose.rightHand.localPosition * HeightRatio), rightHandTracking.position);
            var rightHandAngle    = Quaternion.Angle(pose.rightHand.rotation, rightHandTracking.rotation);

            pose.transform.localPosition = Vector3.zero;
            pose.transform.localScale    = Vector3.one;

            if (leftHandDistance < maxDistance * HeightRatio && leftHandAngle < maxAngle && rightHandDistance < maxDistance * HeightRatio && rightHandAngle < maxAngle) return pose;
        }

        return null;
    }
}