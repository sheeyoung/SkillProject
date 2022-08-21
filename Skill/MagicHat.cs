using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicHat : Skill
{
    List<Monster> changeTarget = new List<Monster>();
    Coroutine EndRoutine;
    public override void ResetSkill()
    {
        base.ResetSkill();
        if (EndRoutine != null)
        {
            BattleScene.Inst.BattleMode.EndSkillCorutine(EndRoutine);
            EndRoutine = null;
        }
    }
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;

        List<Monster> findTarget = FindTarget();
        if (findTarget.Count == 0)
            return false;
        List<Monster> targets = GetDamageTarget(findTarget[0]);

        if (targets.Count == 0)
            return false;

        changeTarget.Clear();
        //Å¸°Ù º¯ÇÔ
        for (int i = 0; i < targets.Count; i++)
        {
            
            CalculateDamage(targets[i], skillData.skillDamageRate, false);
            if (targets[i].ActionCtrl.State == CHAR_ACTION_STATE.DIE)
                continue;
            
            ((MonsterAnimController)(targets[i].AnimCtrl)).ChangeRabbit(true);
            changeTarget.Add(targets[i]);
            if (targets[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, targets[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
            }
            targets[i].OnMonsterDie += MonDieEvent;
        }
        if (skillTable.TargetEffectType == 2)
        {
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, targets[0].transform.position, owner.transform.localScale.x > 0 ? true : false);
        }

        EndRoutine = BattleScene.Inst.BattleMode.RequestMagicHatSkill((float)skillData.skillTypeOpt, OnAttack);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        ShowCastEffect();
        UseMP();
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    private void OnAttack()
    {
        EndRoutine = null;
        for (int i = 0; i < changeTarget.Count; i++)
        {
            ((MonsterAnimController)(changeTarget[i].AnimCtrl)).ChangeRabbit(false);
            if (changeTarget[i].ActionCtrl.State == CHAR_ACTION_STATE.DIE)
                continue;
            OnDebuff(changeTarget[i], 0, 0);
        }
    }
    private void MonDieEvent(Monster mon)
    {
        if (changeTarget.Contains(mon))
            changeTarget.Remove(mon);
    }
}
