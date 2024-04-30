using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerRoomTracker : UdonSharpBehaviour {
    [UdonSynced] public int currentRoom = -1;

    [UdonSynced] public bool owned;

    [UdonSynced] public bool     requestingRoom;
    [UdonSynced] public RoomType requestedRoomType;
    [UdonSynced] public string   requestingLevelId;

    private PlayerRoomManager _manager;
    private Universe         _universe;

    private void Start() {
        _universe = GetComponentInParent<Universe>();
        _manager = GetComponentInParent<PlayerRoomManager>();
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player) {
        if (!player.isLocal) return;

        owned = !Networking.LocalPlayer.isMaster;
        RequestSerialization();

        if (owned) _manager.localTracker = this;
    }
    
    public void RoomRequestCompleted() {
        requestingRoom = false;
        
        if(Networking.GetOwner(gameObject).isLocal) RequestSerialization();
    }

    private void FixedUpdate() {
        var room = _manager.LocalRoom;
        if (room == null) return;

        // Respawn player if any of their coordinates exceed the room bounds
        var position = _universe.movement._GetPosition();
        position -= room.transform.position;
        if (position.x < room.bounds.min.x || position.x > room.bounds.max.x ||
            position.y < room.bounds.min.y || position.y > room.bounds.max.y ||
            position.z < room.bounds.min.z || position.z > room.bounds.max.z) {
            room.TeleportToRoom();
        }
    }
}