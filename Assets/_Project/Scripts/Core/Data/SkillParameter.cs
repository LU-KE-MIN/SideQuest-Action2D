using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SkillParameterValue
{
    public string key;
    public float baseValue;
    public float additive;
    public float multiplicative = 1f;
    
    public float CalculatedValue => (baseValue + additive) * multiplicative;
}

public class SkillParameters
{
    private Dictionary<string, SkillParameterValue> parameters = new();
    
    public void SetBase(string key, float value)
    {
        if (!parameters.ContainsKey(key))
            parameters[key] = new SkillParameterValue { key = key };
        parameters[key].baseValue = value;
    }
    
    public void AddModifier(string key, float add, float mult = 1f)
    {
        if (!parameters.ContainsKey(key))
            parameters[key] = new SkillParameterValue { key = key };
        
        parameters[key].additive += add;
        parameters[key].multiplicative *= mult;
    }
    
    public float Get(string key, float defaultValue = 0f)
    {
        return parameters.TryGetValue(key, out var param) 
            ? param.CalculatedValue 
            : defaultValue;
    }
    
    public Dictionary<string, float> ToDictionary()
    {
        var dict = new Dictionary<string, float>();
        foreach (var kvp in parameters)
        {
            dict[kvp.Key] = kvp.Value.CalculatedValue;
        }
        return dict;
    }
    
    public void Clear()
    {
        parameters.Clear();
    }
    
    public void Reset(string key)
    {
        if (parameters.ContainsKey(key))
        {
            parameters[key].additive = 0;
            parameters[key].multiplicative = 1f;
        }
    }
}

public static class SkillParameterKeys
{
    public const string Damage = "damage";
    public const string Speed = "speed";
    public const string Range = "range";
    public const string Duration = "duration";
    public const string CooldownReduction = "cooldownReduction";
    public const string ManaCost = "manaCost";
    public const string ManaCostReduction = "manaCostReduction";
    public const string LifeSteal = "lifeSteal";
    public const string CritChance = "critChance";
    public const string CritDamage = "critDamage";
    public const string ProjectileCount = "projectileCount";
    public const string AreaOfEffect = "areaOfEffect";
    public const string DashDistance = "dashDistance";
    public const string InvulnerabilityTime = "invulnerabilityTime";
}