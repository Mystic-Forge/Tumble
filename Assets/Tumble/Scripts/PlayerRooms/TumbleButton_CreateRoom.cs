
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleButton_CreateRoom : UdonSharpBehaviour
{
    public RoomType roomType;
    
    private Universe _universe;
    
    void Start()
    {
        _universe = GetComponentInParent<Universe>();    
    }

    public void OnClick() {
        _universe.playerRoomManager.RequestRoom(roomType);
    }
}
