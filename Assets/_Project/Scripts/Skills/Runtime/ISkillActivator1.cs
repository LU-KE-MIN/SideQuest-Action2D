// FILE: Assets/_Project/Scripts/Skills/Runtime/ISkillActivator.cs
namespace Game.Skills
{
    public interface ISkillActivator
    {
        void Activate(SkillInstance skill, SkillContext context);
    }
}