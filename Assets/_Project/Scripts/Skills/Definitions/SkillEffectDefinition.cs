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