using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

using Random = UnityEngine.Random;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TumbleLevel : SyncedTumbleBehaviour {
    public              int      version = 0;
    public              int      levelId = -1;
    [UdonSynced] public string   levelName;
    [UdonSynced] public string   levelDescription;
    [UdonSynced] public string[] tags;

    public string LevelKey => $"l{levelId}";

    [UdonSynced] public string         rawLevelData;
    public              DataDictionary levelData;

    private float _lastLevelSyncTime;

    public Transform  levelRoot;
    public GameObject elementHolderPrefab;

    public bool levelLoaded = false;

    public LevelCollectable[] collectables = new LevelCollectable[0];

    private TumbleRoom _room;

    public TumbleRoom Room {
        get {
            if (_room == null) _room = GetComponentInParent<TumbleRoom>();
            return _room;
        }
    }

    private TumbleLevelLoader64 Loader => Universe.levelLoader;

    private bool _initialized;

    private LevelTrigger _currentCheckpoint;
    private bool         _isStarted;

    public bool IsStarted => _isStarted;

    private void FixedUpdate() {
        if (Universe.levelEditor.level != this) return;

        if (!Networking.GetOwner(gameObject).isLocal) return;

        if (Time.time - _lastLevelSyncTime > 60f) {
            RequestSerialization();
            _lastLevelSyncTime = Time.time;
        }
    }

    public void LoadLevelFromRaw() {
        ClearLevel();
        levelLoaded = true;

        if (string.IsNullOrEmpty(rawLevelData)) return;

        levelData = Loader.DeserializeLevelData(rawLevelData);
        LoadLevel();
    }

    public void LoadLevel() { Loader.LoadLevelFromData(this); }

    public void SaveData() { rawLevelData = TumbleLevelLoader64.SerializeLevel(this); }

    public override void OnPreSerialization() {
        var data = TumbleLevelLoader64.SerializeLevel(this);
        if (data == rawLevelData) return;

        rawLevelData = data;
    }

    public override void OnDeserialized(DeserializationResult result) {
        var existingData = TumbleLevelLoader64.SerializeLevel(this);
        if (rawLevelData == existingData) return;

        LoadLevelFromRaw();
    }

    private void ClearLevel() {
        for (var i = levelRoot.childCount - 1; i >= 0; i--) {
            var child = levelRoot.GetChild(i);
            DestroyImmediate(child.gameObject);
        }

        collectables = new LevelCollectable[0];
    }

    public void StartLevel(LevelTrigger startTrigger) {
        collectables = GetComponentsInChildren<LevelCollectable>();
        ResetLevel();
        SetCheckpoint(startTrigger);
        _isStarted = true;
        Universe.BroadcastCustomEvent("EventLevelStarted");
    }

    public void SetCheckpoint(LevelTrigger checkpoint) { _currentCheckpoint = checkpoint; }
    
    public void RespawnAtCheckpoint() {
        if (_currentCheckpoint == null) return;

        Universe.movement._TeleportTo(_currentCheckpoint.transform.position, _currentCheckpoint.transform.rotation, VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint);
        Universe.movement._SetVelocity(Vector3.zero);
    }

    public bool IsLevelCompletable() {
        if (!_isStarted) return false;

        foreach (var c in collectables)
            if (!c.IsCollected)
                return false;

        return true;
    }

    public void EndLevel() {
        Universe.BroadcastCustomEvent("EventLevelEnded");
        ResetLevel();
    }

    public void ResetLevel() {
        _isStarted = false;
        foreach (var c in collectables) c.ResetCollectable();
        Universe.BroadcastCustomEvent("EventLevelReset");
    }

    public void EventLevelEditorToolModeChanged() {
        if (IsStarted) ResetLevel();
    }

    public Transform GetElementHolder(int elementId) {
        var holder = levelRoot.Find(elementId.ToString());

        if (holder == null) {
            holder      = Instantiate(elementHolderPrefab, levelRoot).transform;
            holder.name = elementId.ToString();
        }

        return holder;
    }

    public bool TryGetHitElement(RaycastHit hit, out GameObject element, out int id) {
        element = null;
        id      = -1;
        if (hit.collider == null) return false;

        if (!hit.collider.transform.IsChildOf(levelRoot)) return false;

        element = hit.collider.gameObject;
        while (element.transform.parent.parent != levelRoot) element = element.transform.parent.gameObject;
        id = GetElementId(element);
        return true;
    }

    public GameObject GetElementAt(Vector3Int cell) {
        for (var i = 0; i < levelRoot.childCount; i++) {
            var holder = levelRoot.GetChild(i);

            for (var j = 0; j < holder.childCount; j++) {
                var element = holder.GetChild(j);
                if (GetCell(element.position) == cell) return element.gameObject;
            }
        }

        return null;
    }

    private GameObject FindElement(DataToken element) {
        if (element.TokenType != TokenType.DataList) return null;
        if (element.DataList.Count < 3) return null;

        var dataList   = element.DataList;
        var elementId  = (int)dataList[0].Number;
        var positionId = (uint)dataList[1].Number;
        var cell       = LevelEditor.DecodeCell(positionId);
        var state      = (uint)dataList[2].Number;

        var holder = GetElementHolder(elementId);

        for (var i = 0; i < holder.childCount; i++) {
            var child = holder.GetChild(i);
            if (GetLocalCell(child.localPosition) == cell && GetElementState(child.gameObject) == state) return child.gameObject;
        }

        return null;
    }

    public DataList GetElementsInCell(Vector3Int cell) {
        var list = new DataList();

        for (var i = 0; i < levelRoot.childCount; i++) {
            var holder = levelRoot.GetChild(i);

            for (var j = 0; j < holder.childCount; j++) {
                var element = holder.GetChild(j);

                if (GetLocalCell(element.localPosition) == cell) list.Add(element.gameObject);
            }
        }

        return list;
    }

    public bool RemoveElement(DataToken element) => RemoveElement(FindElement(element));

    public bool RemoveElement(GameObject element) {
        if (element == null) return false;

        DestroyImmediate(element);
        return true;
    }

    public GameObject AddElement(DataToken elementData) {
        if (elementData.TokenType != TokenType.DataList) return null;
        if (elementData.DataList.Count < 2) return null;

        var dataList    = elementData.DataList;
        var elementId   = (int)dataList[0].Number;
        var positionId = (uint)dataList[1].Number;
        var cell        = LevelEditor.DecodeCell(positionId);
        var state       = (uint)dataList[2].Number;

        return AddElement(elementId, cell, state);
    }

    public GameObject AddElement(int elementId, Vector3 localPosition, uint state) {
        var holder   = GetElementHolder(elementId);
        var element  = Loader.levelElements[elementId];
        var instance = Instantiate(element, holder);
        instance.transform.localPosition = GetLocalCell(localPosition);
        SetElementState(instance, state);
        return instance;
    }

    public void MoveElement(DataToken element, Vector3Int cell) => MoveElement(FindElement(element), cell);

    public void MoveElement(GameObject element, Vector3Int cell) {
        if (element == null) return;

        element.transform.localPosition = cell;
    }

    public void SetElementState(DataToken element, uint state) => SetElementState(FindElement(element), state);

    public void SetElementState(GameObject element, uint state) {
        if (element == null) return;

        var levelElement = element.GetComponent<LevelElement>();

        if (levelElement != null)
            levelElement.SetState(state, false);
        else
            element.transform.localRotation = TumbleLevelLoader64.DecodeRotation((int)state);
    }

    public static uint GetElementState(GameObject element) {
        var levelElement = element.GetComponent<LevelElement>();
        return levelElement == null ? TumbleLevelLoader64.EncodeRotation(element.transform.localRotation) : levelElement.GetState();
    }

    public Vector3Int GetCell(Vector3 worldPosition) {
        var localPosition = levelRoot.InverseTransformPoint(worldPosition);
        return GetLocalCell(localPosition);
    }

    public static Vector3Int GetLocalCell(Vector3 localPosition) =>
        new Vector3Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y), Mathf.RoundToInt(localPosition.z));

    public static int GetElementId(GameObject element) {
        if (element == null || element.transform.parent == null) return -1;

        var holder = element.transform.parent;
        return int.TryParse(holder.name, out var id) ? id : -1;
    }

    public Vector3 GetWorldPosition(Vector3Int cell) => levelRoot.TransformPoint(cell);
}