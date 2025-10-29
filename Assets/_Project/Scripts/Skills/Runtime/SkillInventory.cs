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