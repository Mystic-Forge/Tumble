using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelEditorMenu : TumbleBehaviour {
    public InputField levelNameInputField;
    public InputField levelDescriptionInputField;
    public InputField levelTagsInputField;
    public InputField saveLevelDataInputField;
    public GameObject levelDataPanel;

    public void SetLevelName() {
        LocalLevel.levelName = levelNameInputField.text;
        LocalLevel.RequestSerialization();
    }

    public void SetLevelDescription() {
        LocalLevel.levelDescription = levelDescriptionInputField.text;
        LocalLevel.RequestSerialization();
    }

    public void SetLevelTags() {
        LocalLevel.tags = levelTagsInputField.text.Split(' ');
        LocalLevel.RequestSerialization();
    }

    public void SaveLevelData() {
        LocalLevel.SaveData();
        saveLevelDataInputField.text = LocalLevel.rawLevelData;
    }

    public void LoadLevelData() {
        LocalLevel.rawLevelData = saveLevelDataInputField.text;
        LocalLevel.LoadLevelFromRaw();
        levelNameInputField.text        = LocalLevel.levelName;
        levelDescriptionInputField.text = LocalLevel.levelDescription;
        levelTagsInputField.text        = string.Join(" ", LocalLevel.tags);
        LocalLevel.RequestSerialization();
    }

    public void EventRoomLoaded() {
        gameObject.SetActive(LocalRoom.roomType == RoomType.Editor);
        if (!gameObject.activeSelf) return;

        UpdateFields();
    }
    
    public void EventRoomUnloaded() {
        gameObject.SetActive(false);
    }
    
    public void EventRoomOwnerChanged() {
        UpdateFields();
    }

    public void UpdateFields() {
        var isOwner = LocalRoom.LocalIsOwner;
        levelDataPanel.SetActive(isOwner);
        levelNameInputField.interactable        = isOwner;
        levelDescriptionInputField.interactable = isOwner;
        levelTagsInputField.interactable        = isOwner;
        levelNameInputField.text                = LocalLevel.levelName;
        levelDescriptionInputField.text         = LocalLevel.levelDescription;
        levelTagsInputField.text                = string.Join(" ", LocalLevel.tags);
    }
}