using System;

using UdonSharp;

using UnityEngine;

using VRC.SDKBase;
using VRC.Udon;

using Random = UnityEngine.Random;


[RequireComponent(typeof(MeshFilter)), ExecuteInEditMode, UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StarMesh : UdonSharpBehaviour {
    private MeshFilter Filter => GetComponent<MeshFilter>();

    public int     count                    = 5000;
    public float   distance                 = 1000;
    public Vector2 size                     = new Vector2(100, 100);
    public float   stellarDistanceVariation = 1;
    public float   starColorInfluence       = 0.5f;
    public int     seed;
    public Vector3 galaxyBandNormal = Vector3.up;
    public float   bandPower        = 1;

    void Start() { Filter.sharedMesh = Generate(); }

    private Mesh Generate() {
        var verts  = new Vector3[count * 4];
        var colors = new Color[count * 4];
        var tris   = new int[count * 6];
        var uvs    = new Vector2[count * 4];

        Random.InitState(seed);

        for (int i = 0; i < count; i++) {
            // Prepare properties
            var dir = Random.onUnitSphere;
            var nearestBandPoint = Vector3.ProjectOnPlane(dir, galaxyBandNormal).normalized;
            dir = Vector3.Slerp(dir, nearestBandPoint, Mathf.Pow(Random.value, bandPower));
            // if(Vector3.Dot(dir, Vector3.up) < 0) dir = -dir;
            var size            = Random.Range(this.size.x, this.size.y);
            var stellarDistance = Mathf.Pow(Random.value, 0.5f);
            size = Mathf.LerpUnclamped(size, this.size.x, stellarDistance * stellarDistanceVariation);
            var color = GetBlackbodyColor(Mathf.Pow(Random.value, 2) * 28000 + 1000);

            var pos = dir * (distance + size);
            var rot = Quaternion.LookRotation(pos);

            // Redshift
            color = Color.LerpUnclamped(color, BlackbodyColors[0], Mathf.Pow(stellarDistance, 2));

            // Renormalization
            color = Color.LerpUnclamped(color, Color.white, 1 - starColorInfluence);

            // Dimming by distance
            color *= Mathf.LerpUnclamped(1, 0.1f, stellarDistance);

            // Build mesh
            var index = i * 4;
            verts[index + 0]  = rot * new Vector3(-size, -size, 0) + pos;
            verts[index + 1]  = rot * new Vector3(-size, size,  0) + pos;
            verts[index + 2]  = rot * new Vector3(size,  size,  0) + pos;
            verts[index + 3]  = rot * new Vector3(size,  -size, 0) + pos;
            colors[index + 0] = color;
            colors[index + 1] = color;
            colors[index + 2] = color;
            colors[index + 3] = color;
            uvs[index + 0]    = new Vector2(0, 0);
            uvs[index + 1]    = new Vector2(0, 1);
            uvs[index + 2]    = new Vector2(1, 1);
            uvs[index + 3]    = new Vector2(1, 0);
            var triIndex = i * 6;
            tris[triIndex + 0] = index + 0;
            tris[triIndex + 1] = index + 1;
            tris[triIndex + 2] = index + 2;
            tris[triIndex + 3] = index + 0;
            tris[triIndex + 4] = index + 2;
            tris[triIndex + 5] = index + 3;
        }

        var mesh = new Mesh();
        mesh.vertices  = verts;
        mesh.colors    = colors;
        mesh.triangles = tris;
        mesh.uv        = uvs;
        return mesh;
    }

    /*
    1000 K  #ff3800 
    2000 K  #ff8912 
    3000 K  #ffb46b 
    4000 K  #ffd1a3 
    5000 K  #ffe4ce 
    6000 K  #fff3ef 
    7000 K  #f5f3ff 
    8000 K  #e3e9ff 
    9000 K  #d6e1ff 
    10000 K  #ccdbff 
    11000 K  #c4d7ff 
    12000 K  #bfd3ff 
    13000 K  #bad0ff 
    14000 K  #b6ceff 
    15000 K  #b3ccff 
    16000 K  #b0caff 
    17000 K  #aec8ff 
    18000 K  #acc7ff 
    19000 K  #aac6ff 
    20000 K  #a8c5ff 
    21000 K  #a7c4ff 
    22000 K  #a6c3ff 
    23000 K  #a4c2ff 
    24000 K  #a3c2ff 
    25000 K  #a3c1ff 
    26000 K  #a2c0ff 
    27000 K  #a1c0ff 
    28000 K  #a0bfff 
    29000 K  #a0bfff 
     */

    private Color[] BlackbodyColors = new[] {
        new Color(1.0f,                0.2196078431372549f, 0.0f),                 // 1000 K
        new Color(1.0f,                0.5294117647058824f, 0.07058823529411765f), // 2000 K
        new Color(1.0f,                0.7058823529411765f, 0.4196078431372549f),  // 3000 K
        new Color(1.0f,                0.8196078431372549f, 0.6392156862745098f),  // 4000 K
        new Color(1.0f,                0.8941176470588236f, 0.807843137254902f),   // 5000 K
        new Color(1.0f,                0.9529411764705882f, 0.9372549019607843f),  // 6000 K
        new Color(0.9607843137254902f, 0.9529411764705882f, 1.0f),                 // 7000 K
        new Color(0.8901960784313725f, 0.9137254901960784f, 1.0f),                 // 8000 K
        new Color(0.8392156862745098f, 0.8823529411764706f, 1.0f),                 // 9000 K
        new Color(0.8f,                0.8588235294117647f, 1.0f),                 // 10000 K
        new Color(0.7686274509803922f, 0.8431372549019608f, 1.0f),                 // 11000 K
        new Color(0.7490196078431373f, 0.8274509803921568f, 1.0f),                 // 12000 K
        new Color(0.7294117647058823f, 0.8156862745098039f, 1.0f),                 // 13000 K
        new Color(0.7137254901960784f, 0.807843137254902f,  1.0f),                 // 14000 K
        new Color(0.7019607843137254f, 0.796078431372549f,  1.0f),                 // 15000 K
        new Color(0.6901960784313725f, 0.792156862745098f,  1.0f),                 // 16000 K
        new Color(0.6823529411764706f, 0.7843137254901961f, 1.0f),                 // 17000 K
        new Color(0.6745098039215687f, 0.7843137254901961f, 1.0f),                 // 18000 K
        new Color(0.6666666666666666f, 0.7843137254901961f, 1.0f),                 // 19000 K
        new Color(0.6588235294117647f, 0.7725490196078432f, 1.0f),                 // 20000 K
        new Color(0.6549019607843137f, 0.7686274509803922f, 1.0f),                 // 21000 K
        new Color(0.6509803921568628f, 0.7647058823529411f, 1.0f),                 // 22000 K
        new Color(0.6431372549019608f, 0.7607843137254902f, 1.0f),                 // 23000 K
        new Color(0.6392156862745098f, 0.7607843137254902f, 1.0f),                 // 24000 K
        new Color(0.6392156862745098f, 0.7568627450980392f, 1.0f),                 // 25000 K
        new Color(0.6352941176470588f, 0.7529411764705882f, 1.0f),                 // 26000 K
        new Color(0.6313725490196078f, 0.7529411764705882f, 1.0f),                 // 27000 K
        new Color(0.6274509803921569f, 0.7490196078431373f, 1.0f),                 // 28000 K
        new Color(0.6274509803921569f, 0.7490196078431373f, 1.0f)                  // 29000 K
    };

    private Color GetBlackbodyColor(float temperature) {
        if (temperature <= 1000.0f) { return BlackbodyColors[0]; }

        if (temperature >= 29000.0f) { return BlackbodyColors[BlackbodyColors.Length - 1]; }

        float t = (temperature - 1000) / 1000.0f;
        int   i = Mathf.FloorToInt(t);
        return Color.Lerp(BlackbodyColors[i], BlackbodyColors[i + 1], t - i);
    }

    private void OnValidate() { Filter.sharedMesh = Generate(); }
}