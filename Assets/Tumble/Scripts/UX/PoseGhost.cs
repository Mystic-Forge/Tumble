
using System;

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PoseGhost : UdonSharpBehaviour
{
    public string[] poseNames;
    public float[] poseDurations;

    private float        _currentPoseTimer;
    private int          _currentPoseIndex;
    private PoseManager  _manager;
    private GameObject[] _ghostInstances;
    
    private void Start() {
        _manager = GetComponentInParent<Universe>().poseManager;
        _ghostInstances = new GameObject[poseNames.Length];
        foreach (var poseName in poseNames) {
            var pose = _manager.GetPose(poseName);
            if (pose == null) {
                Debug.LogError($"Pose {poseName} not found");
                continue;
            }
            
            var ghost = Instantiate(pose.gameObject);
            ghost.transform.SetParent(transform);
            ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
            ghost.SetActive(false);
            _ghostInstances[_currentPoseIndex] = ghost;            
            _currentPoseIndex++;
        }
        _currentPoseIndex = 0;
    }

    private void FixedUpdate() {
        _currentPoseTimer -= Time.fixedDeltaTime;
        if (_currentPoseTimer <= 0) {
            _currentPoseIndex = (_currentPoseIndex + 1) % poseNames.Length;
            _currentPoseTimer = poseDurations[_currentPoseIndex];
            
            foreach (var ghost in _ghostInstances) ghost.SetActive(false);
            var instance = _ghostInstances[_currentPoseIndex];
            instance.SetActive(true);
            instance.transform.localPosition = Vector3.up * _manager.height;
            instance.transform.localScale = Vector3.one * _manager.HeightRatio;
        }
    }
}
