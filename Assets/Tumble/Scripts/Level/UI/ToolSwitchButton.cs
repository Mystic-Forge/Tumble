
using System;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ToolSwitchButton : UdonSharpBehaviour {
    private Universe _universe;

    private void Start() {
        _universe = GetComponentInParent<Universe>();
    }
    

    public void Use() {
        
    }
}
