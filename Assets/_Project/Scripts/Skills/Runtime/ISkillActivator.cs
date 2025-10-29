public interface ISkillActivator
{
    bool CanActivate(SkillInstance skill, SkillContext context);
    void Activate(SkillInstance skill, SkillContext context);
    float GetCooldownModifier(SkillInstance skill);
}