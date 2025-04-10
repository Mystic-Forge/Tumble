using System;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

using Random = UnityEngine.Random;


public class AreaElement : LevelElement {
    public Vector3Int endOffset;

    public GameObject area;
    
    protected override void OnSetState() {
        var state = GetState();
        var x     = (int)(state & 0xff);
        var y     = (int)((state >> 8) & 0xff);
        var z     = (int)((state >> 16) & 0xff);
        endOffset = new Vector3Int(x, y, z);
        area.transform.localScale = new Vector3(endOffset.x + 1, endOffset.y + 1, endOffset.z + 1);
        area.transform.localPosition = new Vector3(endOffset.x / 2f, endOffset.y / 2f, endOffset.z / 2f);
    }

    public override void UpdateState() {
        SetState((uint)endOffset.x | ((uint)endOffset.y << 8) | ((uint)endOffset.z << 16));
    }
}