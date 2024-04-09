using System;

using TMPro;

using UdonSharp;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;


[RequireComponent(typeof(Button))]
public class TumbleButton : UdonSharpBehaviour {
    public RectTransform indentRect;
    public float         normalOffset;
    public float         hoverOffset;
    public float         pressOffset;
    public float         speed = 8f;

    [Header("Icon"), FormerlySerializedAs("onIcons"),]
    public Image[] icons;
    public TextMeshProUGUI[] text;

    public Color normalColor;
    public Color hoverColor;
    public Color pressColor;

    private Button _button;
    private bool   _hovering;
    private bool   _pressing;

    private void Start() { _button = GetComponent<Button>(); }

    private void FixedUpdate() {
        var targetOffset = _pressing
            ? pressOffset
            : _hovering
                ? hoverOffset
                : normalOffset;

        var currentOffset = indentRect.offsetMin.y;
        var newOffset     = Mathf.Lerp(currentOffset, targetOffset, Time.deltaTime * speed);
        indentRect.offsetMin = new Vector2(0, newOffset);
        indentRect.offsetMax = new Vector2(0, newOffset);

        var targetColor = _pressing
            ? pressColor
            : _hovering
                ? hoverColor
                : normalColor;

        foreach (var icon in icons) { icon.color = Color.Lerp(icon.color, targetColor, Time.deltaTime * speed); }
        
        foreach (var t in text) { t.color = Color.Lerp(t.color, targetColor, Time.deltaTime * speed); }
    }

    public void HoverStart() { _hovering = true; }

    public void HoverEnd() { _hovering = false; }

    public void PressStart() { _pressing = true; }

    public void PressEnd() {
        _pressing = false;
        foreach (var behaviour in GetComponents<UdonBehaviour>()) behaviour.SendCustomEvent("OnClick");
    }
}