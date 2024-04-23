using System.Linq;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleLevelList : UdonSharpBehaviour {
    public GameObject levelUIElementPrefab;
    public Transform  list;

    private Universe _universe;

    private void Start() { _universe = GetComponentInParent<Universe>(); }

    public void EventLevelDatabaseLoaded() {
        for (var i = list.childCount - 1; i >= 0; i--) DestroyImmediate(list.GetChild(i).gameObject);

        foreach (var levelId in _universe.levelDatabase.levels.GetKeys().ToArray()) AddLevel(levelId.String, _universe.levelDatabase.levels[levelId].DataDictionary);
    }

    public void AddLevel(string levelId, DataDictionary level) {
        Debug.Log($"[TUMBLE] Adding level to list: {level}");
        var element = Instantiate(levelUIElementPrefab);
        element.transform.SetParent(list, false);
        element.GetComponent<TumbleLevelUIElement>().SetLevelData(this, levelId, level);
    }

    public void LoadLevel(string levelId) {
        var room = _universe.playerRoomManager.LocalRoom;

        if (room == null) { _universe.playerRoomManager.RequestRoom(RoomType.Level, levelId); }
    }
}