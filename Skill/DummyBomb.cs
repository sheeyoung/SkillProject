using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyBomb : Skill
{
    private GameObject dummyObject;
    private Coroutine skillCorutine;
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        if (dummyObject == null)
        {
            dummyObject = EffectManager.Inst.GetEffectObject(skillTable.CastEffect);
        }
        dummyObject.SetActive(true);

        var dummyPos = owner.holePrevPos;
        dummyObject.transform.position = dummyPos;

        List<Monster> monsterList = new List<Monster>();
        if (owner.holePrevTargets != null && owner.holePrevTargets.Count > 0)
        {
            for (int i = 0; i < owner.holePrevTargets.Count; i++)
            {
                if (owner.holePrevTargets[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE)
                {
                    monsterList.Add(owner.holePrevTargets[i]);
                }
            }
        }
        SetForceTarget(monsterList, (float)skillData.skillTypeOpt);

        skillCorutine = BattleScene.Inst.BattleMode.RequestDummyBombSkill((float)skillData.skillTypeOpt, (time) => { SetForceTarget(null, time); }, EndSkill);

        return true;
    }
    private void SetForceTarget(List<Monster> monsterList = null, float time = 0f)
    {
        var rangeMons = NowMode.FindMonsterListByMonsterPosCircleRange(dummyObject.transform.position, 0, skillData.skillRangeOpt);
        if (rangeMons == null)
            rangeMons = new List<Monster>();
        if (monsterList != null)
        {
            for (int i = 0; i < monsterList.Count; i++)
            {
                if (rangeMons.Contains(monsterList[i]) == false)
                    rangeMons.Add(monsterList[i]);
            }
        }
        for (int i = 0; i < rangeMons.Count; ++i)
        {
            var mon = rangeMons[i];
            if ((mon.MonGrade == MONSTER_GRADE.BOSS || mon.MonGrade == MONSTER_GRADE.STAGE_BOSS))
            {
                int rmd = User.Inst.LocalRandom();
                float rate = 0;
                switch (BattleScene.Inst.BattleType)
                {
                    case BATTLE_TYPE.INFINITE_BATTLE:
                    case BATTLE_TYPE.REGION_BOSS_BATTLE:
                        rate = User.Inst.TBL.Const.CONST_BOSS_RAGE_RESIST_KNOCKBACK * 10000 * mon.BerserkStep;
                        break;
                    case BATTLE_TYPE.HELL_CONQUEST_BATTLE:
                        rate = User.Inst.TBL.Const.CONST_BOSS_HELL_CONQUEST_KNOCKBACK * 10000;
                        break;
                    case BATTLE_TYPE.SWEEP_BATTLE:
                        rate = User.Inst.TBL.Const.CONST_BOSS_SWEEP_KNOCKBACK * 10000;
                        break;
                    case BATTLE_TYPE.RAID_BATTLE:
                        rate = User.Inst.TBL.Const.CONST_BOSS_RAID_KNOCKBACK * 10000;
                        break;
                    case BATTLE_TYPE.AROUSAL_TEST:
                        rate = User.Inst.TBL.Const.CONST_BOSS_AROSAL_KNOCKBACK * 10000;
                        break;
                }
                if (rmd < rate)
                    continue;
            }
            mon.SetForceTarget(true, skillId, dummyObject.transform.position, time);
        }

    }

    private void EndSkill()
    {
        if (dummyObject) dummyObject.SetActive(false);
        skillCorutine = null;

        var targets = NowMode.FindMonsterListByMonsterPosCircleRange(dummyObject.transform.position, 0, skillData.skillRangeOpt);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;
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
        if (skillTable.TargetEffectType == 2)
        {
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, dummyObject.transform.position, false);
        }
    }

    public override void ClearSkill()
    {
        base.ClearSkill();
        if (skillCorutine != null)
            BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutine);
        if (dummyObject) dummyObject.SetActive(false);
        skillCorutine = null;
    }
}
