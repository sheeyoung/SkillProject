using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HellFire : Skill
{
    protected GameObject hellFireEff;
    protected SkillObject hellFireSkillObject;
    public float remainTime = 0f;
    public float deltaTickTime = 0f;
    private Transform slotTrans;
    public override bool SkillAttack(double linkRate)
    {
        if (hellFireEff == null && string.IsNullOrEmpty(skillTable.CastEffect) == false)
        {
            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            Vector3 pos = slotTrans.position;

            hellFireEff = EffectManager.Inst.GetEffectObject(skillTable.CastEffect, pos, owner.transform.localScale.x > 0 ? true : false);

            if (hellFireEff.GetComponent<SkillObject>() == null)
                hellFireSkillObject = hellFireEff.AddComponent<SkillObject>();
            else
                hellFireSkillObject = hellFireEff.GetComponent<SkillObject>();
        }

        remainTime = (float)skillData.skillTypeOpt;
        deltaTickTime = 0f;

        hellFireSkillObject.Init(UpdateSkill);

        hellFireEff.SetActive(true);
        Vector3 temp = slotTrans.position;
        temp.z = 0f;
        hellFireEff.transform.position = temp;
        hellFireEff.transform.eulerAngles = owner.WeaponRotate.eulerAngles;
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        return true;
    }
    public override void CollideAttack(Projectile projectile, Monster target)
    {
        List<Monster> targets = GetDamageTarget();
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

            if (targets[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, targets[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
            }
        }

        if (mon != null)
        {
            if (skillTable.TargetEffectType == 2 && !string.IsNullOrEmpty(skillTable.TargetEffect))
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, mon.transform.position, owner.transform.localScale.x > 0 ? true : false);
            }

            //폭발
            ExplosionDamage(mon.transform.position);
        }
    }
    public void UpdateSkill(float deltaTime)
    {
        remainTime -= deltaTime;
        deltaTickTime -= deltaTime;
        Vector3 temp = slotTrans.position;
        temp.z = 0f;
        hellFireEff.transform.position = temp;
        hellFireEff.transform.eulerAngles = owner.WeaponRotate.eulerAngles;

        Vector3 tempScale = hellFireEff.transform.localScale;
        tempScale.x = Mathf.Abs(tempScale.x) * (owner.transform.localScale.x > 0 ? 1 : -1);
        hellFireEff.transform.localScale = tempScale;

        if (deltaTickTime <= 0)
        {
            deltaTickTime = User.Inst.TBL.Const.CONST_HELLFIRE_TIC;
            CollideAttack(null, null);
        }

        if (remainTime <= 0f)
        {
            SkillEnd();
            return;
        }
    }
    public void SkillEnd()
    {
        remainTime = 0;
        if(hellFireEff) hellFireEff.SetActive(false);
        if(hellFireSkillObject) hellFireSkillObject.EndEff();
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        remainTime = 0;
        if (hellFireEff) hellFireEff.SetActive(false);
        if (hellFireSkillObject) hellFireSkillObject.EndEff();
    }
}
