using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;


public class PlayerRoomManager : UdonSharpBehaviour {
    public PlayerRoomTracker localTracker;

    private PlayerRoomTracker[] _trackers;
    private TumbleRoom[]        _rooms;

    void Start() {
        _trackers = GetComponentsInChildren<PlayerRoomTracker>();
        _rooms    = GetComponentsInChildren<TumbleRoom>();
    }

    private void FixedUpdate() {
        if (!Networking.LocalPlayer.isMaster) { return; }

        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        foreach (var player in players) {
            if (player == null) { continue; }

            var tracker = GetTracker(player);

            if (tracker == null) {
                tracker = FindAvailableTracker();

                if (tracker == null) {
                    Debug.LogError("No available trackers");
                    return;
                }

                Networking.SetOwner(player, tracker.gameObject);
                tracker.owned = true;
                if (player.isLocal) localTracker = tracker;
            }

            if (tracker.requestingRoom) {
                var room = FindAvailableRoom();
                room.roomType = tracker.requestedRoomType;
                room.SetRoomOwner(player);

                if (player.isLocal)
                    tracker.RoomRequestCompleted();
                else
                    tracker.SendCustomNetworkEvent(NetworkEventTarget.Owner, "RoomRequestCompleted");
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

    public void RequestRoom(RoomType type) {
        localTracker.requestingRoom    = true;
        localTracker.requestedRoomType = type;
    }
}