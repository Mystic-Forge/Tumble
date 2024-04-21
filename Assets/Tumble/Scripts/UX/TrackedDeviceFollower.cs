using UnityEngine;
using VRC.SDKBase;

namespace UdonSharp.Examples.Utilities
{
    /// <summary>
    /// Follows one of the chosen playerApi tracking targets
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class TrackedDeviceFollower : UdonSharpBehaviour 
    {
        public VRCPlayerApi.TrackingDataType trackingTarget;
        public bool                       followPosition = true;
        public bool                       followRotation = true;

        VRCPlayerApi playerApi;
        bool         isInEditor;

        private void Start()
        {
            playerApi  = Networking.LocalPlayer;
            isInEditor = playerApi == null; // PlayerApi will be null in editor
        }

        public override void PostLateUpdate()
        {
            // PlayerApi data will only be valid in game so we don't run the update if we're in editor
            if (isInEditor)
                return;

            VRCPlayerApi.TrackingData trackingData = playerApi.GetTrackingData(trackingTarget);
            if(followPosition)
                transform.position = trackingData.position;
            if(followRotation)
                transform.rotation = trackingData.rotation;
            
        }
    }
}