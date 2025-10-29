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