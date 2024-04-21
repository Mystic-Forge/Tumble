
using Tumble.Scripts.Level;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EditorToolModeSwitchButton : UdonSharpBehaviour
{
    public LevelEditorToolMode toolMode;

    private Universe _universe;
    
    private void Start() {
        _universe = GetComponentInParent<Universe>();
    }
    
    public void OnPress() {
        _universe.levelEditor.tool.SetMode((int)toolMode);    
    }
}
