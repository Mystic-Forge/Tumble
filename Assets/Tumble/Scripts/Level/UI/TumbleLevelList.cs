using System.Linq;

using TMPro;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleLevelList : UdonSharpBehaviour {
    public GameObject levelUIElementPrefab;
    public Transform  list;
    public TextMeshProUGUI pageText;

    private Universe _universe;

    public int itemsPerPage;
    private int _page;
    public int Page {
        get => _page;
        set {
            _page = value;
            pageText.text = $"{_page + 1}/{MaxPages}";
        }
    }

    private int MaxPages => Mathf.CeilToInt(_universe.levelDatabase.levels.Count / (float)itemsPerPage);

    private void Start() { _universe = GetComponentInParent<Universe>(); }

    public void EventLevelDatabaseLoaded() {
        if(_universe == null) _universe = GetComponentInParent<Universe>();
        Refresh();
    }

    public void ClearList() {
        for (var i = list.childCount - 1; i >= 0; i--) DestroyImmediate(list.GetChild(i).gameObject);
    }

    public void Refresh() {
        Page = Mathf.Clamp(Page, 0, MaxPages - 1); 
        ClearList();
        
        var levels = _universe.levelDatabase.levels.GetKeys().ToArray();
        for (var i = Page * itemsPerPage; i < Mathf.Min(levels.Length, (Page + 1) * itemsPerPage); i++) {
            var levelId = levels[i].String;
            AddLevel(levelId, _universe.levelDatabase.levels[levels[i]].DataDictionary);
        }
    }

    public void NextPage() {
        Page++;
        Refresh();
    }

    public void PreviousPage() {
        Page--;
        Refresh();
    }

    public void AddLevel(string levelId, DataDictionary level) {
        var element = Instantiate(levelUIElementPrefab);
        element.transform.SetParent(list, false);
        element.GetComponent<TumbleLevelUIElement>().SetLevelData(this, levelId, level);
    }

    public void LoadLevel(string levelId) {
        var room = _universe.playerRoomManager.LocalTrackerRoom;

        if (room == null) { _universe.playerRoomManager.RequestRoom(RoomType.Level, levelId); }
    }
}