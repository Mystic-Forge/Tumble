
using TMPro;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TumbleRoomUIElement : UdonSharpBehaviour {
    public TextMeshProUGUI roomName;
    public TextMeshProUGUI roomActivity;

    public void UpdateRoomInfo(TumbleRoom room) {
        roomName.text = room.roomName;
        var activityType = room.roomType == RoomType.Level ? "Playing" : "Editing";
        roomActivity.text = $"{activityType} {room.level.levelName}";
    }
}
