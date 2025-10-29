using System;
using UnityEngine;

public class PlayerStats : MonoBehaviour, IResourceProvider, ISkillPointProvider
{
    [Header("Level & Experience")]
    [SerializeField] private int level = 1;
    [SerializeField] private int currentXP = 0;
    [SerializeField] private AnimationCurve xpCurve;
    public int Level => level;

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