using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;


public class PlayerRoomManager : UdonSharpBehaviour {
    public PlayerRoomTracker localTracker;

    public GameObject editorMenu;

    private PlayerRoomTracker[] _trackers;
    private TumbleRoom[]        _rooms;
    
    public TumbleRoom LocalRoom => localTracker == null ? null : localTracker.currentRoom != -1 ? _rooms[localTracker.currentRoom] : null;

    private Universe _universe;
    
    void Start() {
        _universe = GetComponentInParent<Universe>();
        _trackers = GetComponentsInChildren<PlayerRoomTracker>();
        _rooms    = GetComponentsInChildren<TumbleRoom>();
    }

    private void FixedUpdate() {
        if (localTracker != null) editorMenu.gameObject.SetActive(localTracker.currentRoom != -1 && _rooms[localTracker.currentRoom].roomType == RoomType.Editor);

        if (!Networking.LocalPlayer.isMaster) return;

        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        foreach (var player in players) {
            if (player == null) continue;

            var tracker = GetTracker(player);

            if (tracker == null) {
                tracker = FindAvailableTracker();

                if (tracker == null) {
                    Debug.LogError("No available trackers");
                    return;
                }

                Networking.SetOwner(player, tracker.gameObject);
                tracker.owned = true;
                tracker.RequestSerialization();
                if (player.isLocal) localTracker = tracker;
            }

            if (tracker.requestingRoom) {
                var room = FindAvailableRoom();
                var endsInS = player.displayName.ToLower()[player.displayName.Length - 1] == 's';
                room.roomName = player.displayName + (endsInS ? "'" : "'s") + " Room";
                room.roomType = tracker.requestedRoomType;

                if (room.roomType == RoomType.Level) room.level.levelData = _universe.levelDatabase.GetLevel(tracker.requestingLevelId);
                room.SetRoomOwner(player);
                room.RequestSerialization();

                if (player.isLocal)
                    tracker.RoomRequestCompleted();
                else
                    tracker.SendCustomNetworkEvent(NetworkEventTarget.Owner, "RoomRequestCompleted");
                
                tracker.requestingRoom = false;
            }
        }
    }

    public PlayerRoomTracker GetTracker(VRCPlayerApi player) {
        foreach (var tracker in _trackers)
            if (tracker.owned && Networking.GetOwner(tracker.gameObject) == player)
                return tracker;

        return null;
    }

    public PlayerRoomTracker FindAvailableTracker() {
        foreach (var tracker in _trackers)
            if (!tracker.owned)
                return tracker;

        return null;
    }

    public TumbleRoom FindAvailableRoom() {
        foreach (var room in _rooms)
            if (room.roomType == RoomType.Vacant)
                return room;

        return null;
    }

    public void RequestRoom(RoomType type, string levelId = "") {
        localTracker.requestingRoom    = true;
        localTracker.requestedRoomType = type;
        localTracker.requestingLevelId = levelId;
        localTracker.RequestSerialization();
    }

    public TumbleRoom[] GetOpenRooms() {
        var openRoomArray = new TumbleRoom[_rooms.Length];
        var i             = 0;

        for (var j = 0; j < _rooms.Length; j++) {
            var room                                               = _rooms[j];
            if (room.roomType != RoomType.Vacant) openRoomArray[i++] = room;
        }

        var finalArray = new TumbleRoom[i];
        for (var j = 0; j < i; j++) finalArray[j] = openRoomArray[j];

        return finalArray;
    }
    
    public void LeaveCurrentRoom() {
        if (localTracker.currentRoom == -1) return;
        
        _rooms[localTracker.currentRoom].LeaveRoom();
    }
}