using System;

using TMPro;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class LevelInfoMenu : TumbleBehaviour {
    public TextMeshProUGUI levelNameText;
    public TextMeshProUGUI levelDescriptionText;
    public TextMeshProUGUI levelTagsText;

    private LevelEditor _editor;
    
    public void EventRoomLoaded() {
        gameObject.SetActive(LocalRoom.roomType == RoomType.Level);
        if (!gameObject.activeSelf) return;
        
        levelNameText.text        = LocalLevel.levelName;
        levelDescriptionText.text = LocalLevel.levelDescription;
        levelTagsText.text        = string.Join(" ", LocalLevel.tags);
    }
    
    public void EventRoomUnloaded() {
        gameObject.SetActive(false);
    }
}