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
    public bool CanCast(GameObject caster)
    {
        Debug.Log($"[SkillInstance] CanCast 檢查，IsUnlocked:{IsUnlocked}, IsReady:{IsReady}");
        if (!IsUnlocked) return false;
        if (!IsReady) return false;

        var resourceProvider = caster.GetComponent<IResourceProvider>();
        if (resourceProvider == null) return false;

        float manaCost = GetManaCost();
        return resourceProvider.CanAfford(manaCost);
    }

    public float GetManaCost()
    {
        var parameters = GetParameters();
        float costReduction = parameters.Get(SkillParameterKeys.ManaCostReduction, 0f);
        return definition.baseManaCost * (1f - costReduction);
    }

    public bool TryCast(GameObject caster, float cooldownModifier = 1f, float globalCooldown = 0.5f)
    {
        Debug.Log($"[SkillInstance] TryCast 被調用，技能: {definition?.skillId}"); 
        if (!CanCast(caster)) return false;

        var resourceProvider = caster.GetComponent<IResourceProvider>();
        float manaCost = GetManaCost();

        // 消耗魔力
        if (!resourceProvider.TryConsume(manaCost)) return false;

        // 啟動冷卻
        StartCooldown(cooldownModifier, globalCooldown);

        // 使用現有的 OnSkillActivated 事件
        var context = new SkillContext { caster = caster };
        SkillEvents.TriggerSkillActivated(this, context);

        return true;
    }
}