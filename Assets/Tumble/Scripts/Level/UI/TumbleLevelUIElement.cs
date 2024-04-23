
using TMPro;

using UdonSharp;
using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

public class TumbleLevelUIElement : UdonSharpBehaviour {
    public TextMeshProUGUI levelName;

    private string          _levelId;
    private DataDictionary  _levelData;
    private TumbleLevelList _list;

    public void SetLevelData(TumbleLevelList list, string levelId, DataDictionary level) {
        _levelId = levelId;
        _levelData = level;
        _list = list;
        if(level.TryGetValue(new DataToken((int)LevelDataFormatKey.LevelName), out var levelNameData)) levelName.text = levelNameData.String;
    }

    public void LoadLevel() {
        _list.LoadLevel(_levelId);
    }
}
