using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class LightningHole : Skill
{
    Coroutine skillCorutine;

    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);
    }

    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        var targets = FindTarget();
        Monster target = targets.Count > 0 ? targets[0] : null;
        if (target == null)
            return false;

        Vector3 endPos = Vector3.zero;
        endPos = new Vector2(target.transform.position.x, target.transform.position.y) - new Vector2(owner.transform.position.x, owner.transform.position.y);
        endPos.Normalize();
        decimal skillRange = (decimal)skillData.skillRange;
        float fSkillRange = (float)skillRange;
        endPos = endPos * fSkillRange;
        endPos = new Vector3(endPos.x, endPos.y, 0f);
        endPos += owner.transform.position;

        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(endPos, 0, skillData.skillRangeOpt);
        List<Monster> mons = new List<Monster>();
        float skillLevelPow = skillData.GetSpecialType(8);
        skillLevelPow = (1 + skillLevelPow);
        float targetLv = BattleScene.Inst.BattleMode.MonSpawnMgr.MobLevel;
        for (int i = 0; i < monsterlist.Count; ++i)
        {
            if (monsterlist[i].MonGrade == MONSTER_GRADE.ELITE)
                targetLv *= User.Inst.TBL.Const.CONST_SKILL_HOLE_RESIST_ELITE;
            else if (monsterlist[i].MonGrade == MONSTER_GRADE.BOSS || monsterlist[i].MonGrade == MONSTER_GRADE.STAGE_BOSS)
                targetLv *= User.Inst.TBL.Const.CONST_SKILL_HOLE_RESIST_BOSS;

            float resistanceRate = ((targetLv - ((User.Inst.TBL.Const.CONST_SKILL_HOLE_POWER_ADJUST + SkillLv) * skillLevelPow)) / ((User.Inst.TBL.Const.CONST_SKILL_HOLE_POWER_ADJUST + SkillLv) * skillLevelPow));
            bool isResistance = User.Inst.LocalRandom() <= resistanceRate * 10000 ? true : false;
            if (isResistance)
            {
                UIManager.Inst.SetResistance(monsterlist[i]);
                continue;
            }
            mons.Add(monsterlist[i]);
            monsterlist[i].SetForceTarget(true, skillId, endPos, User.Inst.TBL.Const.CONST_SKILL_HOLE_STUN_TIME);
        }
        skillCorutine = BattleScene.Inst.BattleMode.RequestLightningHoleAfterTime(mons, User.Inst.TBL.Const.CONST_SKILL_HOLE_STUN_TIME, CalculateDamage);
        ShowCastEffect();
        if (!string.IsNullOrEmpty(skillTable.TargetEffect))
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, endPos, owner.transform.localScale.x > 0 ? true : false);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        SetPrevTarget(target, mons);
        owner.holePrevPos = endPos;
        UseMP();
        // 퀘스트 업데이트
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    private void CalculateDamage(Monster monster)
    {
        CalculateDamage(monster, skillData.skillDamageRate);
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        if (skillCorutine != null)
            BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutine);
    }
}
