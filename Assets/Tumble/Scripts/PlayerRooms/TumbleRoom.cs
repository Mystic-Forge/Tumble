using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


public class TumbleRoom : UdonSharpBehaviour {
    [UdonSynced] public string    roomName;
    public              Transform spawnPoint;
    [UdonSynced] public RoomType  roomType = RoomType.Level;
    [UdonSynced] public string    roomOwner;

    public TumbleLevel level;

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
        _universe.movement._TeleportTo(spawnPoint.position, spawnPoint.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint);
        _universe.spawnPoint.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        LoadRoom();
    }

    public void LeaveRoom() {
        var localTracker = _roomManager.localTracker;
        if (localTracker == null) return;

        localTracker.currentRoom = -1;
        _universe.spawnPoint.SetPositionAndRotation(Vector3.zero, Quaternion.identity); // Tentative until we get a world spawn location
        _universe.movement._TeleportTo(_universe.spawnPoint.position, _universe.spawnPoint.rotation);

        if (roomType == RoomType.Editor) _universe.levelEditor.level = null;
    }

    private void LoadRoom() {
        if (!level.levelLoaded) level.LoadLevelFromRaw();

        if (roomType == RoomType.Editor) _universe.levelEditor.level = level;
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player) {
        if(player.isLocal)
            SetRoomOwner(player);
    }

    public void SetRoomOwner(VRCPlayerApi player) {
        roomOwner = player.displayName;
        Networking.SetOwner(player, gameObject);
        if (player.isLocal) {
            roomOwner = player.displayName;
            JoinRoom();
        }
    }
}

public enum RoomType {
    Vacant,
    Level,
    Editor,
}