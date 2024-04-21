using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class EditModeToggle : UdonSharpBehaviour {
    public GameObject playIcon;
    public GameObject editIcon;
    
    public GameObject playModeUI;
    public GameObject editModeUI;

    private Universe _universe;

    private void Start() { _universe = GetComponentInParent<Universe>(); }

    public void ToggleEditMode() {
        _universe.levelEditor.tool.SetMode(
            (int)(_universe.levelEditor.tool.mode == LevelEditorToolMode.Play ? LevelEditorToolMode.Place : LevelEditorToolMode.Play)
        );
        
        playIcon.SetActive(_universe.levelEditor.tool.mode != LevelEditorToolMode.Play);
        editIcon.SetActive(!playIcon.activeSelf);
        
        playModeUI.SetActive(!playIcon.activeSelf);
        editModeUI.SetActive(!editIcon.activeSelf);
    }
}