using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExecutionHpAttack : CommonAttack
{
    int skillAttackCount;
    float effDuration;
    GameObject startEff;
    private List<Coroutine> skillCorutines = new List<Coroutine>();
    public bool SkillAttack(int attackCount)
    {
        skillAttackCount = attackCount;
        effDuration = EffectManager.Inst.GetEffectDurationTime("eff_PC_skill03_t2");
        skillCorutines.Clear();
        List<Monster> targets = FindTarget();
        bool isTarget = false;
        for (int i = 0; i < skillData.skillRangeOpt; i++)
        {
            Monster target = targets.Count > i ? targets[i] : null;
            if (target != null)
            {
                Transform slotTrans = UtilFunction.FindTransform("root", target.transform);
                Vector3 pos = slotTrans != null ? slotTrans.position : target.transform.position;

                startEff = EffectManager.Inst.GetEffectObject("eff_PC_skill03_t1", pos, false);
                var corutine = BattleScene.Inst.BattleMode.RequestExecutionAttack(target, 1, effDuration, AttackLoop);
                skillCorutines.Add(corutine);
                isTarget = true;
            }
        }
        if (isTarget == false)
            return false;

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        ShowCastEffect();
        UseMP();

        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    public void AttackLoop(Monster mon, int count)
    {
        if (mon == null)
            return;
        if (count >= skillAttackCount)
        {
            Transform slotTrans1 = UtilFunction.FindTransform("root", mon.transform);
            Vector3 pos1 = slotTrans1 != null ? slotTrans1.position : mon.transform.position;

            EffectManager.Inst.ShowEffect("eff_PC_skill03_t1_1", pos1, false);
            EffectManager.Inst.ReleaseEffect("eff_PC_skill03_t1", startEff);
            return;
        }
        Transform slotTrans = UtilFunction.FindTransform("root", mon.transform);
        Vector3 pos = slotTrans != null ? slotTrans.position : mon.transform.position;
        EffectManager.Inst.ShowEffect("eff_PC_skill03_t2", pos, false);

        CollideAttack(null, mon);
        if(mon.ActionCtrl.State == CHAR_ACTION_STATE.DIE)
        {
            Transform slotTrans1 = UtilFunction.FindTransform("root", mon.transform);
            Vector3 pos1 = slotTrans1 != null ? slotTrans1.position : mon.transform.position;

            EffectManager.Inst.ShowEffect("eff_PC_skill03_t1_1", pos1, false);
            EffectManager.Inst.ReleaseEffect("eff_PC_skill03_t1", startEff);
            return;
        }
        var corutine = BattleScene.Inst.BattleMode.RequestExecutionAttack(mon, count+1, effDuration, AttackLoop);
        skillCorutines.Add(corutine);
    }

    public override void CollideAttack(Projectile projectile, Monster target)
    {
        if (projectile != null && projectileInfo.IsPierce == 0)
        {
            if (curProjectiles.Contains(projectile))
                curProjectiles.Remove(projectile);
            ReleaseProjectile(projectile);
        }

        List<Monster> targets = GetDamageTarget(target);
        var mon = targets.Count > 0 ? targets[0] : null;

        if (targets.Count > 0)
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
        }

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;
            var rateB = targets[i].HP / targets[i].MaxHP;
            rateB *= 100;
            float rate = (float)rateB / 100f;
            if (rate < 0.01)
                rate = 0.01f;

            if(rate <= User.Inst.TBL.Const.CONST_EXECUTION_HP_RATE)
            {
                double hpRateAtk = (User.Inst.TBL.Const.CONST_EXECUTION_HP_RATE / rate) * skillData.skillTypeOpt;
                hpRateAtk *= skillData.skillDamageRate;
                CalculateDamage(targets[i], hpRateAtk);
            }
            else 
                CalculateDamage(targets[i], skillData.skillDamageRate);
            if (targets[i] != null && (targets[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE))
            {
                targets[i].ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
            }

            if (targets[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, targets[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
            }
        }

        if (mon != null)
        {
            if (skillTable.TargetEffectType == 2)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, mon.transform.position, owner.transform.localScale.x > 0 ? true : false);
            }

            ExplosionDamage(mon.transform.position);
        }
    }


    public bool SkillAttackTest(int attackCount)
    {
        skillCorutines.Clear();
        skillAttackCount = attackCount;
        effDuration = EffectManager.Inst.GetEffectDurationTime("eff_PC_skill03_t2");
        List<Monster> targets = FindTarget();
        bool isTarget = false;
        for (int i = 0; i < skillData.skillRangeOpt; i++)
        {
            Monster target = targets.Count > i ? targets[i] : null;
            if (target != null)
            {
                Transform slotTrans = UtilFunction.FindTransform("root", target.transform);
                Vector3 pos = slotTrans != null ? slotTrans.position : target.transform.position;

                startEff = EffectManager.Inst.GetEffectObject("eff_PC_skill03_t1", pos, false);
                var corutine = BattleScene.Inst.BattleMode.RequestExecutionAttack(target, 1, effDuration, AttackLoopTest);
                skillCorutines.Add(corutine);
                isTarget = true;
            }
        }
        if (isTarget == false)
            return false;

        ShowCastEffect();
        UseMP();

        return true;
    }
    public void AttackLoopTest(Monster mon, int count)
    {
        if (count >= skillAttackCount)
        {
            Transform slotTrans1 = UtilFunction.FindTransform("root", mon.transform);
            Vector3 pos1 = slotTrans1 != null ? slotTrans1.position : mon.transform.position;

            EffectManager.Inst.ShowEffect("eff_PC_skill03_t1_1", pos1, false);
            EffectManager.Inst.ReleaseEffect("eff_PC_skill03_t1", startEff);
            return;
        }
        Transform slotTrans = UtilFunction.FindTransform("root", mon.transform);
        Vector3 pos = slotTrans != null ? slotTrans.position : mon.transform.position;
        EffectManager.Inst.ShowEffect("eff_PC_skill03_t2", pos, false);

        CollideAttackTest(null, mon);
        if (mon.ActionCtrl.State == CHAR_ACTION_STATE.DIE)
        {
            Transform slotTrans1 = UtilFunction.FindTransform("root", mon.transform);
            Vector3 pos1 = slotTrans1 != null ? slotTrans1.position : mon.transform.position;

            EffectManager.Inst.ShowEffect("eff_PC_skill03_t1_1", pos1, false);
            EffectManager.Inst.ReleaseEffect("eff_PC_skill03_t1", startEff);
            return;
        }
        var corutine = BattleScene.Inst.BattleMode.RequestExecutionAttack(mon, count + 1, effDuration, AttackLoopTest);
        skillCorutines.Add(corutine);
    }

    public void CollideAttackTest(Projectile projectile, Monster target)
    {
        if (projectile != null && projectileInfo.IsPierce == 0)
        {
            if (curProjectiles.Contains(projectile))
                curProjectiles.Remove(projectile);
            ReleaseProjectile(projectile);
        }

        List<Monster> targets = GetDamageTarget(target);
        var mon = targets.Count > 0 ? targets[0] : null;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;
            var rateB = targets[i].HP / targets[i].MaxHP;
            rateB *= 100;
            float rate = (float)rateB / 100f;
            if (rate < 0.01)
                rate = 0.01f;

            if (rate <= User.Inst.TBL.Const.CONST_EXECUTION_HP_RATE)
            {
                double hpRateAtk = (User.Inst.TBL.Const.CONST_EXECUTION_HP_RATE / rate) * skillData.skillTypeOpt;
                hpRateAtk *= skillData.skillDamageRate;
                CalculateDamageTest(targets[i], hpRateAtk);
            }
            else
                CalculateDamageTest(targets[i], skillData.skillDamageRate);
            if (targets[i] != null && (targets[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE))
            {
                targets[i].ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
            }

            if (targets[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, targets[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
            }
        }

        if (mon != null)
        {
            if (skillTable.TargetEffectType == 2)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, mon.transform.position, owner.transform.localScale.x > 0 ? true : false);
            }

            //����ȭ
            ExplosionDamage(mon.transform.position);
        }
    }
    protected void CalculateDamageTest(Monster monster, double damageRate)
    {
        UI.DAMAGE_TYPE damageType = UI.DAMAGE_TYPE.None;
        monster.OnDamaged(owner, damageType, 1);
    }

    public override void ClearSkill()
    {
        base.ClearSkill();
        for(int i = 0; i < skillCorutines.Count; i++)
        {
            BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutines[i]);
        }
        skillCorutines.Clear();
    }

}
