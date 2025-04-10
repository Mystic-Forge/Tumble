
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class SelectEditModeElementButton : TumbleBehaviour
{
    public int elementId;
    public GameObject selectedIndicator;

    public void SelectElement() {
        Universe.levelEditor.tool.elementId = elementId;
    }
    
    private void FixedUpdate() {
        selectedIndicator.SetActive(Universe.levelEditor.tool.elementId == elementId);
    }
}
