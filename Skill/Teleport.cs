using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Teleport : Skill
{
    GameObject tarilEff;
    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);
    }

    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        return true;
    }
    public bool CheckTeleportMove(int targetPriority, float skillRange, float addSkillRange)
    {
        Vector2 moveDir = owner.MoveVec; ;

        var monsterlist = NowMode.MonSpawnMgr.Monsters.FindAll(m => m.ActionCtrl.State != CHAR_ACTION_STATE.DIE);
        if (monsterlist.Count <= 0)
            return false;
        List<Monster> targets = null;

        if (targetPriority == 1)
            targets = ((PCActionController)owner.ActionCtrl).FindTargetByDist(monsterlist, true, false);
        else
            targets = ((PCActionController)owner.ActionCtrl).FindTargetByGrade(monsterlist);

        float distance = Vector3.Distance(targets[0].transform.position, owner.transform.position);
        float moveDis = distance - skillRange;
        if (moveDis < 0)
            return false;

        double teleportMax = skillData.skillRange + addSkillRange;
        if (moveDis > teleportMax)
        {
            decimal moveDisDecimal = (decimal)teleportMax;
            moveDis = (float)moveDisDecimal;
        }
        if (moveDis < User.Inst.TBL.Const.CONST_SKILL_TELEPORT_DISTANCE_MIN)
            moveDis = User.Inst.TBL.Const.CONST_SKILL_TELEPORT_DISTANCE_MIN;
        Vector3 vecNormal = (targets[0].transform.position - owner.transform.position).normalized;
        vecNormal = owner.transform.position + new Vector3(vecNormal.x * moveDis, vecNormal.y * moveDis, vecNormal.z * moveDis);
        if (float.IsNaN(vecNormal.x) || float.IsNaN(vecNormal.y) || float.IsNaN(vecNormal.z))
            return false;
        TeleportMove(vecNormal);

        return true;
    }
    public bool CheckTeleport_TouchMove(Vector3 movePos, float addSkillRange)
    {
        float distance = Vector3.Distance(movePos, owner.transform.position);
        float moveDis = distance;
        if (moveDis < 0)
            return false;

        double teleportMax = skillData.skillRange + addSkillRange;
        if (moveDis > teleportMax)
        {
            decimal moveDisDecimal = (decimal)teleportMax;
            moveDis = (float)moveDisDecimal;
        }
        if (moveDis < User.Inst.TBL.Const.CONST_SKILL_TELEPORT_DISTANCE_MIN)
            return false;
        Vector3 vecNormal = (movePos - owner.transform.position).normalized;
        vecNormal = owner.transform.position + new Vector3(vecNormal.x * moveDis, vecNormal.y * moveDis, vecNormal.z * moveDis);
        if (float.IsNaN(vecNormal.x) || float.IsNaN(vecNormal.y) || float.IsNaN(vecNormal.z))
            return false;
        TeleportMove(vecNormal);

        return true;
    }


    public void TeleportMove(Vector3 normal)
    {
        if (tarilEff == null)
        {
            tarilEff = EffectManager.Inst.GetEffectObject("eff_PC_taril_c");
        }
        owner.DustEffect.SetActive(false);
        tarilEff.SetActive(true);
        Vector3 pos = owner.transform.position;
        tarilEff.transform.position = pos;
        ShowCastEffect();

        owner.skillCtr.WindShoe_DamageUp(pos, normal);

        ((PCActionController)owner.ActionCtrl).SetTeleport(normal);
        UseMP();
        ShowCastEffect();
        BattleScene.Inst.BattleMode.RequestTeleportEffect(tarilEff, EndTarilEffect);
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);
        owner.skillCtr.WindShoe_Attack();
    }

    public void EndTarilEffect()
    {
        if (tarilEff) tarilEff.SetActive(false);
        owner.DustEffect.SetActive(true);
    }

    public override bool IsSkillTarget(Dictionary<MONSTER_GRADE, int> conditions)
    {
        return true;
    }
}
