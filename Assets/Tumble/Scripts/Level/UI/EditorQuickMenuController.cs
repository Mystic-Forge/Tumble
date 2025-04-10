using System;

using Tumble.Scripts.Level;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class EditorQuickMenuController : TumbleBehaviour {
    public GameObject editorQuickMenu;
    public GameObject paintSelector;
    public GameObject toolSelector;
    public Transform  elementHolder;

    private void FixedUpdate() {
        var mode = Universe.levelEditor.tool.mode;
        elementHolder.gameObject.SetActive(mode == LevelEditorToolMode.Place);
        var currentElement = Universe.levelEditor.tool.elementId;
        paintSelector.SetActive(mode == LevelEditorToolMode.Paint || (mode == LevelEditorToolMode.Place && currentElement == 0));
    }
}