using System;

using Nessie.Udon.Movement;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDKBase;
using VRC.Udon.Common;


namespace Tumble.Scripts {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FlyController : UdonSharpBehaviour {
        public bool  isActive;
        public float flySpeed = 5;

        private float _horizontalInput;
        private float _verticalInput;
        private bool  _upInput;

        private Universe     _universe;
        private VRCPlayerApi _player;
        private NUMovement   _movement;

        private Vector3 _lastPosition;

        private void Start() {
            _universe = GetComponentInParent<Universe>();
            _player   = Networking.LocalPlayer;
            _movement = _universe.movement;
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.P)) {
                isActive = !isActive;
                _movement._SetGravityStrength(isActive ? 0 : 1);
                _lastPosition = _movement._GetPosition();
            }
        }

        private void FixedUpdate() {
            if (!isActive || _universe.BlockInputs) return;

            var position = _lastPosition;

            var headTracking = _player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            var input        = new Vector3(_horizontalInput, _upInput ? 1 : 0, _verticalInput).normalized * flySpeed;
            var moveVector   = headTracking.rotation * input;

            position += moveVector * Time.fixedDeltaTime;

            _movement._SetPosition(position);
            _movement._SetVelocity(Vector3.zero);

            _lastPosition = position;
        }

        public override void OnPlayerRespawn(VRCPlayerApi player) {
            if (!player.isLocal) return;

            _lastPosition = _movement._GetPosition();
        }

        public override void InputMoveHorizontal(float value, UdonInputEventArgs args) { _horizontalInput = value; }

        public override void InputMoveVertical(float value, UdonInputEventArgs args) { _verticalInput = value; }

        public override void InputJump(bool value, UdonInputEventArgs args) { _upInput = value; }
    }
}