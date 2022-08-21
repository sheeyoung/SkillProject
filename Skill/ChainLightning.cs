using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainLightning : Skill
{
    int additionalProjectileCount;
    float constChainlightningDistance;

    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);

        constChainlightningDistance = User.Inst.TBL.Const.CONST_CHAINLIGHTNING_DISTANCE;
    }
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;

        additionalProjectileCount = (int)skillData.skillTypeOpt;
        List<Monster> targets = FindTarget();

        bool isTarget = false; 
        Transform slot = UtilFunction.FindTransform("eff_weapon", owner.transform);

        for (int i = 0; i < skillData.skillRangeOpt; i++)
        {
            Monster target = targets.Count > i ? targets[i] : null;
            Vector3 pos = Vector3.zero;
            if (target != null)
            {
                Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
                isTarget = true;
                if (string.IsNullOrEmpty(skillTable.CastEffect) == false)
                {
                    EffectManager.Inst.ShowLineEffect(skillTable.CastEffect, owner.transform.localScale.x > 0 ? true : false,
                        slot.position,
                        slot.position, 
                        slotTrans.position);
                    CollideAttack(null, target);
                }
            }
        }
        if (isTarget == false)
            return false;

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        UseMP();
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }

    private bool AdditionalAttackProjectile(Monster attackMon)
    {
        if (attackMon == null)
            return false;
        Vector2 moveDir = new Vector2(owner.transform.localScale.x * -1f, 0);
        if (BattleScene.Inst.BattleMode.IsAutoBattle == false)
            moveDir = owner.MoveVec;

        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(attackMon.transform.position, 1, constChainlightningDistance);
        List<Monster> targets = ((PCActionController)owner.ActionCtrl).FindTargetByDist(monsterlist, true, false);
        if (targets.Count > 0)
            targets = ((PCActionController)owner.ActionCtrl).FindTargetByDist(targets, false, false);

        Monster centerTarget = null;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == attackMon)
                continue;
            centerTarget = targets[i];
            break;
        }

        bool isTarget = false;
        Transform slot = UtilFunction.FindTransform("main", attackMon.transform);
        if (centerTarget != null)
        {
            if (string.IsNullOrEmpty(skillTable.CastEffect) == false)
            {
                Transform slotTrans = UtilFunction.FindTransform("main", centerTarget.transform);
                EffectManager.Inst.ShowLineEffect(skillTable.CastEffect, owner.transform.localScale.x > 0 ? true : false,
                    slot.position,
                    slot.position,
                    slotTrans.position);
                CollideAttack(null, centerTarget);
            }
            isTarget = true;
        }
        if (isTarget == false)
            return false;
        return true;
    }
    public override void CollideAttack(Projectile projectile, Monster target)
    {
        if (target != null)
        {
            if (owner.Type == CHARACTER_TYPE.AVATAR)
            {
                if (BattleScene.Inst.BattleMode.Pc.RageMgr != null)
                    BattleScene.Inst.BattleMode.Pc.RageMgr.UpdateRageGauge(User.Inst.TBL.Const.CONST_RAGE_VALUE_ATK);
            }
            else
            {
                if (owner.RageMgr != null)
                    owner.RageMgr.UpdateRageGauge(User.Inst.TBL.Const.CONST_RAGE_VALUE_ATK);
            }

            CalculateDamage(target, skillData.skillDamageRate);
            if (target.ActionCtrl.State != CHAR_ACTION_STATE.DIE)
            {
                target.ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
            }
            Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, slotTrans.position, owner.transform.localScale.x > 0 ? true : false);
            //����ȭ
            ExplosionDamage(target.transform.position);
        }
        additionalProjectileCount--;
        if (additionalProjectileCount > 0)
        {
            AdditionalAttackProjectile(target);
        }
    }
    
    
}
