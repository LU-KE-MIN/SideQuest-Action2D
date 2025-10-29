# Unity スキル成長システム - 完全版

## プロジェクト構造

```
/Scripts
  /Core
    /Interfaces
      IResourceProvider.cs
      ISkillActivator.cs
      ISkillPointProvider.cs
      IDamageable.cs
    /Events
      SkillEvents.cs
      GameEvents.cs
    /Data
      SkillParameter.cs
      StatusEffect.cs
    PlayerStats.cs
    SaveLoadManager.cs
    
  /Skills
    /Definitions
      SkillDefinition.cs
      SkillEffectDefinition.cs
      SkillComboDefinition.cs
    /Effects
      FireballEffect.cs
      DashEffect.cs
      PassiveStatBoost.cs
      AreaDamageEffect.cs
    /Runtime
      SkillInstance.cs
      SkillInventory.cs
      SkillActivator.cs
      SkillInputHandler.cs
      PassiveSkillManager.cs
      SkillComboManager.cs
    /Executors
      DashExecutor.cs
      ProjectileSpawner.cs
    /Utilities
      SkillParameterKeys.cs
      SkillTagSystem.cs
      
  /Combat
    Projectile.cs
    ProjectilePool.cs
    StatusEffectManager.cs
    DamageCalculator.cs
    
  /UI
    SkillTreeUI.cs
    SkillSlotUI.cs
    CooldownDisplay.cs
```

## 1. Core/Interfaces

### IResourceProvider.cs
```csharp
using System;

public interface IResourceProvider
{
    event Action<float, float> OnResourceChanged;
    
    float CurrentResource { get; }
    float MaxResource { get; }
    bool CanAfford(float cost);
    bool TryConsume(float cost);
    void Restore(float amount);
    void ModifyMax(float amount);
}

public interface ISkillPointProvider
{
    event Action<int> OnPointsChanged;
    
    int AvailablePoints { get; }
    bool TrySpendPoints(int amount);
    void AddPoints(int amount);
}

public interface IDamageable
{
    event Action<float> OnDamageReceived;
    event Action OnDeath;
    
    float CurrentHealth { get; }
    float MaxHealth { get; }
    void TakeDamage(float amount, DamageType type = DamageType.Physical);
    void Heal(float amount);
    bool IsAlive { get; }
}

public enum DamageType
{
    Physical,
    Magical,
    True,
    Fire,
    Ice,
    Lightning
}
```

### ISkillActivator.cs
```csharp
public interface ISkillActivator
{
    bool CanActivate(SkillInstance skill, SkillContext context);
    void Activate(SkillInstance skill, SkillContext context);
    float GetCooldownModifier(SkillInstance skill);
}
```

## 2. Core/Events

### SkillEvents.cs
```csharp
using System;
using UnityEngine;

public static class SkillEvents
{
    public static event Action<SkillInstance> OnSkillUnlocked;
    public static event Action<SkillInstance> OnSkillLevelUp;
    public static event Action<SkillInstance, SkillContext> OnSkillActivated;
    public static event Action<SkillInstance> OnSkillReady;
    public static event Action<SkillInstance, float> OnCooldownUpdate;
    public static event Action<string, string> OnComboExecuted;

    public static void TriggerSkillUnlocked(SkillInstance skill) 
        => OnSkillUnlocked?.Invoke(skill);
    
    public static void TriggerSkillLevelUp(SkillInstance skill) 
        => OnSkillLevelUp?.Invoke(skill);
    
    public static void TriggerSkillActivated(SkillInstance skill, SkillContext context) 
        => OnSkillActivated?.Invoke(skill, context);
    
    public static void TriggerSkillReady(SkillInstance skill) 
        => OnSkillReady?.Invoke(skill);
    
    public static void TriggerCooldownUpdate(SkillInstance skill, float remaining) 
        => OnCooldownUpdate?.Invoke(skill, remaining);
    
    public static void TriggerComboExecuted(string comboId, string executorId) 
        => OnComboExecuted?.Invoke(comboId, executorId);
}
```

## 3. Core/Data

### SkillParameter.cs
```csharp
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
    public const string LifeSteal = "lifeSteal";
    public const string CritChance = "critChance";
    public const string CritDamage = "critDamage";
    public const string ProjectileCount = "projectileCount";
    public const string AreaOfEffect = "areaOfEffect";
    public const string DashDistance = "dashDistance";
    public const string InvulnerabilityTime = "invulnerabilityTime";
}
```

### StatusEffect.cs
```csharp
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
```

## 4. Core

### PlayerStats.cs
```csharp
using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IResourceProvider, ISkillPointProvider
{
    [Header("Level & Experience")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private AnimationCurve xpCurve;
    
    [Header("Resources")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float maxMana = 50f;
    [SerializeField] private float currentMana = 50f;
    [SerializeField] private float manaRegen = 1f;
    
    [Header("Skill Points")]
    [SerializeField] private int skillPoints = 0;
    [SerializeField] private int pointsPerLevel = 1;
    
    // IResourceProvider implementation
    public event Action<float, float> OnResourceChanged;
    public float CurrentResource => currentMana;
    public float MaxResource => maxMana;
    
    // ISkillPointProvider implementation
    public event Action<int> OnPointsChanged;
    public int AvailablePoints => skillPoints;
    
    // Level events
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnXPGained;
    
    void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
    }
    
    void Update()
    {
        RegenerateMana(manaRegen * Time.deltaTime);
    }
    
    public bool CanAfford(float cost)
    {
        return currentMana >= cost;
    }
    
    public bool TryConsume(float cost)
    {
        if (!CanAfford(cost)) return false;
        
        currentMana = Mathf.Max(0, currentMana - cost);
        OnResourceChanged?.Invoke(currentMana, maxMana);
        return true;
    }
    
    public void Restore(float amount)
    {
        currentMana = Mathf.Min(maxMana, currentMana + amount);
        OnResourceChanged?.Invoke(currentMana, maxMana);
    }
    
    public void ModifyMax(float amount)
    {
        maxMana += amount;
        currentMana = Mathf.Min(currentMana, maxMana);
        OnResourceChanged?.Invoke(currentMana, maxMana);
    }
    
    public bool TrySpendPoints(int amount)
    {
        if (skillPoints < amount) return false;
        
        skillPoints -= amount;
        OnPointsChanged?.Invoke(skillPoints);
        return true;
    }
    
    public void AddPoints(int amount)
    {
        skillPoints += amount;
        OnPointsChanged?.Invoke(skillPoints);
    }
    
    public void AddExperience(int amount)
    {
        currentXP += amount;
        OnXPGained?.Invoke(amount, currentXP);
        
        CheckLevelUp();
    }
    
    private void CheckLevelUp()
    {
        int requiredXP = GetRequiredXPForNextLevel();
        
        while (currentXP >= requiredXP)
        {
            currentXP -= requiredXP;
            level++;
            skillPoints += pointsPerLevel;
            
            OnLevelUp?.Invoke(level);
            OnPointsChanged?.Invoke(skillPoints);
            
            requiredXP = GetRequiredXPForNextLevel();
        }
    }
    
    private int GetRequiredXPForNextLevel()
    {
        return Mathf.RoundToInt(xpCurve.Evaluate(level + 1) * 100);
    }
    
    private void RegenerateMana(float amount)
    {
        if (currentMana < maxMana)
        {
            Restore(amount);
        }
    }
}
```

## 5. Skills/Definitions

### SkillDefinition.cs
```csharp
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
        if (stats.GetComponent<PlayerStats>().level < requiredLevel)
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
```

### SkillEffectDefinition.cs
```csharp
using UnityEngine;

public struct SkillContext
{
    public GameObject caster;
    public Transform casterTransform;
    public Vector3 targetPosition;
    public GameObject targetObject;
    public Vector3 direction;
    public int skillLevel;
    public SkillParameters parameters;
    public float deltaTime;
}

public abstract class SkillEffectDefinition : ScriptableObject
{
    [Header("Effect Settings")]
    public float delay = 0f;
    public bool requiresTarget;
    public float effectRadius;
    
    // Lifecycle methods
    public virtual bool CanExecute(SkillContext context)
    {
        return true;
    }
    
    public virtual void OnActivate(SkillContext context)
    {
        Execute(context);
    }
    
    public virtual void OnTick(SkillContext context)
    {
        // For continuous effects
    }
    
    public virtual void OnDeactivate(SkillContext context)
    {
        // Cleanup
    }
    
    protected abstract void Execute(SkillContext context);
}
```

## 6. Skills/Effects

### FireballEffect.cs
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "FireballEffect", menuName = "Skills/Effects/Fireball")]
public class FireballEffect : SkillEffectDefinition
{
    [Header("Projectile")]
    public GameObject projectilePrefab;
    public float baseSpeed = 10f;
    public float baseDamage = 20f;
    public float baseLifetime = 5f;
    public int baseProjectileCount = 1;
    public float spreadAngle = 15f;
    
    [Header("Impact")]
    public float explosionRadius = 0f;
    public GameObject explosionVFX;
    
    protected override void Execute(SkillContext context)
    {
        var spawner = context.caster.GetComponent<ProjectileSpawner>();
        if (spawner == null) return;
        
        var parameters = context.parameters;
        float speed = parameters.Get(SkillParameterKeys.Speed, baseSpeed);
        float damage = parameters.Get(SkillParameterKeys.Damage, baseDamage);
        float lifetime = parameters.Get(SkillParameterKeys.Duration, baseLifetime);
        int count = Mathf.RoundToInt(parameters.Get(SkillParameterKeys.ProjectileCount, baseProjectileCount));
        
        if (count == 1)
        {
            spawner.SpawnProjectile(
                projectilePrefab,
                context.casterTransform.position,
                context.direction,
                speed,
                damage,
                lifetime,
                explosionRadius,
                explosionVFX
            );
        }
        else
        {
            // Multiple projectiles with spread
            float angleStep = spreadAngle / (count - 1);
            float startAngle = -spreadAngle / 2f;
            
            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * context.direction;
                
                spawner.SpawnProjectile(
                    projectilePrefab,
                    context.casterTransform.position,
                    direction,
                    speed,
                    damage,
                    lifetime,
                    explosionRadius,
                    explosionVFX
                );
            }
        }
    }
}
```

### DashEffect.cs
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "DashEffect", menuName = "Skills/Effects/Dash")]
public class DashEffect : SkillEffectDefinition
{
    [Header("Dash Settings")]
    public float baseDistance = 5f;
    public float baseDuration = 0.2f;
    public float baseInvulnerabilityTime = 0.2f;
    public bool passThroughEnemies = true;
    
    [Header("Visual")]
    public GameObject dashTrailPrefab;
    public int afterImageCount = 3;
    
    protected override void Execute(SkillContext context)
    {
        var executor = context.caster.GetComponent<DashExecutor>();
        if (executor == null)
        {
            executor = context.caster.AddComponent<DashExecutor>();
        }
        
        var parameters = context.parameters;
        float distance = parameters.Get(SkillParameterKeys.DashDistance, baseDistance);
        float duration = parameters.Get(SkillParameterKeys.Duration, baseDuration);
        float iFrameTime = parameters.Get(SkillParameterKeys.InvulnerabilityTime, baseInvulnerabilityTime);
        
        executor.PerformDash(
            context.direction,
            distance,
            duration,
            iFrameTime,
            passThroughEnemies,
            dashTrailPrefab,
            afterImageCount
        );
    }
}
```

## 7. Skills/Runtime

### SkillInstance.cs
```csharp
using System;
using UnityEngine;

[Serializable]
public class SkillInstance
{
    public SkillDefinition definition;
    public int currentLevel = 0;
    public KeyCode assignedKey = KeyCode.None;
    public int slotIndex = -1;
    
    private float cooldownRemaining;
    private float globalCooldownRemaining;
    private SkillParameters cachedParameters;
    private bool parametersNeedUpdate = true;
    
    public bool IsUnlocked => currentLevel > 0;
    public bool IsMaxLevel => currentLevel >= definition.maxLevel;
    public bool IsReady => cooldownRemaining <= 0f && globalCooldownRemaining <= 0f;
    public float CooldownProgress => definition.baseCooldown > 0 
        ? 1f - (cooldownRemaining / definition.baseCooldown) 
        : 1f;
    
    public void Update(float deltaTime)
    {
        if (cooldownRemaining > 0)
        {
            cooldownRemaining -= deltaTime;
            if (cooldownRemaining <= 0)
            {
                cooldownRemaining = 0;
                SkillEvents.TriggerSkillReady(this);
            }
            else
            {
                SkillEvents.TriggerCooldownUpdate(this, cooldownRemaining);
            }
        }
        
        if (globalCooldownRemaining > 0)
        {
            globalCooldownRemaining -= deltaTime;
        }
    }
    
    public void StartCooldown(float cooldownModifier = 1f, float globalCooldown = 0.5f)
    {
        var parameters = GetParameters();
        float reduction = parameters.Get(SkillParameterKeys.CooldownReduction, 0f);
        float finalCooldown = definition.baseCooldown * cooldownModifier * (1f - reduction);
        
        cooldownRemaining = Mathf.Max(0.1f, finalCooldown);
        globalCooldownRemaining = globalCooldown;
    }
    
    public SkillParameters GetParameters()
    {
        if (parametersNeedUpdate || cachedParameters == null)
        {
            BuildParameters();
        }
        return cachedParameters;
    }
    
    private void BuildParameters()
    {
        if (cachedParameters == null)
            cachedParameters = new SkillParameters();
        else
            cachedParameters.Clear();
        
        // Apply tier modifications
        var tier = definition.GetTier(currentLevel);
        if (tier != null)
        {
            foreach (var mod in tier.modifications)
            {
                if (mod.overrideBase)
                {
                    cachedParameters.SetBase(mod.parameterKey, mod.overrideValue);
                }
                else
                {
                    cachedParameters.AddModifier(mod.parameterKey, mod.additiveBonus, mod.multiplicativeBonus);
                }
            }
        }
        
        parametersNeedUpdate = false;
    }
    
    public void LevelUp()
    {
        if (!IsMaxLevel)
        {
            currentLevel++;
            parametersNeedUpdate = true;
            SkillEvents.TriggerSkillLevelUp(this);
        }
    }
    
    public void Unlock()
    {
        if (!IsUnlocked)
        {
            currentLevel = 1;
            parametersNeedUpdate = true;
            SkillEvents.TriggerSkillUnlocked(this);
        }
    }
}
```

### SkillInventory.cs
```csharp
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillInventory : MonoBehaviour
{
    [SerializeField] private List<SkillInstance> skills = new();
    [SerializeField] private int maxActiveSkills = 6;
    
    private ISkillPointProvider pointProvider;
    private Dictionary<string, SkillInstance> skillLookup;
    
    void Awake()
    {
        pointProvider = GetComponent<ISkillPointProvider>();
        BuildLookup();
    }
    
    void Update()
    {
        float deltaTime = Time.deltaTime;
        foreach (var skill in skills)
        {
            skill.Update(deltaTime);
        }
    }
    
    public bool TryUnlockSkill(string skillId)
    {
        if (!skillLookup.TryGetValue(skillId, out var skill)) return false;
        if (skill.IsUnlocked) return false;
        
        // Check prerequisites
        if (!ArePrerequisitesMet(skill.definition)) return false;
        
        // Check skill points
        int cost = skill.definition.pointsPerLevel;
        if (!pointProvider.TrySpendPoints(cost)) return false;
        
        skill.Unlock();
        return true;
    }
    
    public bool TryLevelUpSkill(string skillId)
    {
        if (!skillLookup.TryGetValue(skillId, out var skill)) return false;
        if (!skill.IsUnlocked || skill.IsMaxLevel) return false;
        
        int cost = skill.definition.pointsPerLevel;
        if (!pointProvider.TrySpendPoints(cost)) return false;
        
        skill.LevelUp();
        return true;
    }
    
    public bool TryAssignSkillToSlot(string skillId, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxActiveSkills) return false;
        if (!skillLookup.TryGetValue(skillId, out var skill)) return false;
        if (!skill.IsUnlocked) return false;
        
        // Clear previous skill in this slot
        foreach (var s in skills.Where(s => s.slotIndex == slotIndex))
        {
            s.slotIndex = -1;
        }
        
        skill.slotIndex = slotIndex;
        return true;
    }
    
    public SkillInstance GetSkill(string skillId)
    {
        return skillLookup.TryGetValue(skillId, out var skill) ? skill : null;
    }
    
    public IEnumerable<SkillInstance> GetActiveSkills()
    {
        return skills.Where(s => s.IsUnlocked && s.slotIndex >= 0);
    }
    
    public IEnumerable<SkillInstance> GetPassiveSkills()
    {
        return skills.Where(s => s.IsUnlocked && s.definition.type == SkillType.Passive);
    }
    
    private bool ArePrerequisitesMet(SkillDefinition definition)
    {
        foreach (var prereqId in definition.prerequisiteSkillIds)
        {
            if (!skillLookup.TryGetValue(prereqId, out var prereq) || !prereq.IsUnlocked)
                return false;
        }
        return true;
    }
    
    private void BuildLookup()
    {
        skillLookup = new Dictionary<string, SkillInstance>();
        foreach (var skill in skills)
        {
            if (skill.definition != null)
            {
                skillLookup[skill.definition.skillId] = skill;
            }
        }
    }
}
```

### SkillActivator.cs
```csharp
using UnityEngine;

public class SkillActivator : MonoBehaviour, ISkillActivator
{
    private IResourceProvider resourceProvider;
    private StatusEffectManager statusEffectManager;
    private PassiveSkillManager passiveManager;
    
    [SerializeField] private float globalCooldown = 0.5f;
    
    void Awake()
    {
        resourceProvider = GetComponent<IResourceProvider>();
        statusEffectManager = GetComponent<StatusEffectManager>();
        passiveManager = GetComponent<PassiveSkillManager>();
    }
    
    public bool CanActivate(SkillInstance skill, SkillContext context)
    {
        if (!skill.IsUnlocked || !skill.IsReady) return false;
        
        // Check resource cost
        float cost = CalculateCost(skill);
        if (!resourceProvider.CanAfford(cost)) return false;
        
        // Check if stunned/silenced
        if (statusEffectManager != null && statusEffectManager.IsDisabled()) 
            return false;
        
        // Check each effect's CanExecute
        foreach (var effect in skill.definition.baseEffects)
        {
            if (effect != null && !effect.CanExecute(context))
                return false;
        }
        
        return true;
    }
    
    public void Activate(SkillInstance skill, SkillContext context)
    {
        if (!CanActivate(skill, context)) return;
        
        // Consume resources
        float cost = CalculateCost(skill);
        resourceProvider.TryConsume(cost);
        
        // Update context with skill parameters
        context.skillLevel = skill.currentLevel;
        context.parameters = skill.GetParameters();
        
        // Execute base effects
        foreach (var effect in skill.definition.baseEffects)
        {
            if (effect != null)
            {
                effect.OnActivate(context);
            }
        }
        
        // Execute tier effects
        var tier = skill.definition.GetTier(skill.currentLevel);
        if (tier != null && tier.additionalEffects != null)
        {
            foreach (var effect in tier.additionalEffects)
            {
                if (effect != null)
                {
                    effect.OnActivate(context);
                }
            }
        }
        
        // Apply cooldown
        float cdModifier = GetCooldownModifier(skill);
        skill.StartCooldown(cdModifier, globalCooldown);
        
        // Trigger events
        SkillEvents.TriggerSkillActivated(skill, context);
        
        // Play VFX/SFX
        PlayEffects(skill, context);
    }
    
    public float GetCooldownModifier(SkillInstance skill)
    {
        float modifier = 1f;
        
        // Apply global CDR from passives
        if (passiveManager != null)
        {
            modifier *= passiveManager.GetGlobalCooldownModifier();
        }
        
        // Apply status effects
        if (statusEffectManager != null)
        {
            modifier *= statusEffectManager.GetCooldownModifier();
        }
        
        return modifier;
    }
    
    private float CalculateCost(SkillInstance skill)
    {
        var parameters = skill.GetParameters();
        float baseCost = skill.definition.baseManaCost;
        float costReduction = parameters.Get("manaCostReduction", 0f);
        return Mathf.Max(0, baseCost * (1f - costReduction));
    }
    
    private void PlayEffects(SkillInstance skill, SkillContext context)
    {
        if (skill.definition.castingVFX != null)
        {
            Instantiate(skill.definition.castingVFX, context.casterTransform.position, Quaternion.identity);
        }
        
        if (skill.definition.castSound != null)
        {
            AudioSource.PlayClipAtPoint(skill.definition.castSound, context.casterTransform.position);
        }
    }
}
```

### SkillInputHandler.cs
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class SkillInputHandler : MonoBehaviour
{
    private SkillInventory inventory;
    private ISkillActivator activator;
    private Camera mainCamera;
    
    [Header("Input Settings")]
    [SerializeField] private bool useNewInputSystem = false;
    [SerializeField] private KeyCode[] skillHotkeys = new KeyCode[]
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.F, KeyCode.G
    };
    
    void Awake()
    {
        inventory = GetComponent<SkillInventory>();
        activator = GetComponent<ISkillActivator>();
        mainCamera = Camera.main;
    }
    
    void Update()
    {
        if (!useNewInputSystem)
        {
            HandleLegacyInput();
        }
    }
    
    private void HandleLegacyInput()
    {
        for (int i = 0; i < skillHotkeys.Length; i++)
        {
            if (Input.GetKeyDown(skillHotkeys[i]))
            {
                TryActivateSkillInSlot(i);
            }
        }
    }
    
    public void OnSkillInput(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        
        if (int.TryParse(context.control.name, out int slotIndex))
        {
            TryActivateSkillInSlot(slotIndex);
        }
    }
    
    private void TryActivateSkillInSlot(int slotIndex)
    {
        var skills = inventory.GetActiveSkills();
        SkillInstance skillToActivate = null;
        
        foreach (var skill in skills)
        {
            if (skill.slotIndex == slotIndex)
            {
                skillToActivate = skill;
                break;
            }
        }
        
        if (skillToActivate == null) return;
        
        var context = BuildSkillContext(skillToActivate);
        activator.Activate(skillToActivate, context);
    }
    
    private SkillContext BuildSkillContext(SkillInstance skill)
    {
        var context = new SkillContext
        {
            caster = gameObject,
            casterTransform = transform,
            deltaTime = Time.deltaTime
        };
        
        // Determine aim direction
        if (mainCamera != null)
        {
            var mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = transform.position.z;
            context.direction = (mousePos - transform.position).normalized;
            context.targetPosition = mousePos;
        }
        else
        {
            context.direction = transform.forward;
            context.targetPosition = transform.position + transform.forward * 10f;
        }
        
        // Find target if needed
        if (skill.definition.baseEffects.Exists(e => e.requiresTarget))
        {
            context.targetObject = FindNearestTarget(context.targetPosition, 5f);
        }
        
        return context;
    }
    
    private GameObject FindNearestTarget(Vector3 position, float radius)
    {
        var colliders = Physics.OverlapSphere(position, radius);
        GameObject nearest = null;
        float nearestDist = float.MaxValue;
        
        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.GetComponent<IDamageable>().IsAlive) continue;
            
            float dist = Vector3.Distance(position, col.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = col.gameObject;
            }
        }
        
        return nearest;
    }
}
```

## 8. Skills/Executors

### DashExecutor.cs
```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashExecutor : MonoBehaviour
{
    private Rigidbody2D rb2d;
    private Rigidbody rb3d;
    private Collider2D col2d;
    private Collider col3d;
    
    private bool isDashing;
    private bool isInvulnerable;
    private List<GameObject> afterImages = new();
    
    void Awake()
    {
        rb2d = GetComponent<Rigidbody2D>();
        rb3d = GetComponent<Rigidbody>();
        col2d = GetComponent<Collider2D>();
        col3d = GetComponent<Collider>();
    }
    
    public void PerformDash(
        Vector3 direction, 
        float distance, 
        float duration, 
        float invulnerabilityTime,
        bool passThroughEnemies,
        GameObject trailPrefab,
        int afterImageCount)
    {
        if (isDashing) return;
        
        StartCoroutine(DashRoutine(
            direction, 
            distance, 
            duration, 
            invulnerabilityTime, 
            passThroughEnemies,
            trailPrefab,
            afterImageCount
        ));
    }
    
    private IEnumerator DashRoutine(
        Vector3 direction, 
        float distance, 
        float duration, 
        float invulnerabilityTime,
        bool passThroughEnemies,
        GameObject trailPrefab,
        int afterImageCount)
    {
        isDashing = true;
        isInvulnerable = true;
        
        // Store original layer
        int originalLayer = gameObject.layer;
        if (passThroughEnemies)
        {
            gameObject.layer = LayerMask.NameToLayer("Dash");
        }
        
        // Spawn trail
        GameObject trail = null;
        if (trailPrefab != null)
        {
            trail = Instantiate(trailPrefab, transform.position, Quaternion.identity);
            trail.transform.SetParent(transform);
        }
        
        // Calculate dash
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * distance;
        float elapsed = 0f;
        
        // Create after images
        float imageInterval = duration / (afterImageCount + 1);
        float nextImageTime = imageInterval;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Use easing for smooth movement
            float easedT = EaseOutCubic(t);
            
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, easedT);
            
            if (rb2d != null)
            {
                rb2d.MovePosition(newPos);
            }
            else if (rb3d != null)
            {
                rb3d.MovePosition(newPos);
            }
            else
            {
                transform.position = newPos;
            }
            
            // Create after image
            if (elapsed >= nextImageTime && afterImageCount > 0)
            {
                CreateAfterImage();
                nextImageTime += imageInterval;
            }
            
            yield return null;
        }
        
        // Clean up trail
        if (trail != null)
        {
            trail.transform.SetParent(null);
            var ps = trail.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop();
                Destroy(trail, ps.main.duration);
            }
            else
            {
                Destroy(trail, 1f);
            }
        }
        
        // Restore layer
        gameObject.layer = originalLayer;
        isDashing = false;
        
        // Handle invulnerability
        if (invulnerabilityTime > duration)
        {
            yield return new WaitForSeconds(invulnerabilityTime - duration);
        }
        
        isInvulnerable = false;
    }
    
    private void CreateAfterImage()
    {
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) return;
        
        GameObject afterImage = new GameObject("AfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;
        
        var imageRenderer = afterImage.AddComponent<SpriteRenderer>();
        imageRenderer.sprite = spriteRenderer.sprite;
        imageRenderer.color = new Color(1f, 1f, 1f, 0.5f);
        
        afterImages.Add(afterImage);
        StartCoroutine(FadeAfterImage(imageRenderer));
    }
    
    private IEnumerator FadeAfterImage(SpriteRenderer renderer)
    {
        float fadeTime = 0.5f;
        float elapsed = 0f;
        Color startColor = renderer.color;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / fadeTime);
            renderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        Destroy(renderer.gameObject);
    }
    
    private float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }
    
    public bool IsDashing => isDashing;
    public bool IsInvulnerable => isInvulnerable;
}
```

## 9. Combat

### ProjectilePool.cs
```csharp
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePool : MonoBehaviour
{
    private static ProjectilePool instance;
    public static ProjectilePool Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("ProjectilePool");
                instance = go.AddComponent<ProjectilePool>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    private Dictionary<GameObject, Queue<GameObject>> pools = new();
    private Dictionary<GameObject, int> poolSizes = new();
    
    [SerializeField] private int defaultPoolSize = 20;
    [SerializeField] private bool autoExpand = true;
    
    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    public void PrewarmPool(GameObject prefab, int size)
    {
        if (pools.ContainsKey(prefab)) return;
        
        CreatePool(prefab, size);
    }
    
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(prefab))
        {
            CreatePool(prefab, defaultPoolSize);
        }
        
        var pool = pools[prefab];
        GameObject obj;
        
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
        }
        else if (autoExpand)
        {
            obj = CreatePooledObject(prefab);
        }
        else
        {
            Debug.LogWarning($"Pool for {prefab.name} is empty and auto-expand is disabled!");
            return null;
        }
        
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        
        return obj;
    }
    
    public void Return(GameObject obj, GameObject prefab)
    {
        if (!pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"No pool exists for {prefab.name}");
            Destroy(obj);
            return;
        }
        
        obj.SetActive(false);
        obj.transform.SetParent(transform);
        pools[prefab].Enqueue(obj);
    }
    
    private void CreatePool(GameObject prefab, int size)
    {
        var pool = new Queue<GameObject>();
        pools[prefab] = pool;
        poolSizes[prefab] = size;
        
        for (int i = 0; i < size; i++)
        {
            var obj = CreatePooledObject(prefab);
            obj.SetActive(false);
            pool.Enqueue(obj);
        }
    }
    
    private GameObject CreatePooledObject(GameObject prefab)
    {
        var obj = Instantiate(prefab);
        obj.transform.SetParent(transform);
        
        // Add return component
        var poolable = obj.AddComponent<PoolableObject>();
        poolable.prefab = prefab;
        
        return obj;
    }
}

public class PoolableObject : MonoBehaviour
{
    public GameObject prefab;
    
    public void ReturnToPool()
    {
        ProjectilePool.Instance.Return(gameObject, prefab);
    }
}
```

### Projectile.cs
```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    private Rigidbody2D rb;
    private PoolableObject poolable;
    private GameObject owner;
    private float damage;
    private float lifetime;
    private float explosionRadius;
    private GameObject explosionVFX;
    private DamageType damageType;
    
    private float aliveTime;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        poolable = GetComponent<PoolableObject>();
    }
    
    public void Initialize(
        GameObject owner,
        Vector3 direction,
        float speed,
        float damage,
        float lifetime,
        float explosionRadius = 0f,
        GameObject explosionVFX = null,
        DamageType damageType = DamageType.Magical)
    {
        this.owner = owner;
        this.damage = damage;
        this.lifetime = lifetime;
        this.explosionRadius = explosionRadius;
        this.explosionVFX = explosionVFX;
        this.damageType = damageType;
        
        aliveTime = 0f;
        rb.velocity = direction.normalized * speed;
        
        // Rotate to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    
    void Update()
    {
        aliveTime += Time.deltaTime;
        if (aliveTime >= lifetime)
        {
            ReturnToPool();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject == owner) return;
        
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null && damageable.IsAlive)
        {
            if (explosionRadius > 0)
            {
                ApplyAreaDamage(other.transform.position);
            }
            else
            {
                damageable.TakeDamage(damage, damageType);
            }
            
            if (explosionVFX != null)
            {
                Instantiate(explosionVFX, transform.position, Quaternion.identity);
            }
            
            ReturnToPool();
        }
    }
    
    private void ApplyAreaDamage(Vector3 center)
    {
        var colliders = Physics2D.OverlapCircleAll(center, explosionRadius);
        foreach (var col in colliders)
        {
            if (col.gameObject == owner) continue;
            
            var damageable = col.GetComponent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                float distance = Vector2.Distance(center, col.transform.position);
                float falloff = 1f - (distance / explosionRadius);
                float finalDamage = damage * Mathf.Max(0.5f, falloff);
                
                damageable.TakeDamage(finalDamage, damageType);
            }
        }
    }
    
    private void ReturnToPool()
    {
        if (poolable != null)
        {
            poolable.ReturnToPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
```

## 使用例

### シーン設定
```csharp
// PlayerのGameObjectに以下をアタッチ:
// - PlayerStats
// - SkillInventory
// - SkillActivator
// - SkillInputHandler
// - PassiveSkillManager
// - StatusEffectManager
// - DashExecutor
// - ProjectileSpawner

// SkillDefinitionアセットを作成
// 1. Fireball (Active)
// 2. Dash (Active)
// 3. Attack Boost (Passive)
// 4. Lightning Chain (Active)

// SkillInventoryに登録
// UIからスキル解放/レベルアップ
```

### カスタムエフェクト追加例
```csharp
[CreateAssetMenu(fileName = "HealEffect", menuName = "Skills/Effects/Heal")]
public class HealEffect : SkillEffectDefinition
{
    public float baseHealAmount = 50f;
    public bool healOverTime = false;
    public float tickInterval = 1f;
    public int tickCount = 5;
    
    protected override void Execute(SkillContext context)
    {
        var health = context.caster.GetComponent<IDamageable>();
        if (health == null) return;
        
        float healAmount = context.parameters.Get("healAmount", baseHealAmount);
        
        if (healOverTime)
        {
            var hot = context.caster.AddComponent<HealOverTime>();
            hot.Initialize(healAmount / tickCount, tickInterval, tickCount);
        }
        else
        {
            health.Heal(healAmount);
        }
    }
}
```

このシステムの特徴:
- 完全な責務分離
- 型安全性の向上
- イベント駆動アーキテクチャ
- パフォーマンス最適化（オブジェクトプール）
- 拡張性（新しいエフェクトやスキルタイプの追加が容易）
- パッシブ/トリガー/トグル型のサポート
- コンボシステム対応
- ステータスエフェクト連携
