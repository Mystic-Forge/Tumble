
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class InputFieldPoseBlocker : UdonSharpBehaviour
{
    private Universe _universe;
    
    private void Start()
    {
        _universe = GetComponentInParent<Universe>();
    }

    public void OnSelect() {
        _universe.poseManager.blockPoses = true;
    }
    
    public void OnDeselect() {
        _universe.poseManager.blockPoses = false;
    }
}
