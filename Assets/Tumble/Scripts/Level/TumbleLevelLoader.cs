using System;
using System.Linq;

using UdonSharp;

using UnityEngine;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

using Random = UnityEngine.Random;


public class TumbleLevelLoader : UdonSharpBehaviour {
    public const int    Version        = 0;
    public const string VersionKey     = "v";
    public const string LevelKey       = "l";
    public const string ChunkKey       = "c";
    public const string ElementListKey = "e";
    public const string RotationKey    = "r";

    public GameObject[] levelElements;

    public string SerializeLevel(TumbleLevel level) {
        var root = level.levelRoot;

        var data = $"{VersionKey}{Version};{LevelKey}{level.levelCode};";

        var chunks = new DataDictionary();

        for (var i = 0; i < root.childCount; i++) {
            var child     = root.GetChild(i);
            var elementId = int.Parse(child.name);

            for (var e = 0; e < child.childCount; e++) {
                var element = child.GetChild(e);

                var localPos = new Vector3Int(
                    Mathf.RoundToInt(element.localPosition.x),
                    Mathf.RoundToInt(element.localPosition.y),
                    Mathf.RoundToInt(element.localPosition.z)
                );

                var chunkPos = new Vector3Int(
                    Mathf.FloorToInt(localPos.x / 62f),
                    Mathf.FloorToInt(localPos.y / 62f),
                    Mathf.FloorToInt(localPos.z / 62f)
                );

                var chunkLocalPos = new Vector3Int(
                    localPos.x - chunkPos.x * 62,
                    localPos.y - chunkPos.y * 62,
                    localPos.z - chunkPos.z * 62
                );

                chunkPos += new Vector3Int(31, 31, 31);

                var chunkId = SerializePosition(chunkPos);
                
                var rot        = SerializeRotation(element.localRotation);
                // var elementPos = $"{pos}"; // XYZR

                if (!chunks.ContainsKey(chunkId)) chunks[chunkId]                         = new DataDictionary();
                var chunk                                                                 = chunks[chunkId].DataDictionary;
                if (!chunk.ContainsKey(elementId.ToString())) chunk[elementId.ToString()] = new DataDictionary();
                var rotations                                                             = chunk[elementId.ToString()].DataDictionary;
                if (!rotations.ContainsKey(rot)) rotations[rot]                           = new DataDictionary();
                var xPositions                                                            = rotations[rot].DataDictionary;
                if (!xPositions.ContainsKey(chunkLocalPos.x)) xPositions[chunkLocalPos.x] = new DataDictionary();
                var yPositions                                                            = xPositions[chunkLocalPos.x].DataDictionary;
                if (!yPositions.ContainsKey(chunkLocalPos.y)) yPositions[chunkLocalPos.y] = new DataList();
                yPositions[chunkLocalPos.y].DataList.Add(new DataToken(chunkLocalPos.z));
            }
        }

        foreach (var chunkKey in chunks.GetKeys().ToArray()) {
            data += $"{ChunkKey}{chunkKey.String}";
            var chunk = chunks[chunkKey].DataDictionary;

            foreach (var elementKey in chunk.GetKeys().ToArray()) {
                data += $"{elementKey}{elementKey.String}:";
                var rotations = chunk[elementKey].DataDictionary;

                foreach (var rotation in rotations.GetKeys().ToArray()) {
                    data += $"{RotationKey}{rotation}[";
                    var xPositions = rotations[rotation].DataDictionary;

                    foreach (var xPos in xPositions.GetKeys().ToArray()) {
                        data += GetCharacter(xPos.Int);
                        var yPositions = xPositions[xPos].DataDictionary;
                        data += "[";

                        foreach (var yPos in yPositions.GetKeys().ToArray()) {
                            data += GetCharacter(yPos.Int);
                            var zPositions = yPositions[yPos].DataList;
                            data += "[";

                            foreach (var zPos in zPositions.ToArray()) {
                                data += GetCharacter(zPos.Int);
                            }

                            data += "]";
                        }

                        data += "]";
                    }
                }

                data += ";";
            }
        }
        return data.ToString();
    }

    public void DeserializeLevel(string data, TumbleLevel level) {
        var parts    = new DataDictionary();
        var elements = new DataDictionary();

        foreach (var p in data.Split(';')) {
            if (p.Length < 2) continue;

            var key   = p.Substring(0, 1);
            var value = p.Substring(1);

            if (key == ElementListKey) {
                var split       = value.Split(':');
                var elementId   = split[0];
                var elementData = split[1];
                elements[elementId] = new DataToken(elementData);
                continue;
            }

            parts[key] = new DataToken(value);
        }

        var levelCode = parts[LevelKey];
        level.levelCode = levelCode.String;

        var root = level.levelRoot;

        foreach (var elementKey in elements.GetKeys().ToArray()) {
            var elementId   = int.Parse(elementKey.String);
            var holder      = level.GetElementHolder(elementId);
            var offset      = 0;
            var elementData = elements[elementKey].String;

            while (offset < elementData.Length) {
                var x   = elementData.Substring(offset,     2);
                var y   = elementData.Substring(offset + 2, 2);
                var z   = elementData.Substring(offset + 4, 2);
                var rot = elementData.Substring(offset + 6, 1);

                var pos      = new Vector3Int(DeserializeNumber(x), DeserializeNumber(y), DeserializeNumber(z));
                var rotation = DeserializeRotation(rot);
                var element  = Instantiate(levelElements[elementId], holder);
                element.transform.localPosition =  pos;
                element.transform.localRotation =  rotation;
                offset                          += 7;
            }
        }
    }

    public static char GetRandomCharacter() {
        var value = Random.Range(0, 62);
        return GetCharacter(value);
    }

    public static char GetCharacter(int value) {
        if (value < 10) return (char)(value + 48); // 0-9
        if (value < 36) return (char)(value + 55); // A-Z

        return (char)(value + 61); // a-z
    }

    public static int GetNumber(char character) {
        if (character >= '0' && character <= '9') return character - 48;
        if (character >= 'A' && character <= 'Z') return character - 55;

        return character - 61;
    }

    public static string SerializeNumber(int number, bool pad = true) {
        if (!pad) return GetCharacter(number).ToString();

        if (number >= 1922 || number < -1922) return "00";

        number += 1922; // Offset the negative numbers to be positive
        var tens = number / 62;
        var ones = number % 62;
        return $"{GetCharacter(tens)}{GetCharacter(ones)}";
    }

    public static string SerializePosition(Vector3 position) => $"{GetCharacter(Mathf.RoundToInt(position.x))}{GetCharacter(Mathf.RoundToInt(position.y))}{GetCharacter(Mathf.RoundToInt(position.z))}";

    public static string SerializeRotation(Quaternion rotation) {
        var rot = rotation.eulerAngles / 90;
        var rx  = (int)rot.x;
        var ry  = (int)rot.y;
        var rz  = (int)rot.z;
        var r   = SerializeNumber(rx * 9 + ry * 3 + rz, false);
        return r;
    }

    public static int DeserializeNumber(string number) {
        if (number.Length == 1) return GetNumber(number[0]);

        var tens = number[0];
        var ones = number[1];
        return (GetNumber(tens) * 62 + GetNumber(ones)) - 1922;
    }

    public static Quaternion DeserializeRotation(string rotation) {
        var r  = DeserializeNumber(rotation);
        var rx = r / 9;
        var ry = (r % 9) / 3;
        var rz = r % 3;
        return Quaternion.Euler(rx * 90, ry * 90, rz * 90);
    }
}