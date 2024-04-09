using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


public class PlayerRoomTracker : UdonSharpBehaviour {
    [UdonSynced] public int currentRoom = -1;

    [UdonSynced] public bool owned;

    [UdonSynced] public bool     requestingRoom;
    [UdonSynced] public RoomType requestedRoomType;

    private PlayerRoomManager _manager;

    private void Start() { _manager = GetComponentInParent<PlayerRoomManager>(); }

    public override void OnOwnershipTransferred(VRCPlayerApi player) {
        if (!player.isLocal) return;

        owned = !Networking.LocalPlayer.isMaster;

        if (owned) _manager.localTracker = this;
    }
    
    public void RoomRequestCompleted() {
        requestingRoom = false;
    }
}