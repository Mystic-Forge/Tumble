using System;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LevelEditorSynchronizer : UdonSharpBehaviour {
    [UdonSynced] public string playerName;
    [UdonSynced] public string changesData;

    public LevelEditor editor;

    public bool HasOwner => !string.IsNullOrWhiteSpace(playerName); // && Networking.GetOwner(gameObject).displayName == playerName;

    public bool IsLocal => playerName == Networking.LocalPlayer.displayName;

    private DataDictionary _localChanges = new DataDictionary();

    private Universe _universe;

    private void Start() {
        _universe = GetComponentInParent<Universe>();

        _localChanges.Add("c", new DataList());
    }

    private void FixedUpdate() {
        var owner                                                    = Networking.GetOwner(gameObject);
        if (!owner.isMaster && owner.isLocal && !IsLocal) playerName = Networking.LocalPlayer.displayName;
        if (IsLocal && !owner.isLocal) Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }

    public void SubmitChange(DataDictionary change) {
        _localChanges["c"].DataList.Add(change);
        RequestSerialization();
    }

    public override void OnPreSerialization() {
        var result = VRCJson.TrySerializeToJson(_localChanges, JsonExportType.Minify, out var jsonToken);

        if (!result) {
            Debug.Log("[TUMBLE] Failed to serialize level changes");
            _localChanges["c"].DataList.Clear();
            return;
        }

        changesData = jsonToken.String;
    }

    public override void OnPostSerialization(SerializationResult result) => _localChanges["c"].DataList.Clear();

    public override void OnDeserialization(DeserializationResult result) {
        if (string.IsNullOrEmpty(changesData)) return;

        // If we are not in the same room, we do not care about the change
        var owner        = Networking.GetOwner(gameObject);
        var ownerTracker = _universe.playerRoomManager.GetTracker(owner);
        var localTracker = _universe.playerRoomManager.localTracker;
        if(ownerTracker == null || localTracker == null) return;
        if (ownerTracker.currentRoom != localTracker.currentRoom) return;

        if (VRCJson.TryDeserializeFromJson(changesData, out var deserializedChanges)) {
            var changes = deserializedChanges.DataDictionary;

            foreach (var change in changes["c"].DataList.ToArray()) editor.ReceiveChange(change.DataDictionary);
        }
    }

    public override void OnOwnershipTransferred(VRCPlayerApi player) {
        if (player.isLocal && !player.isMaster) {
            playerName = player.displayName;
            RequestSerialization();
        }
    }
}

public enum LevelEditorChangeType {
    Add,
    Remove,
}