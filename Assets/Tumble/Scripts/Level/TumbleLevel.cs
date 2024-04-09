using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

using Random = UnityEngine.Random;


[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class TumbleLevel : UdonSharpBehaviour {
    public int    version = 0;
    public int    levelIndex;
    public int    levelId;
    public string levelName;

    public string LevelKey => $"l{levelIndex}";

    [UdonSynced] public string levelData;
    private             float  _lastLevelSyncTime;

    public Transform  levelRoot;
    public GameObject elementHolderPrefab;

    public bool levelLoaded = false;

    private TumbleLevelLoader64 _loader;
    private Universe            _universe;

    private bool _initialized;

    private void Start() {
        _universe = GetComponentInParent<Universe>();
        _loader = _universe.levelLoader;
    }

    private void FixedUpdate() {
        if(_universe.levelEditor.level != this) return;
        
        if (!Networking.GetOwner(gameObject).isLocal) return;

        if (Time.time - _lastLevelSyncTime > 5f) {
            RequestSerialization();
            _lastLevelSyncTime = Time.time;
        }
    }

    public void SaveData() {
        levelData = TumbleLevelLoader64.SerializeLevel(this);
    }
    
    public override void OnPreSerialization() {
        var data = TumbleLevelLoader64.SerializeLevel(this);
        if (data == levelData) return;

        levelData = data;
    }

    public override void OnDeserialization(DeserializationResult result) {
        var existingData = TumbleLevelLoader64.SerializeLevel(this);
        if (levelData == existingData) return;

        ClearLevel();
        _loader.DeserializeLevel(levelData, this);
    }

    private void ClearLevel() {
        for (var i = levelRoot.childCount - 1; i >= 0; i--) {
            var child = levelRoot.GetChild(i);
            DestroyImmediate(child.gameObject);
        }
    }

    public Transform GetElementHolder(int elementId) {
        var holder = levelRoot.Find(elementId.ToString());

        if (holder == null) {
            holder      = Instantiate(elementHolderPrefab, levelRoot).transform;
            holder.name = elementId.ToString();
        }

        return holder;
    }

    public bool TryGetHitElement(RaycastHit hit, out GameObject element) {
        element = null;
        if (hit.collider == null) return false;

        if (!hit.collider.transform.IsChildOf(levelRoot)) return false;

        element = hit.collider.gameObject;
        while (element.transform.parent.parent != levelRoot) element = element.transform.parent.gameObject;
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

    public Vector3Int GetCell(Vector3 worldPosition) {
        var localPosition = levelRoot.InverseTransformPoint(worldPosition);
        return new Vector3Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y), Mathf.RoundToInt(localPosition.z));
    }

    public Vector3 GetWorldPosition(Vector3Int cell) => levelRoot.TransformPoint(cell);
}