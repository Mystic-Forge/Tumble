
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class InputFieldInputBlocker : UdonSharpBehaviour
{
    private Universe _universe;
    
    private void Start()
    {
        _universe = GetComponentInParent<Universe>();
    }

    public void OnSelect() {
        _universe.blockingInputs++;
    }
    
    public void OnDeselect() {
        _universe.blockingInputs--;
    }
}
