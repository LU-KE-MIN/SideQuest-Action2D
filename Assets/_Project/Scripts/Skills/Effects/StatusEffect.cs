using System;
using UnityEngine;

[Serializable]
public class StatusEffect
{
    public string id;
    public string name;
    public Sprite icon;
    public float duration;
    public float tickInterval = 0f; // 0 = no tick
    public bool isStackable;
    public int maxStacks = 1;
    
    [SerializeField] private EffectModifier[] modifiers;
    
    public EffectModifier[] Modifiers => modifiers;
}

[Serializable]
public class EffectModifier
{
    public enum ModifierType
    {
        Flat,
        Percentage,
        Override
    }
    
    public string statKey;
    public float value;
    public ModifierType type;
}