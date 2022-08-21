using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Shield : Skill
{
    protected System.Numerics.BigInteger HP;
    protected GameObject shieldEff;
    public float remainTime = 0f;
    private Transform slotTrans;
    public bool IsShield()
    {
        if (shieldEff == null)
            return false;
        return HP > 0;
    }
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        bool isPlayShield = User.Inst.LocalRandom() < owner.skillCtr.GetSandGlass_LinkRate(skillData.linkSkillRate[0]) * 10000 ? true : false;
        if (isPlayShield == false)
            return false;
        if (shieldEff == null && string.IsNullOrEmpty(skillTable.CastEffect) == false)
        {
            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            Vector3 pos = slotTrans.position;

            shieldEff = EffectManager.Inst.GetEffectObject(skillTable.CastEffect, pos, owner.transform.localScale.x > 0 ? true : false);
        }

        double hpRate = skillData.skillDamageRate * 100d;
        if (remainTime <= 0f)
            HP = (owner.OrigMaxHP * (int)hpRate) / 100;
        else
        {
            HP += owner.OrigMaxHP * (int)hpRate / 100;
            var MaxHp = (owner.OrigMaxHP * (int)(User.Inst.TBL.Const.CONST_BARRIER_MAX_HP * 100f)) / 100;
            if (HP > MaxHp)
                HP = MaxHp;
        }
        remainTime = (float)skillData.skillTypeOpt;
        shieldEff.SetActive(true);
        Vector3 temp = slotTrans.position;
        temp.z = 0f;
        shieldEff.transform.position = temp;
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        return true;
    }
    public System.Numerics.BigInteger OnDamageShield(System.Numerics.BigInteger damage)
    {
        if (HP <= 0)
            return damage;
        if (remainTime <= 0f)
            return damage;
        System.Numerics.BigInteger remainDamage = new System.Numerics.BigInteger(0);
        if (damage > HP)
        {
            remainDamage = damage - HP;
        }

        HP -= damage;
        if (HP <= 0)
        {
            ShieldEnd();
        }
        return remainDamage;
    }
    public void UpdateShield(float deltaTime)
    {
        if (remainTime > 0f)
        {
            if (HP <= 0)
            {
                ShieldEnd();
                return;
            }
            remainTime -= deltaTime;
            Vector3 temp = slotTrans.position;
            temp.z = 0f;
            shieldEff.transform.position = temp;

            if (remainTime <= 0f)
            {
                ShieldEnd();
            }
        }
    }
    public void ShieldEnd()
    {
        HP = 0;
        remainTime = 0;
        shieldEff.SetActive(false);
    }
}
