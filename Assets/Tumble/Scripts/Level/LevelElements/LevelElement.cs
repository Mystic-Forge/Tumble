using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public abstract class LevelElement : TumbleBehaviour {
    private uint _state;
    
    public virtual bool PlaceModeIgnoresColliders => false;
    
    /// <summary>
    /// Used for uniquely identifying an element. Typically this will be some form of hash of the element's data.
    /// </summary>
    /// <returns>
    /// A byte representing the hash of the element.
    /// </returns>
    public uint GetState() => _state;
    
    public void SetState(uint state, bool syncChange = true) {
        if(syncChange)
            Universe.levelEditor.SetElementState(gameObject, state);
        _state = state;
        OnSetState();
    }

    protected virtual void OnSetState() {
        transform.localRotation = TumbleLevelLoader64.DecodeRotation((int)_state);
    }
    
    public virtual void UpdateState() {
        SetState(_state);
    }
    
    public virtual bool TryOverlapElement(Ray ray, Vector3 point) => true;

    public virtual void OnElementPlaced(Ray ray, Vector3 point) { }
    
    public virtual void OnPlaceDrag(Ray ray, Vector3 point) { }
}