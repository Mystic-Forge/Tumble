﻿using System;

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

    public bool HasOwner => !string.IsNullOrWhiteSpace(playerName);

    public bool IsLocal => playerName == Networking.LocalPlayer.displayName;

    private DataDictionary _localChanges = new DataDictionary();

    private Universe _universe;

    private void Start() {
        _universe = GetComponentInParent<Universe>();

        _localChanges.Add("c", new DataList());
    }

    private void FixedUpdate() {
        if (!Networking.LocalPlayer.isMaster && !HasOwner) playerName = Networking.LocalPlayer.displayName;

        if (IsLocal && Networking.GetOwner(gameObject) != Networking.LocalPlayer) Networking.SetOwner(Networking.LocalPlayer, gameObject);
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

    public override void OnPostSerialization(SerializationResult result) {
        var changes = _localChanges["c"].DataList;
        changes.Clear();
    }

    public override void OnDeserialization(DeserializationResult result) {
        if (string.IsNullOrEmpty(changesData)) return;

        var owner = Networking.GetOwner(gameObject);
        if (_universe.playerRoomManager.GetTracker(owner).currentRoom != _universe.playerRoomManager.localTracker.currentRoom) return; // If we are not in the room, we do not care about the change

        if (VRCJson.TryDeserializeFromJson(changesData, out var deserializedChanges)) {
            var changes = deserializedChanges.DataDictionary;

            foreach (var change in changes["c"].DataList.ToArray()) editor.ReceiveChange(change.DataDictionary);
        }
    }
}

public enum LevelEditorChangeType {
    Add,
    Remove,
}