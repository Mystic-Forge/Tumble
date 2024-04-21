using System;
using System.Collections.Generic;

using TMPro;

using UdonSharp;

using UnityEngine;
using UnityEngine.UI;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon;


[RequireComponent(typeof(Text)), UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class EmojiSupportedText : UdonSharpBehaviour {
    private const int HighSurrogateStart = 0xD800;
    private const int HighSurrogateEnd   = 0xDBFF;
    private const int LowSurrogateStart  = 0xDC00;
    private const int LowSurrogateEnd    = 0xDFFF;

    public InputField      inputField;
    public TextMeshProUGUI target;

    private Text                _text;
    private EmojiSupportManager _emojiSupportManager;

    private void Start() {
        _text                = GetComponent<Text>();
        _emojiSupportManager = GetComponentInParent<Universe>().emojiSupportManager;
    }

    public void OnTextChange() {
        var source                     = _text.text;
        if (inputField != null) source = inputField.text;
        var s =  ToStringWithEmojis(ToUTF32(source));;
        target.text = s;
        // target.SetAllDirty();
        // target.ForceMeshUpdate(forceTextReparsing: true);
    }

    public string ToStringWithEmojis(DataList data) {
        var strings = new DataList();

        for (var index = 0; index < data.Count; index++) {
            var character = data[index].UInt;

            if (!_emojiSupportManager.codeSequence.TryGetValue(character.ToString(), out var emoji)) { strings.Add(((char)character).ToString()); } else {
                var dict = emoji.DataDictionary;

                // if (VRCJson.TrySerializeToJson(dict, JsonExportType.Beautify, out var json))
                    // Debug.Log(json);
                // else
                    // Debug.LogError("Failed to serialize codepoint dictionary: " + json.Error);

                // Debug.Log($"{character:X8} is a part of an emoji");
                // Debug.Log($"{data[index + 1].UInt:X8}");

                while (index + 1 < data.Count && dict.TryGetValue(data[index + 1].UInt.ToString(), out emoji)) {
                    // Debug.Log($"{data[index + 1].UInt:X8} is a part of an emoji");
                    index++;
                    dict = emoji.DataDictionary;
                }

                var emojiIndex = dict["index"];

                if (emojiIndex.TokenType == TokenType.UInt) strings.Add($"\\U{(emojiIndex.UInt + 0xE000):X8}");
            }
        }

        var finalString                                    = "";
        foreach (var str in strings.ToArray()) finalString += str.String;

        return finalString;
    }

    public DataList ToUTF32(string data) {
        var utf32List = new DataList();
        var offset    = 0;

        while (offset < data.Length) {
            var part1 = (ushort)data[offset++]; // High surrogate

            if (part1 >= HighSurrogateStart && part1 <= HighSurrogateEnd) {
                var part2 = (ushort)data[offset++]; // Low surrogate

                if (part2 < LowSurrogateStart || part2 > LowSurrogateEnd) {
                    Debug.LogError("Invalid surrogate pair");
                    return null;
                }

                var utf32Data = UTF16ToUTF32(part1, part2);
                utf32List.Add(utf32Data);
            } else { utf32List.Add(part1); }
        }

        return utf32List;
    }

    private uint UTF16ToUTF32(ushort high, ushort low) {
        low  -= LowSurrogateStart;
        high -= HighSurrogateStart;
        return (uint)((high << 10) + low + 0x10000);
    }
}