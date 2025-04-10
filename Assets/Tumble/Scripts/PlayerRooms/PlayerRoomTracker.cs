using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class PlayerRoomTracker : TumbleBehaviour {
    [UdonSynced] private int _currentRoom = -1;

    [UdonSynced] public bool owned;

    [UdonSynced] public bool     requestingRoom;
    [UdonSynced] public RoomType requestedRoomType;
    [UdonSynced] public string   requestingLevelId;

    public TumbleRoom Room => _currentRoom == -1 ? null : Universe.playerRoomManager.GetRoom(_currentRoom);
    
    public override void OnOwnershipTransferred(VRCPlayerApi player) {
        if (!player.isLocal) return;

        owned = !Networking.LocalPlayer.isMaster;
        RequestSerialization();

        if (owned) Universe.playerRoomManager.localTracker = this;
    }
    
    public void RoomRequestCompleted() {
        requestingRoom = false;
        
        if(Networking.GetOwner(gameObject).isLocal) RequestSerialization();
    }
    
    public void SetRoom(int roomIndex) {
        _currentRoom = roomIndex;
        if(roomIndex != -1)
            Universe.BroadcastCustomEvent("EventRoomUsersUpdated");
        RequestSerialization();
    }

    public override void OnDeserialization(DeserializationResult result) {
        if(LocalRoom == null) return;
        if (_currentRoom == LocalRoom.Index) Universe.BroadcastCustomEvent("EventRoomUsersUpdated");
    }

    private void FixedUpdate() {
        var room = LocalRoom;
        if (room == null) return;

        // Respawn player if any of their coordinates exceed the room bounds
        var position = Universe.movement._GetPosition();
        position -= room.transform.position;
        if (position.x < room.bounds.min.x || position.x > room.bounds.max.x ||
            position.y < room.bounds.min.y || position.y > room.bounds.max.y ||
            position.z < room.bounds.min.z || position.z > room.bounds.max.z) {
            room.TeleportToRoom();
        }
    }
}