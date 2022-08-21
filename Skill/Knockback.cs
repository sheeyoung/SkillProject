using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knockback : Skill
{
    private Coroutine skillCorutine;
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        skillCorutine = null;

        List<Monster> targets = new List<Monster>();
        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0, skillData.skillRange);

        for (int i = 0; i < monsterlist.Count; i++)
        {
            var mon = monsterlist[i];
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
            mon.ActionCtrl.State = CHAR_ACTION_STATE.KNOCKBACK;
        }

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        float speed = (float)skillData.skillTypeOpt / User.Inst.TBL.Const.CONST_KNOCKBACK_TIME;
        ShowCastEffect();
        skillCorutine = BattleScene.Inst.BattleMode.RequestKnockback(monsterlist, speed, (float)skillData.skillTypeOpt, EndAction);
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    public void EndAction(Monster mon)
    {
        if (mon == null)
            return;
        int debuffIndex = skillData.GetDebuffPassiveInfo((int)DEBUFF.DEBUFF_STUN);
        if (debuffIndex == -1)
            return;
        bool isStun = User.Inst.LocalRandom() <= skillData.debuffPassives[debuffIndex].specialOpt * 10000 ? true : false;
        if (isStun)
            mon.DebuffMgr.AddDebuff(owner, 2, 0d);
        if (mon != null)
        {
            if (skillTable.TargetEffectType == 2)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, mon.transform.position, owner.transform.localScale.x > 0 ? true : false);
            }
        }
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        if (skillCorutine != null)
            BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutine);
    }
}
