using UdonSharp;

using UnityEngine;


[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class InterpolatedTransform : UdonSharpBehaviour {
    public float updateRate = 0.2f;

    private Vector3 _lastPosition;
    private Vector3 _position;
    private Vector3 _lastParentPos;
    
    private float _lastSyncTime;
    
    private void Start() {
        _lastPosition = transform.position;
        _lastParentPos = transform.parent.position;
    }
    
    public void Teleport() {
        _lastPosition = transform.position;
        _lastParentPos = transform.parent.position;
        _lastSyncTime = Time.time;
    }

    public void LateUpdate() {
        if (_lastParentPos != transform.parent.position) {
            _lastParentPos = transform.parent.position;
            _lastPosition  = _position;
            _lastSyncTime  = Time.time;
        }
        _position = Vector3.Lerp(_lastPosition, _lastParentPos, (Time.time - _lastSyncTime) / updateRate);
        transform.position = _position;
    }
}