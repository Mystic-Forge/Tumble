using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDKBase;
using VRC.Udon;


public class EditorToolPaintSwitchButton : UdonSharpBehaviour {
    public int paintColor;

    private Universe _universe;

    public void OnPress() {
        if (_universe == null) _universe = GetComponentInParent<Universe>();
        _universe.levelEditor.tool.paintColor = paintColor;
    }
}