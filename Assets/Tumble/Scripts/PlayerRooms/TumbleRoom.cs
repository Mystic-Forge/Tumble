using System.Globalization;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TumbleRoom : SyncedTumbleBehaviour {
    [UdonSynced] public string    roomName;
    public              Transform spawnPoint;
    [UdonSynced] public RoomType  roomType = RoomType.Level;

    public TumbleLevel level;
    
    public Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 256);
    
    public int Index => transform.GetSiblingIndex();

    public void JoinRoom() {
        var localTracker = Universe.playerRoomManager.localTracker;
        if (localTracker == null) return;

        localTracker.SetRoom(Index);
        Universe.spawnPoint.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        TeleportToRoom();

        LoadRoom();
    }

    public void TeleportToRoom() {
        Universe.movement._TeleportTo(spawnPoint.position, spawnPoint.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint);
    }

    public void LeaveRoom() {
        var localTracker = Universe.playerRoomManager.localTracker;
        if (localTracker == null) return;

        localTracker.SetRoom(-1);
        Universe.spawnPoint.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // Tentative until we get a world spawn location
        Universe.movement._TeleportTo(Universe.spawnPoint.position, Universe.spawnPoint.rotation);

        if (roomType == RoomType.Editor) Universe.levelEditor.level = null;
        
        Universe.BroadcastCustomEvent("EventRoomUnloaded");
    }

    private void LoadRoom() {
        if (!level.levelLoaded) {
            if (roomType == RoomType.Editor) level.LoadLevelFromRaw();
            else level.LoadLevel();
        }

        if (roomType == RoomType.Editor) Universe.levelEditor.level = level;
        
        Universe.BroadcastCustomEvent("EventRoomLoaded");
        Universe.BroadcastCustomEvent("EventRoomOwnerChanged"); // Redundant call to ensure any systems that rely on room owner are updated
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player) {
        if(LocalRoom == this) 
            Universe.BroadcastCustomEvent("EventRoomOwnerChanged");
    }

    public void SetRoomOwner(VRCPlayerApi player) {
        SetOwner(player);
        level.SetOwner(player);
    }

    public override void OnBecameOwner() {
        base.OnBecameOwner();

        if (Universe.playerRoomManager.localTracker.requestingRoom) {
            JoinRoom();
            Universe.playerRoomManager.localTracker.RoomRequestCompleted();
        }
    }

    public VRCPlayerApi[] GetPlayers() {
        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        
        var playersInRoom = new VRCPlayerApi[players.Length];
        var i             = 0;

        foreach (var p in players) {
            var tracker = Universe.playerRoomManager.GetTracker(p);
            if (tracker == null) continue;
            if (tracker.Room != this) continue;
            playersInRoom[i++] = p;
        }

        var finalPlayers = new VRCPlayerApi[i];
        for (var j = 0; j < i; j++) finalPlayers[j] = playersInRoom[j];
        
        return finalPlayers;
    }
}

public enum RoomType {
    Vacant,
    Level,
    Editor,
}