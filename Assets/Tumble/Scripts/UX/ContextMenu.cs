using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class ContextMenu : UdonSharpBehaviour {
    public GameObject holder;
    public Transform  openPosition;

    public bool IsOpen => holder.activeSelf;
    
    private Universe _universe;
    
    private void Start() { _universe = GetComponentInParent<Universe>(); }

    private void Update() {
        // Desktop mode show
        if (Input.GetKeyDown(KeyCode.E)) {
            if (IsOpen)
                HideMenu();
            else
                ShowMenu();
        }
    }
    
    public override void PostLateUpdate()
    {
        var trackingData = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
        transform.position = trackingData.position;
        if(Networking.LocalPlayer.IsUserInVR())
            transform.rotation = trackingData.rotation;
    }

    private void ShowMenu() {
        holder.SetActive(true);
        holder.transform.position = openPosition.position;
        holder.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(openPosition.forward, Vector3.up), Vector3.up);
        
        _universe.BroadcastCustomEvent("EventShowContextMenu");
    }

    private void HideMenu() {
        holder.SetActive(false); 
        _universe.BroadcastCustomEvent("EventHideContextMenu");
    }

    public override void InputLookVertical(float value, UdonInputEventArgs args) {
        if (!Networking.LocalPlayer.IsUserInVR()) return;

        if (value > 0.5f && !IsOpen)
                ShowMenu();
        else if (value < -0.5f && IsOpen)
                HideMenu();
    }
}