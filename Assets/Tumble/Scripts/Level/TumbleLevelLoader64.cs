using System;
using System.Linq;

using UdonSharp;

using UnityEngine;

using VRC.Compression;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;

using Random = UnityEngine.Random;


public class TumbleLevelLoader64 : UdonSharpBehaviour {
    public const int MaxSearchDistance = 512;
    public const int MaxMatchLength    = 63;

    public string dataToLoad;

    private void Start() {
        var data = DeserializeLevelData(dataToLoad);
        Debug.Log(LevelDataToString(data));
    }

    public GameObject[] levelElements;

    public GameObject GetElementPrefab(int elementId) {
        if (elementId < 0 || elementId >= levelElements.Length) {
            Debug.LogError("Invalid element ID: " + elementId);
            return null;
        }

        return levelElements[elementId];
    }

    // ChunkCount
    // ChunkIndex
    // RotationCount
    // Rotation
    // ElementCount
    // ElementId
    // YCount
    // Y
    // XCount
    // X
    public static string SerializeLevel(TumbleLevel level) {
        var root = level.levelRoot;
        if (root == null) return "";

        var chunks = new DataDictionary();

        for (var i = 0; i < root.childCount; i++) {
            var child     = root.GetChild(i);
            var elementId = int.Parse(child.name);

            for (var e = 0; e < child.childCount; e++) {
                var element = child.GetChild(e);
                StoreElement(chunks, element, elementId);
            }
        }

        var levelData = new DataList(); // byte array

        AddInt(levelData, (int)LevelDataFormatKey.GameVersion);
        AddInt(levelData, Universe.Version);

        // if (level.levelId != -1) {
            // AddInt(levelData, (int)LevelDataFormatKey.LevelID);
            // AddInt(levelData, level.levelId);
        // }

        AddInt(levelData, (int)LevelDataFormatKey.VRChatUsername);
        AddString(levelData, level.room.roomOwner);

        AddInt(levelData, (int)LevelDataFormatKey.LevelName);
        AddString(levelData, level.levelName);
        AddInt(levelData, (int)LevelDataFormatKey.LevelDescription);
        AddString(levelData, level.levelDescription);
        AddInt(levelData, (int)LevelDataFormatKey.LevelTags);
        AddString(levelData, string.Join(" ", level.tags));

        var data      = new DataList(); // byte array
        var chunkKeys = chunks.GetKeys().ToArray();
        AddInt(data, chunkKeys.Length);

        for (var i = 0; i < chunkKeys.Length; i++) {
            var chunkIndex = chunkKeys[i].Int;
            AddInt(data, chunkIndex);
            var chunk        = chunks[chunkIndex].DataDictionary;
            var rotationKeys = chunk.GetKeys().ToArray();
            AddInt(data, rotationKeys.Length);

            for (var r = 0; r < rotationKeys.Length; r++) {
                var rotationKey = rotationKeys[r].Int;
                AddInt(data, rotationKey);

                var elements    = chunk[rotationKey].DataDictionary;
                var elementKeys = elements.GetKeys().ToArray();
                AddInt(data, elementKeys.Length);

                for (var e = 0; e < elementKeys.Length; e++) {
                    var elementKey = elementKeys[e].Int;
                    AddInt(data, elementKey);

                    var yPositions = elements[elementKey].DataDictionary;
                    var yKeys      = yPositions.GetKeys().ToArray();
                    AddInt(data, yKeys.Length);

                    for (var y = 0; y < yKeys.Length; y++) {
                        var xKey = yKeys[y].Byte;
                        data.Add(xKey);

                        var xPositions = yPositions[xKey].DataList;
                        AddInt(data, xPositions.Count);
                        for (var x = 0; x < xPositions.Count; x++) data.Add(xPositions[x]);
                    }
                }
            }
        }

        AddInt(levelData, (int)LevelDataFormatKey.LevelData);
        AddInt(levelData, (int)data.Count);
        foreach (var b in data.ToArray()) levelData.Add(b);

        AddInt(levelData, (int)LevelDataFormatKey.EndOfData);

        var bytes                                          = new byte[levelData.Count];
        for (var i = 0; i < levelData.Count; i++) bytes[i] = (byte)levelData[i].Int;

        // var compressedBytes = Compress(bytes);
        // Debug.Log("Serialized level: " + data.Count + " bytes, compressed to " + compressedBytes.Length + " bytes");

        return EncodeBinaryToString(bytes);
    }

    private static void StoreElement(DataDictionary chunks, Transform element, int elementId) {
        var worldPos = new Vector3Int(
            Mathf.RoundToInt(element.localPosition.x),
            Mathf.RoundToInt(element.localPosition.y),
            Mathf.RoundToInt(element.localPosition.z)
        );

        var chunkPosIndex = new Vector3Int(
            Mathf.FloorToInt(worldPos.x / 32f),
            Mathf.FloorToInt(worldPos.y / 32f),
            Mathf.FloorToInt(worldPos.z / 32f)
        );

        var chunkPos = chunkPosIndex * 32;

        var localPos      = worldPos - chunkPos;
        var locationBytes = new byte[2];
        locationBytes[0] =  (byte)localPos.x; // XXXXXYYY YYZZZZZ- -ZZZZZYY YYYXXXXX
        locationBytes[0] |= (byte)((localPos.y << 5) & 0b11100000);
        locationBytes[1] =  (byte)((localPos.y >> 3) & 0b00000011);
        locationBytes[1] |= (byte)((localPos.z << 2) & 0b11111100);

        chunkPosIndex += new Vector3Int(16, 16, 16);
        var chunkIndex = chunkPosIndex.x * 1024 + chunkPosIndex.y * 32 + chunkPosIndex.z;

        var r = EncodeRotation(element.rotation);
        // Really there are only 24 different rotations, so I will need to optimize this later

        if (!chunks.ContainsKey(chunkIndex)) chunks.Add(chunkIndex, new DataDictionary());
        var rotations = chunks[chunkIndex].DataDictionary;

        if (!rotations.ContainsKey(r)) rotations.Add(r, new DataDictionary());
        var elements = rotations[r].DataDictionary;

        if (!elements.ContainsKey(elementId)) elements.Add(elementId, new DataDictionary());
        var xPositions = elements[elementId].DataDictionary;

        if (!xPositions.ContainsKey(locationBytes[0])) xPositions.Add(locationBytes[0], new DataList());
        var yPositions = xPositions[locationBytes[0]].DataList;

        yPositions.Add(locationBytes[1]);
    }

    public DataDictionary DeserializeLevelData(string data) {
        var bytes = DecodeStringToBinary(data);
        var levelData = new DataDictionary();
        var offset    = 0;

        while (offset < bytes.Length) {
            var key = TakeInt(bytes, ref offset);

            switch ((LevelDataFormatKey)key) {
                case LevelDataFormatKey.GameVersion:
                    var version = TakeInt(bytes, ref offset);
                    levelData.Add((int)LevelDataFormatKey.GameVersion, version);
                    break;
                // case LevelDataFormatKey.LevelID:
                //     var levelId = TakeInt(bytes, ref offset);
                //     levelData.Add((int)LevelDataFormatKey.LevelID, levelId);
                //     break;
                case LevelDataFormatKey.VRChatUsername:
                    var username = TakeString(bytes, ref offset);
                    levelData.Add((int)LevelDataFormatKey.VRChatUsername, username);
                    break;
                case LevelDataFormatKey.LevelName:
                    var levelName = TakeString(bytes, ref offset);
                    levelData.Add((int)LevelDataFormatKey.LevelName, levelName);
                    break;
                case LevelDataFormatKey.LevelDescription:
                    var levelDescription = TakeString(bytes, ref offset);
                    levelData.Add((int)LevelDataFormatKey.LevelDescription, levelDescription);
                    // Debug.Log($"Level Description: {levelDescription}");
                    break;
                case LevelDataFormatKey.LevelTags:
                    var levelTags = TakeString(bytes, ref offset);
                    levelData.Add((int)LevelDataFormatKey.LevelTags, levelTags);
                    break;
                case LevelDataFormatKey.LevelData:
                    levelData.Add((int)LevelDataFormatKey.LevelData, DeserializeChunkData(bytes, ref offset));
                    break;
                case LevelDataFormatKey.EndOfData: return levelData;
            }
        }

        return levelData;
    }

    public void LoadLevelFromData(TumbleLevel level) {
        level.levelName        = level.levelData[(int)LevelDataFormatKey.LevelName].String;
        level.levelDescription = level.levelData[(int)LevelDataFormatKey.LevelDescription].String;
        level.tags            = level.levelData[(int)LevelDataFormatKey.LevelTags].String.Split(' ');
        
        var chunks = level.levelData[(int)LevelDataFormatKey.LevelData].DataDictionary;

        var chunkKeys = chunks.GetKeys().ToArray();

        for (var i = 0; i < chunkKeys.Length; i++) {
            var chunkIndex = chunkKeys[i].Int;

            var chunkPos = new Vector3Int(
                chunkIndex / 1024,
                (chunkIndex / 32) % 32,
                chunkIndex % 32
            );

            chunkPos -= new Vector3Int(16, 16, 16);
            chunkPos *= 32;
            LoadChunk(chunks[chunkIndex].DataDictionary, level, chunkPos);
        }
    }

    private DataDictionary DeserializeChunkData(byte[] bytes, ref int offset) {
        var dataLength = TakeInt(bytes, ref offset);

        var chunks     = new DataDictionary();
        var chunkCount = TakeInt(bytes, ref offset);

        for (var c = 0; c < chunkCount; c++) {
            var chunkIndex = TakeInt(bytes, ref offset);
            var chunk      = new DataDictionary();

            var rotationCount = TakeInt(bytes, ref offset);

            for (var r = 0; r < rotationCount; r++) {
                var rotation = TakeInt(bytes, ref offset);
                Debug.Log($"Rotation: {rotation}");
                var elements = new DataDictionary();

                var elementCount = TakeInt(bytes, ref offset);

                for (var e = 0; e < elementCount; e++) {
                    var elementId  = TakeInt(bytes, ref offset);
                    var yPositions = new DataDictionary();

                    var yCount = TakeInt(bytes, ref offset);

                    for (var y = 0; y < yCount; y++) {
                        var xKey = bytes[offset++];

                        var xPositions = new DataList();

                        var xCount = TakeInt(bytes, ref offset);

                        for (var x = 0; x < xCount; x++) xPositions.Add(bytes[offset++]);

                        yPositions.Add(xKey, xPositions);
                    }

                    elements.Add(elementId, yPositions);
                }

                chunk.Add(rotation, elements);
            }

            chunks.Add(chunkIndex, chunk);
        }

        return chunks;
    }

    private void LoadChunk(DataDictionary chunk, TumbleLevel level, Vector3Int offset) {
        var rotations = chunk.GetKeys().ToArray();

        for (var r = 0; r < rotations.Length; r++) {
            var rotationIndex = rotations[r].Int;
            var rotation      = DecodeRotation(rotationIndex);

            var elements    = chunk[rotationIndex].DataDictionary;
            var elementKeys = elements.GetKeys().ToArray();

            for (var e = 0; e < elementKeys.Length; e++) {
                var elementId     = elementKeys[e].Int;
                var elementHolder = level.GetElementHolder(elementId);

                var yPositions = elements[elementId].DataDictionary;
                var yKeys      = yPositions.GetKeys().ToArray();

                for (var y = 0; y < yKeys.Length; y++) {
                    var xKey       = yKeys[y].Byte;
                    var xPositions = yPositions[xKey].DataList;

                    for (var x = 0; x < xPositions.Count; x++) {
                        var locationBytes = new byte[2];
                        locationBytes[0] = xKey;
                        locationBytes[1] = xPositions[x].Byte;

                        var localPos = new Vector3Int(
                            locationBytes[0] & 0b00011111,
                            (locationBytes[0] >> 5) | ((locationBytes[1] & 0b00000011) << 3),
                            (locationBytes[1] >> 2) & 0b00111111
                        );

                        var worldPos = localPos + offset;

                        var elementObject = Instantiate(levelElements[elementId], elementHolder);
                        elementObject.transform.localPosition = worldPos;
                        elementObject.transform.localRotation = rotation;
                    }
                }
            }
        }
    }

    public static int EncodeRotation(Quaternion rotation) {
        var rot = new Vector3Int(
            (int)Mathf.Repeat(Mathf.RoundToInt(rotation.eulerAngles.x / 90), 4),
            (int)Mathf.Repeat(Mathf.RoundToInt(rotation.eulerAngles.y / 90), 4),
            (int)Mathf.Repeat(Mathf.RoundToInt(rotation.eulerAngles.z / 90), 4)
        );

        return (int)(rot.x * 16 + rot.y * 4 + rot.z);
    }

    public static Quaternion DecodeRotation(int rotation) {
        var x = rotation / 16;
        var y = (rotation / 4) % 4;
        var z = rotation % 4;
        return Quaternion.Euler(x * 90, y * 90, z * 90);
    }

    public static void AddString(DataList data, string value) {
        AddInt(data, value.Length);

        for (var i = 0; i < value.Length; i++) {
            var c = value[i];
            data.Add((byte)(c & 0xff));
            data.Add((byte)(c >> 8));
        }
    }

    public static string TakeString(byte[] data, ref int offset) {
        var length = TakeInt(data, ref offset);
        var result = "";

        for (var i = 0; i < length; i++) {
            var c = data[offset] | (data[offset + 1] << 8);
            result += (char)c;
            offset += 2;
        }

        return result;
    }

    private static void AddInt(DataList data, int value) {
        var encoded = EncodeInt32(value);
        for (var i = 0; i < encoded.Length; i++) data.Add(encoded[i]);
    }

    private static int TakeInt(byte[] data, ref int offset) {
        var value = DecodeInt32(data, offset, out var usedBytes);
        offset += usedBytes;
        return value;
    }

    public static char ToBase64(int value) {
        if (value < 0 || value >= 64) Debug.LogError("Invalid base 64 value: " + value);
        if (value < 26) return (char)('A' + value);
        if (value < 52) return (char)('a' + value - 26);
        if (value < 62) return (char)('0' + value - 52);
        if (value == 62) return '+';

        return '/';
    }

    public static int FromBase64(char value) {
        if (value >= 'A' && value <= 'Z') return value - 'A';
        if (value >= 'a' && value <= 'z') return value - 'a' + 26;
        if (value >= '0' && value <= '9') return value - '0' + 52;
        if (value == '+') return 62;
        if (value == '/') return 63;
        if (value == '=') return 0;

        Debug.LogError("Invalid base 64 character: " + value);
        return 0;
    }

    public static string EncodeBinaryToString(byte[] data) {
        var result = "";

        for (var i = 0; i < data.Length; i += 3) {
            var chunk = (data[i] << 16) | (i + 1 < data.Length ? data[i + 1] << 8 : 0) | (i + 2 < data.Length ? data[i + 2] : 0);
            result += ToBase64((chunk >> 18) & 63);
            result += ToBase64((chunk >> 12) & 63);

            if (i + 1 < data.Length)
                result += ToBase64((chunk >> 6) & 63);
            else
                result += "=";

            if (i + 2 < data.Length)
                result += ToBase64(chunk & 63);
            else
                result += "=";
        }

        return result;
    }

    public static byte[] DecodeStringToBinary(string data) {
        var result = new byte[data.Length * 3 / 4];
        var offset = 0;

        for (var i = 0; i < data.Length; i += 4) {
            var chunk = (FromBase64(data[i]) << 18) | (FromBase64(data[i + 1]) << 12) | (i + 2 < data.Length ? FromBase64(data[i + 2]) << 6 : 0) | (i + 3 < data.Length ? FromBase64(data[i + 3]) : 0);
            result[offset++] = (byte)((chunk >> 16) & 255);
            if (i + 2 < data.Length) result[offset++] = (byte)((chunk >> 8) & 255);
            if (i + 3 < data.Length) result[offset++] = (byte)(chunk & 255);
        }

        return result;
    }

    public static string CompressString(string input) {
        var compressedData      = new DataList();
        var currentIndex        = 0;
        var lastBlockSize       = 0;
        var lastBlockStartIndex = 0;
        var newString           = "";

        while (currentIndex < input.Length) {
            var longestMatchLength = 0;
            var longestMatchIndex  = -1;

            var currentLength = newString.Length;

            for (var i = Mathf.Max(0, currentLength - MaxSearchDistance); i < currentLength; i++) {
                var matchLength = 0;

                while (
                    i + matchLength < currentLength
                    && currentIndex + matchLength < input.Length
                    && newString[i + matchLength] == input[currentIndex + matchLength]
                ) {
                    matchLength++;
                    if (matchLength == MaxMatchLength) break;
                }

                if (matchLength > longestMatchLength) {
                    longestMatchLength = matchLength;
                    longestMatchIndex  = i;
                }
            }

            if (longestMatchLength > 4 && currentIndex + longestMatchLength < input.Length) {
                var blockSize = EncodeInt32Base64(lastBlockSize);
                // Insert the block size at the block offset
                newString     = newString.Substring(0, lastBlockStartIndex) + blockSize + newString.Substring(lastBlockStartIndex);
                lastBlockSize = 0;

                var offset = currentLength - longestMatchIndex;
                var length = longestMatchLength;
                newString           += $"{EncodeInt32Base64(offset)}{EncodeInt32Base64(length)}{input[currentIndex + longestMatchLength]}";
                currentIndex        += longestMatchLength + 1;
                lastBlockStartIndex =  newString.Length;
            } else {
                newString += input[currentIndex];
                currentIndex++;
                lastBlockSize++;
            }
        }

        return newString;
    }

    public static byte[] CompressBytes(byte[] input) {
        var currentIndex        = 0;
        var lastBlockSize       = 0;
        var lastBlockStartIndex = 0;
        var newData             = new DataList();

        while (currentIndex < input.Length) {
            var longestMatchLength = 0;
            var longestMatchIndex  = -1;

            var currentLength = newData.Count;

            for (var i = Mathf.Max(0, currentLength - MaxSearchDistance); i < currentLength; i++) {
                var matchLength = 0;

                while (
                    i + matchLength < currentLength
                    && currentIndex + matchLength < input.Length
                    && newData[i + matchLength].Byte == input[currentIndex + matchLength]
                ) {
                    matchLength++;
                    if (matchLength == MaxMatchLength) break;
                }

                if (matchLength > longestMatchLength) {
                    longestMatchLength = matchLength;
                    longestMatchIndex  = i;
                }
            }

            if (
                longestMatchLength > 3
                && currentIndex + longestMatchLength < input.Length
                // && lastBlockSize > 10
            ) {
                // Debug.Log("Match: " + longestMatchLength + " at " + longestMatchIndex + " from " + currentIndex);
                var d         = newData.GetRange(0, lastBlockStartIndex);
                var blockSize = EncodeInt32(lastBlockSize);
                for (var i = 0; i < blockSize.Length; i++) d.Add(blockSize[i]);
                d.AddRange(newData.GetRange(lastBlockStartIndex, currentLength - lastBlockStartIndex));
                newData = d;

                lastBlockSize = 0;
                if (lastBlockStartIndex <= longestMatchIndex) longestMatchIndex += blockSize.Length;

                var offset      = newData.Count - longestMatchIndex;
                var length      = longestMatchLength;
                var offsetBytes = EncodeInt32(offset);
                for (var i = 0; i < offsetBytes.Length; i++) newData.Add(offsetBytes[i]);
                var lengthBytes = EncodeInt32(length);
                for (var i = 0; i < lengthBytes.Length; i++) newData.Add(lengthBytes[i]);

                currentIndex        += longestMatchLength;
                lastBlockStartIndex =  newData.Count;
            } else {
                newData.Add(input[currentIndex]);
                currentIndex++;
                lastBlockSize++;
            }
        }

        // Insert the block size at the block offset
        var d1         = newData.GetRange(0, lastBlockStartIndex);
        var blockSize1 = EncodeInt32(lastBlockSize);
        for (var i = 0; i < blockSize1.Length; i++) d1.Add(blockSize1[i]);
        d1.AddRange(newData.GetRange(lastBlockStartIndex, newData.Count - lastBlockStartIndex));
        newData = d1;

        var output                                        = new byte[newData.Count];
        for (var i = 0; i < newData.Count; i++) output[i] = newData[i].Byte;

        return output;
    }

    public static string DecompressString(string data) {
        if (data.Length == 0) return "";

        var blockLength = DecodeInt32Base64(data, out var blockLengthCharacters);
        var result      = data.Substring(blockLengthCharacters - 1, 1);
        var inBlock     = true;

        for (var i = blockLengthCharacters; i < data.Length; i++) {
            if (inBlock) {
                result += data[i];
                blockLength--;
                if (blockLength == 0) inBlock = false;
            } else {
                var a0          = FromBase64(data[i]);
                var a1          = FromBase64(data[i + 1]);
                var a           = a0 | ((a1 << 6) & 0b11000000);
                var b           = a1 >> 2;
                var matchLength = b & 0b1111;
                var matchOffset = a;
                result      += result.Substring(i - matchOffset, matchLength);
                result      += data[i + 2];
                i           += 2;
                blockLength =  DecodeInt32Base64(data.Substring(i), out blockLengthCharacters);
                i           += blockLengthCharacters - 1;
                inBlock     =  true;
            }
        }

        return result;
    }

    public static byte[] DecompressBytes(byte[] data) {
        if (data.Length == 0) return new byte[0];

        var offset      = 0;
        var blockLength = TakeInt(data, ref offset);
        if (blockLength == 0) return new byte[0];

        var inBlock = true;

        var result = new DataList();

        while (offset < data.Length) {
            if (inBlock) {
                result.Add(data[offset]);
                offset++;
                blockLength--;
                if (blockLength == 0) inBlock = false;
            } else {
                var wordOffsetOriginal = offset;
                var wordOffset         = TakeInt(data, ref offset);
                var wordLength         = TakeInt(data, ref offset);

                for (var i = 0; i < wordLength; i++) result.Add(result[result.Count - wordOffset]);

                if (offset >= data.Length) break;

                blockLength = TakeInt(data, ref offset);
                inBlock     = true;
            }
        }

        var finalResult                                       = new byte[result.Count];
        for (var i = 0; i < result.Count; i++) finalResult[i] = result[i].Byte;

        return finalResult;
    }

    public static byte[] EncodeInt32(int value) {
        var list = new DataList();

        if (value < 0) Debug.LogError("Cannot encode negative integers to variable length byte arrays");

        do {
            var lowest7Bits = (byte)(value & 0x7f);
            value >>= 7;
            if (value > 0) lowest7Bits |= 128;
            list.Add(lowest7Bits);
        } while (value > 0);

        var result                                     = new byte[list.Count];
        for (var i = 0; i < list.Count; i++) result[i] = list[i].Byte;

        return result;
    }

    public static int DecodeInt32(byte[] data, int offset, out int usedBytes) {
        var result = 0;
        var shift  = 0;

        usedBytes = 0;

        for (var i = offset; i < data.Length; i++) {
            var value = data[i];
            result |= (value & 0x7f) << shift;
            shift  += 7;

            usedBytes++;

            if ((value & 128) == 0) break;
        }

        return result;
    }

    public static string EncodeInt32Base64(int value) {
        var list = new DataList();

        if (value < 0) Debug.LogError("Cannot encode negative integers to variable length byte arrays");

        do {
            var lowest5Bits = (byte)(value & 0x1f);
            value >>= 5;
            if (value > 0) lowest5Bits |= 32;
            list.Add(lowest5Bits);
        } while (value > 0);

        var result                                  = "";
        for (var i = 0; i < list.Count; i++) result += ToBase64(list[i].Byte);

        return result;
    }

    public static int DecodeInt32Base64(string data, out int length) {
        // May not use the entire string
        var result = 0;
        var shift  = 0;

        length = 0;

        for (var i = 0; i < data.Length; i++) {
            var value = FromBase64(data[i]);
            result |= (value & 0x1f) << shift;
            shift  += 5;

            length++;

            if ((value & 32) == 0) break;
        }

        return result;
    }

    public static byte[] EncodeVariableLengthByteArray(byte[] data) {
        var result = new DataList();

        for (var i = 0; i < data.Length; i++) {
            var value = data[i];

            do {
                var lowest7Bits = (byte)(value & 0x7f);
                value >>= 7;
                if (value > 0) lowest7Bits |= 128;
                result.Add(lowest7Bits);
            } while (value > 0);
        }

        var finalResult                                       = new byte[result.Count];
        for (var i = 0; i < result.Count; i++) finalResult[i] = result[i].Byte;

        return finalResult;
    }

    public static byte[] DecodeVariableLengthByteArray(byte[] data) {
        var  result = new DataList();
        byte value  = 0;
        byte shift  = 0;

        for (var i = 0; i < data.Length; i++) {
            var current = data[i];
            value |= (byte)((current & 0x7f) << shift);
            shift += 7;

            if ((current & 128) == 0) {
                result.Add(value);
                value = 0;
                shift = 0;
            }
        }

        var finalResult                                       = new byte[result.Count];
        for (var i = 0; i < result.Count; i++) finalResult[i] = result[i].Byte;

        return finalResult;
    }

    private static bool CompareByteArrays(DataList a, DataList b, int index) {
        var key             = a[index].DataList;
        var extrapolatedKey = new DataList();
        extrapolatedKey.Add(key[1]);

        while (key[0].Byte != 0) {
            key = a[key[0].Byte - 1].DataList;
            extrapolatedKey.Add(key[1]);
            if (extrapolatedKey.Count > b.Count) return false;
        }

        if (extrapolatedKey.Count != b.Count) return false;

        for (var i = 0; i < extrapolatedKey.Count; i++) {
            if (extrapolatedKey[i].Byte != b[i].Byte) return false;
        }

        return true;
    }

    public static string LevelDataToString(DataDictionary data) {
        var result = "";
        var keys   = data.GetKeys().ToArray();

        for (var i = 0; i < keys.Length; i++) {
            var key   = (LevelDataFormatKey)(int)keys[i].Number;
            var value = data[keys[i]];

            switch (key) {
                case LevelDataFormatKey.GameVersion:
                    result += $"Game Version: {value.Int}";
                    break;
                // case LevelDataFormatKey.LevelID:
                //     result += $"Level ID: {value.Int}";
                //     break;
                case LevelDataFormatKey.VRChatUsername:
                    result += $"VRChat Username: {value.String}";
                    break;
                case LevelDataFormatKey.LevelName:
                    result += $"Level Name: {value.String}";
                    break;
                case LevelDataFormatKey.LevelDescription:
                    result += $"Level Description: {value.String}";
                    break;
                case LevelDataFormatKey.LevelTags:
                    result += $"Level Tags: {value.String}";
                    break;
                case LevelDataFormatKey.LevelData:
                    if (!VRCJson.TrySerializeToJson(value.DataDictionary, JsonExportType.Beautify, out var jsonResult)) jsonResult = "Failed to load";
                    result += $"Level Data: {jsonResult}";
                    break;
            }

            result += "\n";
        }

        return result;
    }
}

public enum LevelDataFormatKey {
    GameVersion      = 0,
    VRChatUsername   = 1,
    LevelName        = 2,
    LevelDescription = 3,
    LevelTags        = 4,
    LevelData        = 5,
    EndOfData        = 6,
}