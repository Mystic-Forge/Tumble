using System;

using TMPro;

using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


public class LevelEditor : UdonSharpBehaviour {
    public TumbleLevel level;
    public Transform   synchronizerHolder;

    public LevelEditorTool tool;

    private LevelEditorSynchronizer[] _synchronizers;

    public LevelEditorSynchronizer LocalSynchronizer => GetSynchronizer(Networking.LocalPlayer);

    private TumbleLevelLoader64 _loader;

    private LevelEditorSynchronizer GetFirstAvailableSynchronizer() {
        for (var i = 0; i < _synchronizers.Length; i++) {
            var synchronizer = _synchronizers[i];

            if (!synchronizer.HasOwner) return synchronizer;
        }

        return null;
    }

    private LevelEditorSynchronizer GetSynchronizer(VRCPlayerApi player) {
        var name = player.displayName;

        for (var i = 0; i < _synchronizers.Length; i++) {
            var synchronizer = _synchronizers[i];
            if (synchronizer.GetOwner() == player) return synchronizer;
        }

        return null;
    }

    private void Start() {
        _synchronizers = synchronizerHolder.GetComponentsInChildren<LevelEditorSynchronizer>();
        _loader        = GetComponentInParent<Universe>().levelLoader;
    }

    private void FixedUpdate() {
        if (!Networking.LocalPlayer.isMaster) return;

        var players = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(players);

        foreach (var p in players) {
            var synchronizer = GetSynchronizer(p);
            if (synchronizer != null) continue;

            var firstAvailableSynchronizer = GetFirstAvailableSynchronizer();
            if (firstAvailableSynchronizer == null) continue;

            firstAvailableSynchronizer.SetOwner(p);
        }
    }

    public GameObject AddElement(int elementId, Vector3 localPosition, uint state) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return null;

        if (elementId < 0 || elementId >= _loader.levelElements.Length) return null;

        var change = new DataDictionary();

        change["t"] = new DataToken((int)LevelEditorChangeType.Add);
        change["e"] = EncodeElement(elementId, localPosition, state);

        synchronizer.SubmitChange(change);

        return level.AddElement(elementId, localPosition, state);
    }

    public void MoveElement(GameObject element, Vector3 localPosition) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return;

        var cell = TumbleLevel.GetLocalCell(localPosition);

        var change = new DataDictionary();

        var elementData = EncodeElement(element);
        change["t"] = new DataToken((int)LevelEditorChangeType.Move);
        change["e"] = elementData;
        change["p"] = new DataToken(EncodeCell(cell));

        synchronizer.SubmitChange(change);

        level.MoveElement(element, cell);
    }

    public void SetElementState(GameObject element, uint state) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return;

        var change = new DataDictionary();

        change["t"] = new DataToken((int)LevelEditorChangeType.SetState);
        change["e"] = EncodeElement(element);
        change["d"] = new DataToken(state);

        synchronizer.SubmitChange(change);
    }

    private static DataToken EncodeElement(GameObject element) =>
        EncodeElement(TumbleLevel.GetElementId(element), element.transform.localPosition, TumbleLevel.GetElementState(element));

    private static DataToken EncodeElement(int elementId, Vector3 localPosition, uint state) {
        var data = new DataList();
        data.Add(elementId);
        var cell = EncodeCell(TumbleLevel.GetLocalCell(localPosition));
        data.Add(cell); // Still a byte of space left :o
        data.Add(state);
        return data;
    }

    public static uint EncodeCell(Vector3Int cell) => (uint)(cell.x + cell.y * 256 + cell.z * 256 * 256);

    public static Vector3Int DecodeCell(uint cell) {
        var x = (int)cell % 256;
        var y = (int)(cell / 256) % 256;
        var z = cell / (256 * 256);
        return new Vector3Int((int)x, (int)y, (int)z);
    }

    public void RemoveElement(GameObject element) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return;

        var change = new DataDictionary();

        change["t"] = new DataToken((int)LevelEditorChangeType.Remove);
        change["e"] = EncodeElement(element);

        synchronizer.SubmitChange(change);

        level.RemoveElement(element);
    }

    public void ReceiveChange(DataDictionary change) {
        var changeType = (LevelEditorChangeType)(int)change["t"].Number;

        switch (changeType) {
            case LevelEditorChangeType.Add: {
                var elementData = change["e"];
                level.AddElement(elementData);
                break;
            }
            case LevelEditorChangeType.Remove: {
                var removeElement = change["e"];
                level.RemoveElement(removeElement);
                break;
            }
            case LevelEditorChangeType.Move: {
                var moveElement = change["e"];
                var cell        = DecodeCell((uint)change["p"].Number);
                level.MoveElement(moveElement, cell);
                break;
            }
            case LevelEditorChangeType.SetState: {
                var elementData = change["e"];
                level.SetElementState(elementData, (uint)change["d"].Number);
                break;
            }
        }
    }
}