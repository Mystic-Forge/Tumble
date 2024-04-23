using System;

using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class EditModeToggle : UdonSharpBehaviour {
    public GameObject playIcon;
    public GameObject editIcon;
    
    public GameObject playModeUI;
    public GameObject editModeUI;

    private Universe _universe;

    private float _contextMenuOpenTime;
    private bool _toggled;

    private void Start() { _universe = GetComponentInParent<Universe>(); }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Tab)) ToggleEditMode();
    }
    
    public void ToggleEditMode() {
        _universe.levelEditor.tool.SetMode(
            (int)(_universe.levelEditor.tool.mode == LevelEditorToolMode.Play ? LevelEditorToolMode.Place : LevelEditorToolMode.Play)
        );
        
        playIcon.SetActive(_universe.levelEditor.tool.mode != LevelEditorToolMode.Play);
        editIcon.SetActive(!playIcon.activeSelf);
        
        playModeUI.SetActive(!playIcon.activeSelf);
        editModeUI.SetActive(!editIcon.activeSelf);
    }
    
    public void EventShowContextMenu() {
        _contextMenuOpenTime = Time.time;
        _toggled             = true;
    }
    
    public override void InputLookVertical(float value, UdonInputEventArgs args) {
        if (!Networking.LocalPlayer.IsUserInVR()) return;
        
        if (_toggled && value < 0.3f) _toggled = false;
        
        if(Time.time - _contextMenuOpenTime < 0.1f) return;

        if (_universe.contextMenu.IsOpen
            && !_toggled
            && value > 0.5f) {
            ToggleEditMode();
            _toggled = true;
        }
    }
}