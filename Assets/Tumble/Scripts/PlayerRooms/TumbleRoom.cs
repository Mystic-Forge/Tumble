using System.Globalization;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TumbleRoom : UdonSharpBehaviour {
    [UdonSynced] public string    roomName;
    public              Transform spawnPoint;
    [UdonSynced] public RoomType  roomType = RoomType.Level;
    [UdonSynced] public string    roomOwner;

    public TumbleLevel level;
    
    public bool LocalIsRoomOwner => Networking.LocalPlayer.displayName == roomOwner;
    
    public Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 256);

    private PlayerRoomManager _roomManager;
    private Universe          _universe;

    private void Start() {
        _universe    = GetComponentInParent<Universe>();
        _roomManager = GetComponentInParent<PlayerRoomManager>();
        
        if(level != null)
            level.room = this;
    }

    public void JoinRoom() {
        var localTracker = _roomManager.localTracker;
        if (localTracker == null) return;

        localTracker.currentRoom = transform.GetSiblingIndex();
        localTracker.RequestSerialization();
        _universe.spawnPoint.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        TeleportToRoom();

        LoadRoom();
    }

    public void TeleportToRoom() {
        _universe.movement._TeleportTo(spawnPoint.position, spawnPoint.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint);
    }

    public void LeaveRoom() {
        var localTracker = _roomManager.localTracker;
        if (localTracker == null) return;

        localTracker.currentRoom = -1;
        localTracker.RequestSerialization();
        _universe.spawnPoint.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // Tentative until we get a world spawn location
        _universe.movement._TeleportTo(_universe.spawnPoint.position, _universe.spawnPoint.rotation);

        if (roomType == RoomType.Editor) {
            _universe.levelEditor.level = null;
        }
    }

    private void LoadRoom() {
        if (!level.levelLoaded) {
            if (roomType == RoomType.Editor) level.LoadLevelFromRaw();
            else level.LoadLevel();
        }

        if (roomType == RoomType.Editor) _universe.levelEditor.level = level;
        
        _universe.BroadcastCustomEvent("EventOnRoomLoaded");
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player) {
        if(player.isLocal)
            SetRoomOwner(player);
    }

    public void SetRoomOwner(VRCPlayerApi player) {
        roomOwner = player.displayName;
        Networking.SetOwner(player, gameObject);
        Networking.SetOwner(player, level.gameObject);
        if (player.isLocal && _roomManager.localTracker.requestingRoom) {
            roomOwner = player.displayName;
            JoinRoom();
        }
        RequestSerialization();
    }
    
    public VRCPlayerApi[] GetPlayers() {
        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);
        
        var playersInRoom = new VRCPlayerApi[players.Length];
        var i             = 0;

        foreach (var p in players) {
            var tracker = _roomManager.GetTracker(p);
            if (tracker == null) continue;
            if (tracker.currentRoom != transform.GetSiblingIndex()) continue;
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