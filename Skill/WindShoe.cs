using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindShoe : Skill
{
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        List<Monster> targets = FindTarget();

        string castPosName = "main";
        if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
            castPosName = skillTable.CastEffectPosition;
        Transform ownerSlotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
        Vector3 startPos = ownerSlotTrans != null ? ownerSlotTrans.position : owner.transform.position;

        if (targets.Count == 0)
            return false;
        CollideAttack(null, null);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        UseMP();

        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    public override void CollideAttack(Projectile projectile, Monster target)
    {
        List<Monster> targets = GetDamageTarget(target);
        var mon = targets.Count > 0 ? targets[0] : null;

        for (int i = 0; i < targets.Count; i++)
        {
            OnDebuff(targets[i], 0, 0);

            if (targets[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, targets[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
            }
        }

        if (mon != null)
        {
            ShowCastEffect();
            if (string.IsNullOrEmpty(skillTable.TargetEffect) == false &&  skillTable.TargetEffectType == 2)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, mon.transform.position, owner.transform.localScale.x > 0 ? true : false);
            }
        }
    }
}
