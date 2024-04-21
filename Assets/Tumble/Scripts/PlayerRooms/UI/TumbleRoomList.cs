
using System;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class TumbleRoomList : UdonSharpBehaviour {
    public GameObject roomElement;
    
    private Universe _universe;
    private PlayerRoomManager _roomManager;
    
    private float _lastUpdate;
    
    private void Start() {
        _universe    = GetComponentInParent<Universe>();
        _roomManager = _universe.playerRoomManager;
    }

    private void FixedUpdate() {
        if (Time.time - _lastUpdate < 3) return;
        
        _lastUpdate = Time.time;

        for (var i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
        
        foreach (var room in _roomManager.GetOpenRooms()) AddRoom(room);
    }

    public void AddRoom(TumbleRoom room) {
        var element = Instantiate(roomElement, transform);
        element.GetComponent<TumbleRoomUIElement>().UpdateRoomInfo(room);
    }
}
