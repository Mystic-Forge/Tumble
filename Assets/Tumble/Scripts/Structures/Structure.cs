using System;

using Nessie.Udon.Movement;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;


public class Structure : UdonSharpBehaviour {
    public int         tier;
    public float       mass     = 1;
    public float       drag     = 1;
    public float       friction = 5;
    public bool        spawnGrounded;
    public float       timer            = 10;
    public Vector3     spawnLaunchForce = new Vector3(0f, 2f, 0f);
    public BoxCollider collider;
    public Vector3     velocity;
    public bool        useGravity = true;
    public bool        physicsActive;
    public bool        grounded;

    public  bool    spawning;
    private Vector3 _spawnHeadPos;

    private float _timeLeft;

    public bool  radialExplodeForce;
    public float explosionForce = 8;

    private Universe _universe;
    private bool     _foundGround;

    public void Initialize() { _universe = GetComponentInParent<Universe>(); }


    public void OnSpawnStructure(Vector3 spawnHeadPos) {
        _spawnHeadPos = spawnHeadPos;

        _timeLeft     = timer;
        useGravity    = true;
        physicsActive = true;
        grounded      = false;
        velocity      = transform.TransformVector(spawnLaunchForce);
        spawning      = true;
    }

    private void FixedUpdate() {
        _timeLeft -= Time.fixedDeltaTime;

        if (_timeLeft <= 0) gameObject.SetActive(false);

        if (physicsActive) {
            if (useGravity && !spawning && !grounded) velocity += Physics.gravity * Time.fixedDeltaTime;
            var dragToUse                                      = grounded ? friction : drag;
            velocity -= velocity * dragToUse * Time.fixedDeltaTime;
        }

        _foundGround = false;

        if (spawning) {
            var dir  = transform.position - _spawnHeadPos;
            var hits = Physics.RaycastAll(_spawnHeadPos, dir.normalized, dir.magnitude);

            foreach (var hit in hits) {
                if (hit.collider == null) continue;
                if (hit.collider.GetComponentInParent<NUMovement>() != null) continue;
                if (hit.collider.GetComponentInParent<Structure>() != null) continue;

                if (_universe.cheats.AllGroundIsDirt || GetHitGroundType(hit) != GroundType.Break) {
                    _foundGround = true;
                    break;
                }
            }
        }

        // Collision Detection / Response
        var boxHits = Physics.BoxCastAll(collider.transform.position,
            collider.size / 2,
            velocity.normalized,
            collider.transform.rotation,
            velocity.magnitude * Time.fixedDeltaTime);

        foreach (var hit in boxHits) {
            if (ProcessHit(hit)) continue;
            if (ProcessOverlap(hit.collider)) continue;
        }

        transform.position += velocity * Time.fixedDeltaTime;

        var overlap = Physics.OverlapBox(collider.transform.position, collider.size / 2, collider.transform.rotation);

        foreach (var collider in overlap)
            if (ProcessOverlap(collider))
                continue;

        if (_foundGround && !spawning && !grounded)
            Ground();
        else if (!_foundGround && grounded) UnGround();

        if (!_foundGround && spawning) {
            if (spawnGrounded) {
                transform.position -= velocity * Time.fixedDeltaTime;
                Ground();
            }

            spawning = false;
        }
    }

    private bool ProcessHit(RaycastHit hit) {
        if (hit.collider == null) return true;

        var movement = hit.collider.GetComponentInParent<NUMovement>();

        if (movement != null) {
            if (tier == 0) {
                DestroyStructure(true);
                return true;
            }

            var targetForce = Vector3.Reflect(velocity, hit.normal) * Vector3.Dot(velocity.normalized, hit.normal);

            var velocityDifference = Vector3.Dot(movement._GetVelocity(), targetForce.normalized);
            var impulseAmount      = targetForce.magnitude - velocityDifference;
            impulseAmount = Mathf.Max(0, impulseAmount);
            movement._SetVelocity(movement._GetVelocity() + targetForce.normalized * impulseAmount);

            movement._TeleportTo(
                Networking.LocalPlayer.GetPosition() + velocity * Time.fixedDeltaTime,
                Networking.LocalPlayer.GetRotation(),
                VRC_SceneDescriptor.SpawnOrientation.AlignPlayerWithSpawnPoint,
                true);

            return true;
        }

        return false;
    }

    private bool ProcessOverlap(Collider collider) {
        if (collider == null) return true;
        if (collider.isTrigger) return true;
        if(collider == _universe.movement.groundedCollider) return true;
        
        var otherStructure = collider.GetComponentInParent<Structure>();

        if (otherStructure == this) return true;

        var movement                  = collider.GetComponentInParent<NUMovement>();

        if (movement != null) {
            if (tier == 0) DestroyStructure(true);
            return true;
        }

        if (!_universe.cheats.AllGroundIsDirt && GetColliderGroundType(collider) == GroundType.Break) {
            Debug.LogError("Hit Ground: " + collider.name);
            DestroyStructure();
            return true;
        }

        if (otherStructure != null)
            DoCollision(otherStructure);
        else
            _foundGround = true;

        return false;
    }

    public void DestroyStructure(bool explode = false) {
        gameObject.SetActive(false);

        if (explode) {
            var force                                 = radialExplodeForce ? (_universe.movement._GetPosition() - transform.position).normalized * explosionForce : Vector3.up * explosionForce;
            var playerVelocity                        = _universe.movement._GetVelocity();
            if (!radialExplodeForce) playerVelocity.y = 0;
            _universe.movement._SetVelocity(playerVelocity + force);
        }
    }

    public void Ground() {
        grounded = true;
        velocity = Vector3.zero;
    }

    public void UnGround() { grounded = false; }

    public void DoCollision(Structure other) {
        if (other.tier <= tier) other.DestroyStructure(true);
        if (other.tier >= tier) DestroyStructure(true);
    }
    
    public static GroundType GetHitGroundType(RaycastHit hit) {
        if (hit.collider == null) return GroundType.Break;
        return GetColliderGroundType(hit.collider);
    }
    
    public static GroundType GetColliderGroundType(Collider collider) {
        var renderer = collider.GetComponentInParent<Renderer>();
        if (renderer == null) return GroundType.Break;

        var material = renderer.sharedMaterial;
        if (material.name.Contains("[t1]")) return GroundType.Spawn;
        if (material.name.Contains("[t2]")) return GroundType.Support;
        
        return GroundType.Break;
    }
}

public enum GroundType {
    Break,
    Spawn,
    Support,
}