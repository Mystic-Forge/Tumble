
using System;

using Tumble.Scripts.Level;

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

using VRC.SDKBase;
using VRC.Udon;

public class EditorToolModeSwitchButton : UdonSharpBehaviour
{
    public LevelEditorToolMode toolMode;

    private Universe _universe;
    private Toggle _toggle;

    private void Start() {
        _toggle = GetComponent<Toggle>();
    }

    private void Update() {
        if (_universe == null) _universe = GetComponentInParent<Universe>();
        if(!_toggle.isOn && _universe.levelEditor.tool.mode == toolMode) _toggle.isOn = true;
    }

    public void OnPress() {
        if (_universe == null) _universe = GetComponentInParent<Universe>();
        _universe.levelEditor.tool.SetMode((int)toolMode);    
    }
}
