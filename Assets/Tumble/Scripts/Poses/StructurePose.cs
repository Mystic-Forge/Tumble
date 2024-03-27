using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


public class StructurePose : Pose {
    public string    structureId = "plank";
    public Transform structureSpawnPoint;

    public AudioSource spawnStructureSound;
    
    private StructureManager _structureManager;
    private Universe         _universe;

    private void Start() {
        _universe         = GetComponentInParent<Universe>();
        _structureManager = _universe.structureManager;
    }

    public override void OnPoseEnter() {
        var playerHead = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        var dir        = structureSpawnPoint.position - playerHead;
        var ray        = new Ray(playerHead, dir.normalized);
        var hits       = Physics.RaycastAll(ray, dir.magnitude);

        var passed = _universe.modifiers.AllGroundIsDirt;

        if (!passed)
            foreach (var h in hits) {
                if (Structure.GetHitGroundType(h) == GroundType.Spawn) {
                    passed = true;
                    break;
                }
            }

        if (!passed) return;

        var structure = _structureManager.SpawnStructure(structureId);
        structure.transform.position = structureSpawnPoint.position;
        structure.transform.rotation = structureSpawnPoint.rotation;
        structure.OnSpawnStructure(ray.origin);

        spawnStructureSound.Play();
    }
}