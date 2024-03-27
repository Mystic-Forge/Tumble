using System;
using System.Collections.Generic;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

using VRC.SDK3.Data;


public class Modifiers : UdonSharpBehaviour {
    [NonSerialized] public ModifiersEnum[] AllModifiers = new ModifiersEnum[3] {
        ModifiersEnum.AllGroundIsDirt,
        ModifiersEnum.InfiniteAirActions,
        ModifiersEnum.NoCooldowns,
    };

    public bool AllGroundIsDirt => allGroundIsDirtToggle.isOn;

    public bool InfiniteAirActions => infiniteAirActionsToggle.isOn;

    public bool NoCooldowns => noCooldownsToggle.isOn;

    public Sprite[] modifierIcons;

    public ModifiersEnum EnabledModifiers {
        get {
            var flags                     = (int)ModifiersEnum.None;
            if (AllGroundIsDirt) flags    |= (int)ModifiersEnum.AllGroundIsDirt;
            if (InfiniteAirActions) flags |= (int)ModifiersEnum.InfiniteAirActions;
            if (NoCooldowns) flags        |= (int)ModifiersEnum.NoCooldowns;

            return (ModifiersEnum)flags;
        }
    }

    public bool AnyEnabled => EnabledModifiers != ModifiersEnum.None;

    public Toggle allGroundIsDirtToggle;
    public Toggle infiniteAirActionsToggle;
    public Toggle noCooldownsToggle;

    public ModifiersEnum[] ExtractModifiers(ModifiersEnum EnabledModifiers) {
        var modifiers = new DataList();

        foreach (var modifier in AllModifiers)
            if (((int)EnabledModifiers & (int)modifier) != 0)
                modifiers.Add(new DataToken((int)modifier));

        var modifierArray                                          = new ModifiersEnum[modifiers.Count];
        for (var i = 0; i < modifiers.Count; i++) modifierArray[i] = (ModifiersEnum)modifiers[i].Int;

        return modifierArray;
    }
    
    public Sprite GetIcon(ModifiersEnum modifier) {
        var po2 = (int)modifier;
        var i   = 0;
        while (po2 > 1) {
            po2 >>= 1;
            i++;
        }
        
        return modifierIcons[i];
    }
}

[Flags]
public enum ModifiersEnum {
    None               = 0,
    All                = ~0,
    AllGroundIsDirt    = 1,
    InfiniteAirActions = 2,
    NoCooldowns        = 4,
}