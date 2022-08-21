using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lightning : CommonAttack
{
    public override bool SkillAttack(double linkRate)
    {
        List<Monster> targets = FindTarget();
        if (skillData.skillTarget == 3) //자기 주변 광역
        {
            if (targets.Count == 0)
                return false;
            CollideAttack(null, null);
        }
        else
        {
            Monster centerTarget = targets.Count > 0 ? targets[0] : null;

            bool isTarget = false;
            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            Transform ownerSlotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            Vector3 startPos = ownerSlotTrans != null ? ownerSlotTrans.position : owner.transform.position;

            for (int i = 0; i < skillData.skillRangeOpt; i++)
            {
                Monster target = targets.Count > i ? targets[i] : null;
                Vector3 pos = Vector3.zero;
                if (target != null)
                {
                    Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
                    pos = slotTrans != null ? slotTrans.position : target.transform.position;

                    if (projectilePool != null)
                    {
                        BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                        {
                            Projectile projectile = projectilePool.Pop();
                            projectile.Attack(owner, startPos, pos);
                            curProjectiles.Add((Projectile)projectile);
                        });
                    }
                    else
                    {
                        CollideAttack(null, target);
                    }
                    if (string.IsNullOrEmpty(skillTable.CastEffect) == false)
                    {
                        EffectManager.Inst.ShowLineEffect(skillTable.CastEffect, owner.transform.localScale.x > 0 ? true : false, startPos, startPos, slotTrans.position);
                    }
                    isTarget = true;
                }
            }
            if (isTarget == false)
                return false;
        }

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        UseMP();
        // 퀘스트 업데이트
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
}
