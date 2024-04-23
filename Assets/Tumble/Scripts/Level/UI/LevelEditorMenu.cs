
using System;

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelEditorMenu : UdonSharpBehaviour
{
    public InputField levelNameInputField;
    public InputField levelDescriptionInputField;
    public InputField levelTagsInputField;
    public InputField saveLevelDataInputField;
    public GameObject levelDataPanel;

    private Universe    _universe;
    private LevelEditor _editor;
    private TumbleLevel Level => _editor == null ? null : _editor.level;

    private void Start() {
        _universe = GetComponentInParent<Universe>();
        _editor   = _universe.levelEditor;
    }

    private void FixedUpdate() {
        var localRoom = _universe.playerRoomManager.LocalRoom;
        var isOwner   = localRoom == null ? false : localRoom.LocalIsRoomOwner;
        levelDataPanel.SetActive(isOwner);
        levelNameInputField.interactable        = isOwner;
        levelDescriptionInputField.interactable = isOwner;
        levelTagsInputField.interactable        = isOwner;
    }

    public void SetLevelName() {
        Level.levelName = levelNameInputField.text;
    }
    
    public void SetLevelDescription() {
        Level.levelDescription = levelDescriptionInputField.text;
    }
    
    public void SetLevelTags() {
        Level.tags = levelTagsInputField.text.Split(' ');
    }
    
    public void SaveLevelData() {
        Level.SaveData();
        saveLevelDataInputField.text = Level.rawLevelData;
    }
    
    public void LoadLevelData() {
        Level.rawLevelData = saveLevelDataInputField.text;
        Level.LoadLevelFromRaw();
        levelNameInputField.text        = Level.levelName;
        levelDescriptionInputField.text = Level.levelDescription;
        levelTagsInputField.text        = string.Join(" ", Level.tags);
    }

    public void EventOnRoomLoaded() {
        if (Level == null) return;
        
        levelNameInputField.text        = Level.levelName;
        levelDescriptionInputField.text = Level.levelDescription;
        levelTagsInputField.text        = string.Join(" ", Level.tags);
    }
}
