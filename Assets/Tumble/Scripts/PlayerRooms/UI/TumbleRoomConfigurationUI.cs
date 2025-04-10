using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleRoomConfigurationUI : TumbleBehaviour {
    public InputField roomNameInputField;

    public Transform  usersContainer;
    public GameObject userEntryPrefab;
    
    public void UpdateUserList() {
        for (var i = usersContainer.childCount - 1; i >= 0; i--) DestroyImmediate(usersContainer.GetChild(i).gameObject);

        foreach (var player in LocalRoom.GetPlayers()) {
            var entry = Instantiate(userEntryPrefab, usersContainer);
            entry.GetComponent<UserUIElement>().SetUser(player, LocalRoom, this);
        }
    }

    public void UpdateRoomName() {
        if (LocalRoom == null) return;
        if (!LocalRoom.LocalIsOwner) return;

        LocalRoom.roomName = roomNameInputField.text;
        LocalRoom.RequestSerialization();
    }

    public void EventRoomLoaded() {
        gameObject.SetActive(true);
        roomNameInputField.text = LocalRoom.roomName;
    }
    
    public void EventRoomUnloaded() {
        gameObject.SetActive(false);
    }

    public void EventRoomOwnerChanged() {
        var isOwner = LocalRoom.LocalIsOwner;
        roomNameInputField.interactable = isOwner;
        // roomNameInputField.text         = LocalRoom.roomName;
        UpdateUserList();
    }

    public void EventRoomUsersUpdated() {
        UpdateUserList();
    }
}