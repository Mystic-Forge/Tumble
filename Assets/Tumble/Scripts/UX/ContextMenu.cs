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

    public bool Open => holder.activeSelf;

    private void Update() {
        // Desktop mode show
        if (Input.GetKeyDown(KeyCode.Tab)) {
            if (Open)
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
    }

    private void HideMenu() { holder.SetActive(false); }

    public override void InputLookVertical(float value, UdonInputEventArgs args) {
        if (!Networking.LocalPlayer.IsUserInVR()) return;

        if (value > 0.5f && !Open)
                ShowMenu();
        else if (value < -0.5f && Open)
                HideMenu();
    }
}