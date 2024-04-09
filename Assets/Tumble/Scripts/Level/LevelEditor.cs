using System;

using TMPro;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


public class LevelEditor : UdonSharpBehaviour {
    public TumbleLevel level;
    public Transform   synchronizerHolder;

    public InputField saveLevelDataInputField;
    
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
        for (var i = 0; i < _synchronizers.Length; i++) {
            var synchronizer = _synchronizers[i];
            if (synchronizer.IsLocal) return synchronizer;
        }

        return null;
    }

    private void Start() {
        _synchronizers = synchronizerHolder.GetComponentsInChildren<LevelEditorSynchronizer>();
        _loader        = GetComponentInParent<Universe>().levelLoader;
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.J)) JoinEditors();
        
        if(level == null) return;
        saveLevelDataInputField.text = level.levelData;
    }
    
    public void SaveLevelData() {
        level.SaveData();
    }

    public void JoinEditors() {
        var synchronizer = LocalSynchronizer;

        if (synchronizer != null) {
            if (Networking.GetOwner(synchronizer.gameObject) != Networking.LocalPlayer) Networking.SetOwner(Networking.LocalPlayer, synchronizer.gameObject); // Rejoin failsafe
            return;
        }

        synchronizer = GetFirstAvailableSynchronizer();
        if (synchronizer == null) return;

        synchronizer.playerName = Networking.LocalPlayer.displayName;
        Networking.SetOwner(Networking.LocalPlayer, synchronizer.gameObject);
    }

    public void AddElement(int elementId, Vector3 position, Quaternion rotation) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return;

        if (_loader.levelElements.Length <= elementId || elementId < 0) return;

        var positionArray = new DataList();
        positionArray.Add(Mathf.RoundToInt(position.x));
        positionArray.Add(Mathf.RoundToInt(position.y));
        positionArray.Add(Mathf.RoundToInt(position.z));

        var change = new DataDictionary();

        change["t"] = new DataToken((int)LevelEditorChangeType.Add);
        change["e"] = new DataToken(elementId);
        change["p"] = new DataToken(positionArray);
        change["r"] = new DataToken(TumbleLevelLoader64.EncodeRotation(rotation));

        synchronizer.SubmitChange(change);

        InstantiateElement(elementId, position, rotation);
    }
    
    public void RemoveElement(Vector3 position) {
        var synchronizer = LocalSynchronizer;
        if (synchronizer == null) return;

        var positionArray = new DataList();
        positionArray.Add(Mathf.RoundToInt(position.x));
        positionArray.Add(Mathf.RoundToInt(position.y));
        positionArray.Add(Mathf.RoundToInt(position.z));

        var change = new DataDictionary();

        change["t"] = new DataToken((int)LevelEditorChangeType.Remove);
        change["p"] = new DataToken(positionArray);

        synchronizer.SubmitChange(change);
    }

    public void ReceiveChange(DataDictionary change) {
        var changeType = (LevelEditorChangeType)(int)change["t"].Number;

        switch (changeType) {
            case LevelEditorChangeType.Add:
                var elementId     = (int)change["e"].Number;
                var positionArray = change["p"].DataList;
                var position      = new Vector3Int((int)positionArray[0].Number, (int)positionArray[1].Number, (int)positionArray[2].Number);

                if (level.GetElementAt(position) != null) return;

                var rotation = TumbleLevelLoader64.DecodeRotation((int)change["r"].Number);

                InstantiateElement(elementId, position, rotation);
                break;
            case LevelEditorChangeType.Remove:
                var removePositionArray = change["p"].DataList;
                var removePosition      = new Vector3Int((int)removePositionArray[0].Number, (int)removePositionArray[1].Number, (int)removePositionArray[2].Number);
                var element            = level.GetElementAt(removePosition);
                if (element != null) Destroy(element.gameObject);
                break;
        }
    }

    private void InstantiateElement(int elementId, Vector3 position, Quaternion rotation) {
        var holder   = level.GetElementHolder(elementId);
        var element  = _loader.levelElements[elementId];
        var instance = Instantiate(element, holder);
        instance.transform.localPosition = position;
        instance.transform.localRotation = rotation;
    }
}