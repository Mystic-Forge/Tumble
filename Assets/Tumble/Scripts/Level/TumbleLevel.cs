using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;

using Random = UnityEngine.Random;


public class TumbleLevel : UdonSharpBehaviour {
    public int    version = 0;
    public int    levelIndex;
    public string levelCode; // YYDDDRRRR

    public bool test;

    public string LevelKey => $"l{levelIndex}";

    public Transform  levelRoot;
    public GameObject elementHolderPrefab;

    private void Start() {
        if (!test) return;

        levelCode = GenerateLevelCode();

        var loader = GetComponentInParent<Universe>().levelLoader;

        var data        = TumbleLevelLoader64.SerializeLevel(this);
        Debug.Log(data);
        ClearLevel();
        loader.DeserializeLevel(data, this);
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
            holder = Instantiate(elementHolderPrefab, levelRoot).transform;
            holder.name          = elementId.ToString();
        }

        return holder;
    }
    
    public bool TryGetHitElement(RaycastHit hit, out GameObject element) {
        element = null;
        if (hit.collider == null) return false;

        if (!hit.collider.transform.IsChildOf(levelRoot)) return false;
    
        element = hit.collider.gameObject;
        while(element.transform.parent.parent != levelRoot) element = element.transform.parent.gameObject;
        return true;
    }
    
    public Vector3Int GetCell(Vector3 position) {
        var localPosition = levelRoot.InverseTransformPoint(position);
        return new Vector3Int(Mathf.RoundToInt(localPosition.x), Mathf.RoundToInt(localPosition.y), Mathf.RoundToInt(localPosition.z));
    }

    public Vector3 GetWorldPosition(Vector3Int cell) => levelRoot.TransformPoint(cell);

    public string GenerateLevelCode() {
        var now  = DateTime.UtcNow;
        var day  = now.DayOfYear;
        var year = now.Year - 2000;

        return $"{year:D2}{day:D3}{TumbleLevelLoader.GetRandomCharacter()}{TumbleLevelLoader.GetRandomCharacter()}{TumbleLevelLoader.GetRandomCharacter()}{TumbleLevelLoader.GetRandomCharacter()}";
    }
}