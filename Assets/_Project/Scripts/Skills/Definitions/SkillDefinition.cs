using System;
using System.Collections.Generic;
using UnityEngine;

public enum SkillType
{
    Active,
    Passive,
    Triggered,
    Toggle
}

[System.Flags]
public enum SkillTags
{
    None = 0,
    Physical = 1 << 0,
    Magical = 1 << 1,
    Fire = 1 << 2,
    Ice = 1 << 3,
    Lightning = 1 << 4,
    Melee = 1 << 5,
    Ranged = 1 << 6,
    Area = 1 << 7,
    Buff = 1 << 8,
    Debuff = 1 << 9,
    Movement = 1 << 10
}

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Skills/Skill Definition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    public string skillId;
    public string displayName;
    [TextArea(3, 5)]
    public string description;
    public Sprite icon;
    public SkillType type = SkillType.Active;
    public SkillTags tags = SkillTags.None;
    
    [Header("Requirements")]
    public int requiredLevel = 1;
    public List<string> prerequisiteSkillIds;
    public List<RequiredStat> requiredStats;
    
    [Header("Progression")]
    [Min(1)]
    public int maxLevel = 5;
    public int pointsPerLevel = 1;
    
    [Header("Base Properties")]
    public float baseCooldown = 1f;
    public float baseManaCost = 10f;
    public float baseCastTime = 0f;
    
    [Header("Effects")]
    public List<SkillEffectDefinition> baseEffects;
    public List<SkillUpgradeTier> upgradeTiers;
    
    [Header("Visual & Audio")]
    public GameObject castingVFX;
    public GameObject impactVFX;
    public AudioClip castSound;
    public AudioClip impactSound;
    
    public SkillUpgradeTier GetTier(int level)
    {
        if (upgradeTiers == null || upgradeTiers.Count == 0) return null;
        
        int index = Mathf.Clamp(level - 1, 0, upgradeTiers.Count - 1);
        return upgradeTiers[index];
    }
    
    public bool MeetsRequirements(GameObject caster)
    {
        var stats = caster.GetComponent<PlayerStats>();
        if (stats == null) return false;

        // Check level requirement
        if (stats.Level < requiredLevel) 
            return false;

        // Check stat requirements
        foreach (var req in requiredStats)
        {
            if (!req.IsMet(caster)) return false;
        }
        
        return true;
    }
}

[Serializable]
public class SkillUpgradeTier
{
    public string tierName;
    [TextArea(2, 4)]
    public string description;
    public List<ParameterModification> modifications;
    public List<SkillEffectDefinition> additionalEffects;
}

[Serializable]
public class ParameterModification
{
    public string parameterKey;
    public float additiveBonus;
    public float multiplicativeBonus = 1f;
    public bool overrideBase;
    public float overrideValue;
}

[Serializable]
public class RequiredStat
{
    public enum StatType { Strength, Intelligence, Agility, Custom }
    public StatType type;
    public string customStatName;
    public int requiredValue;
    
    public bool IsMet(GameObject caster)
    {
        // Implementation depends on your stat system
        return true;
    }
}