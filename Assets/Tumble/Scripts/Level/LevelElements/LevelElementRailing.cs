using System;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


[ExecuteInEditMode]
public class LevelElementRailing : LevelElement {
    public Transform  partsHolder;
    public GameObject cornerRailingPrefab;
    public GameObject straightRailingPrefab;
    public GameObject pillarPrefab;

    public override bool PlaceModeIgnoresColliders => true;

    protected override void OnSetState() { UpdateElement(); }

    public override bool TryOverlapElement(Ray ray, Vector3 point) {
        var localPoint = transform.InverseTransformPoint(point);

        var state = GetState() ^ FindState(localPoint);
        SetState(state);
        return false;
    }

    public static uint FindState(Vector3 point) {
        var ax = Mathf.Abs(point.x);
        var az = Mathf.Abs(point.z);

        // Pillar
        if (ax > 0.25 && az > 0.25) {
            if (point.x < 0 && point.z > 0) return 16u;
            if (point.x > 0 && point.z > 0) return 32u;
            if (point.x > 0) return 64u;
            else return 128u;
        }
        
        // Railing
        if (az > ax) return point.z > 0 ? 1u : 4u;

        return point.x > 0 ? 2u : 8u;
    }

    public override void OnElementPlaced(Ray ray, Vector3 point) {
        TryOverlapElement(ray, point);
        UpdateElement();
    }

    public void UpdateElement() {
        var state = GetState();

        for (var i = partsHolder.childCount - 1; i >= 0; i--) DestroyImmediate(partsHolder.GetChild(i).gameObject);
        var side1 = (state & 1) != 0;
        var side2 = (state & 2) != 0;
        var side3 = (state & 4) != 0;
        var side4 = (state & 8) != 0;

        BuildRailingCorner(side1, side2, Quaternion.Euler(0, 0,   0));
        BuildRailingCorner(side2, side3, Quaternion.Euler(0, 90,  0));
        BuildRailingCorner(side3, side4, Quaternion.Euler(0, 180, 0));
        BuildRailingCorner(side4, side1, Quaternion.Euler(0, 270, 0));

        var pillar1 = (state & 16) != 0;
        var pillar2 = (state & 32) != 0;
        var pillar3 = (state & 64) != 0;
        var pillar4 = (state & 128) != 0;

        if (pillar1) Instantiate(pillarPrefab, partsHolder);

        if (pillar2) {
            var pillar = Instantiate(pillarPrefab, partsHolder);
            pillar.transform.rotation = Quaternion.Euler(0, 90, 0);
        }

        if (pillar3) {
            var pillar = Instantiate(pillarPrefab, partsHolder);
            pillar.transform.rotation = Quaternion.Euler(0, 180, 0);
        }

        if (pillar4) {
            var pillar = Instantiate(pillarPrefab, partsHolder);
            pillar.transform.rotation = Quaternion.Euler(0, 270, 0);
        }
    }

    private void BuildRailingCorner(bool a, bool b, Quaternion rotation) {
        if (!a && !b) return;

        GameObject instance = null;

        if (a && !b) {
            instance                         = Instantiate(straightRailingPrefab, partsHolder);
            instance.transform.localScale    = new Vector3(-1, 1, 1);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = rotation;
        }
        else
            if (b && !a) {
                instance                         = Instantiate(straightRailingPrefab, partsHolder);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = rotation * Quaternion.Euler(0, 90, 0);
            }
            else
                if (a && b) {
                    instance                         = Instantiate(cornerRailingPrefab, partsHolder);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = rotation;
                }
    }
}