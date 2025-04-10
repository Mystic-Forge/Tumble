using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;


public class RailingGizmo : LevelEditorGizmo {
    public Color          baseColor;
    public Color          highlightColor;
    public MeshRenderer[] railings;
    public MeshRenderer[] pillars;

    private MaterialPropertyBlock _baseBlock;
    private MaterialPropertyBlock _highlightBlock;

    private void Start() {
        _baseBlock      = new MaterialPropertyBlock();
        _highlightBlock = new MaterialPropertyBlock();

        _baseBlock.SetColor("_Color", baseColor);
        _highlightBlock.SetColor("_Color", highlightColor);
    }

    public override void UpdateGizmo(Ray ray, Vector3 point, Vector3Int cell, GameObject element) {
        var cellWorldPos = Universe.levelEditor.level.GetWorldPosition(cell);
        transform.position = cellWorldPos;
        var highlightState = LevelElementRailing.FindState(point - cellWorldPos);
        railings[0].SetPropertyBlock(highlightState == 1 ? _highlightBlock : _baseBlock);
        railings[1].SetPropertyBlock(highlightState == 2 ? _highlightBlock : _baseBlock);
        railings[2].SetPropertyBlock(highlightState == 4 ? _highlightBlock : _baseBlock);
        railings[3].SetPropertyBlock(highlightState == 8 ? _highlightBlock : _baseBlock);
        pillars[0].SetPropertyBlock(highlightState == 16 ? _highlightBlock : _baseBlock);
        pillars[1].SetPropertyBlock(highlightState == 32 ? _highlightBlock : _baseBlock);
        pillars[2].SetPropertyBlock(highlightState == 64 ? _highlightBlock : _baseBlock);
        pillars[3].SetPropertyBlock(highlightState == 128 ? _highlightBlock : _baseBlock);
    }
}