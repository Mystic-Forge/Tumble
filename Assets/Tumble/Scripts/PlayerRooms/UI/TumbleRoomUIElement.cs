
using TMPro;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TumbleRoomUIElement : UdonSharpBehaviour {
    public TextMeshProUGUI roomName;
    public TextMeshProUGUI roomActivity;
    
    private TumbleRoom _room;

    public void UpdateRoomInfo(TumbleRoom room) {
        _room = room;
        roomName.text = room.roomName;
        var activityType = room.roomType == RoomType.Level ? "Playing" : "Editing";
        roomActivity.text = $"{activityType} {room.level.levelName}";
    }

    public void Join() {
        _room.JoinRoom();
    }
}
