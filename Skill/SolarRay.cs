using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarRay : Skill
{
    protected GameObject solarRayEff;
    protected SkillObject solarRaySkillObject;
    public float remainTime = 0f;
    private Monster curTarget;
    private Transform slotTrans;
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;

        var targets = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0, 30);
        List<Monster> findTargets = ((PCActionController)owner.ActionCtrl).FindTargetByDist(targets, true, false);

        if (findTargets.Count > 0)
            curTarget = findTargets[0];
        else
            curTarget = null;

        if (curTarget == null)
            return false;


        string castPosName = "main";
        slotTrans = UtilFunction.FindTransform(castPosName, curTarget.transform);

        if (solarRayEff == null)
        {
            Vector3 pos = slotTrans.position;

            solarRayEff = EffectManager.Inst.GetEffectObject(skillTable.TargetEffect, pos, owner.transform.localScale.x > 0 ? true : false);

            if (solarRayEff.GetComponent<SkillObject>() == null)
                solarRaySkillObject = solarRayEff.AddComponent<SkillObject>();
            else
                solarRaySkillObject = solarRayEff.GetComponent<SkillObject>();
        }

        remainTime = (float)skillData.skillTypeOpt;
        tickTime = 0f;
        solarRayEff.SetActive(true);

        Vector3 temp = slotTrans.position;
        temp.z = 0f;
        solarRayEff.transform.position = temp;

        //BattleScene.Inst.BattleMode.RequestSolarRaySkill(UpdateSolarRayMove);
        solarRaySkillObject.Init(UpdateSolarRayMove);

        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);
        return true;
    }

    
    float tickTime = 0;
    public void UpdateSolarRayMove(float deltaTime)
    {
        if (remainTime > 0f)
        {
            if (curTarget.ActionCtrl.State == CHAR_ACTION_STATE.DIE)
            {
                var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(solarRayEff.transform.position, 0, 30);
                var findMonsterlist = ((PCActionController)owner.ActionCtrl).FindTargetByDist(monsterlist, true, false);

                if (findMonsterlist == null || findMonsterlist.Count == 0)
                {
                    //if(notFoundTarget > 0)
                    //{
                    //    AttackEnd();
                    //    return false;
                    //}
                    //notFoundTarget++;
                    return;
                }

                curTarget = findMonsterlist[0];
                string castPosName = "main";
                slotTrans = UtilFunction.FindTransform(castPosName, curTarget.transform);
            }
            remainTime -= deltaTime;
            Vector3 temp = slotTrans.position;
            temp.z = -12f;
            solarRayEff.transform.position = Vector3.MoveTowards(solarRayEff.transform.position, temp, User.Inst.TBL.Const.CONST_SOLARRAY_MOVESPEED * deltaTime);

            if (remainTime <= 0f)
            {
                AttackEnd();
                return;
            }
            tickTime += deltaTime;
            if(tickTime >= User.Inst.TBL.Const.CONST_SOLARRAY_TIC)
            {
                tickTime = 0;
                CollideAttack(null, curTarget);
            }

        }
    }
    public override void CollideAttack(Projectile projectile, Monster target)
    {
        var targets = NowMode.FindMonsterListByMonsterPosCircleRange(solarRayEff.transform.position, 0, skillData.skillRangeOpt);
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
            CalculateDamage(targets[i], skillData.skillDamageRate);
            if (targets[i] != null && (targets[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE))
            {
                targets[i].ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
            }
        }

        if (mon != null)
        {
            ExplosionDamage(mon.transform.position);
        }
    }

    public void AttackEnd()
    {
        remainTime = 0;
        EffectManager.Inst.ShowEffect("eff_PC_skill_solarray_t1", solarRayEff.transform.position, false);
        solarRayEff.SetActive(false);
        solarRaySkillObject.EndEff();
    }
}
