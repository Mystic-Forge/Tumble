using System;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class LevelEditorSynchronizer : SyncedTumbleBehaviour {
    [UdonSynced] public string changesData;

    public LevelEditor editor;

    public bool HasOwner => ownerId != -1;

    private DataDictionary _localChanges = new DataDictionary();

    private void Start() {
        _localChanges.Add("c", new DataList());
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

    public override void OnDeserialized(DeserializationResult result) {
        if (string.IsNullOrEmpty(changesData)) return;

        // If we are not in the same room, we do not care about the change
        var owner        = GetOwner();
        var ownerTracker = Universe.playerRoomManager.GetTracker(owner);
        var localTracker = Universe.playerRoomManager.localTracker;
        if(ownerTracker == null || localTracker == null) return;
        if (ownerTracker.Room != LocalRoom) return;

        if (VRCJson.TryDeserializeFromJson(changesData, out var deserializedChanges)) {
            var changes = deserializedChanges.DataDictionary;

            foreach (var change in changes["c"].DataList.ToArray()) editor.ReceiveChange(change.DataDictionary);
        }
    }
}

public enum LevelEditorChangeType {
    Add,
    Remove,
    Move,
    SetState,
}