using System;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;


public class TumbleRoomConfigurationUI : UdonSharpBehaviour {
    public GameObject hideMenu;
    public GameObject container;
    public InputField roomNameInputField;

    public Transform  usersContainer;
    public GameObject userEntryPrefab;

    private Universe          _universe;
    private PlayerRoomManager _roomManager;

    private TumbleRoom CurrentRoom => _roomManager.LocalRoom;

    private float _lastUserListUpdate;

    private void Start() {
        _universe    = GetComponentInParent<Universe>();
        _roomManager = _universe.playerRoomManager;
    }

    private void Update() {
        var room = CurrentRoom;
        container.SetActive(room != null);
        hideMenu.SetActive(room == null);
        if (room == null) return;

        var isOwner = room.LocalIsRoomOwner;
        roomNameInputField.interactable = isOwner;
        if (!isOwner) roomNameInputField.text = room.roomName;

        // We should only update this if the user list changed, but this is fine for now
        if (Time.time - _lastUserListUpdate > 5) UpdateUserList();
    }

    public void UpdateUserList() {
        var room = CurrentRoom;
        _lastUserListUpdate = Time.time;

        for (var i = usersContainer.childCount - 1; i >= 0; i--) DestroyImmediate(usersContainer.GetChild(i).gameObject);

        foreach (var player in room.GetPlayers()) {
            var entry = Instantiate(userEntryPrefab, usersContainer);
            entry.GetComponent<UserUIElement>().SetUser(player, room, this);
        }
    }

    public void UpdateRoomName() {
        var room = CurrentRoom;
        if (room == null) return;
        if (!room.LocalIsRoomOwner) return;

        room.roomName = roomNameInputField.text;
        room.RequestSerialization();
    }

    public void EventOnRoomLoaded() {
        roomNameInputField.text = CurrentRoom.roomName;
    }
}