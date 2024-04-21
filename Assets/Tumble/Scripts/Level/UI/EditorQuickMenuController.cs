using System;

using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class EditorQuickMenuController : UdonSharpBehaviour {
    public GameObject editorQuickMenu;
    public GameObject paintSelector;
    public GameObject toolSelector;

    private Universe _universe;

    void Start() { _universe = GetComponentInParent<Universe>(); }

    private void FixedUpdate() { paintSelector.SetActive(_universe.levelEditor.tool.mode == LevelEditorToolMode.Paint || _universe.levelEditor.tool.mode == LevelEditorToolMode.Place); }
}