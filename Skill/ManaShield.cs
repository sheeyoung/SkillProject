using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaShield : Skill
{
    protected System.Numerics.BigInteger decManaValue;
    protected GameObject shieldEff;
    private Transform slotTrans;
    public float ManaRate = 50;
    private int passiveIndex = 0;
    bool isInit = true;
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        if (isInit == false)
            return false;
        isInit = false;

        if (shieldEff == null && string.IsNullOrEmpty(skillTable.CastEffect) == false)
        {
            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            Vector3 pos = slotTrans.position;

            shieldEff = EffectManager.Inst.GetEffectObject(skillTable.CastEffect, pos, owner.transform.localScale.x > 0 ? true : false);
        }

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
    public System.Numerics.BigInteger OnDamageShield(System.Numerics.BigInteger damage, Character attacker)
    {
        if (damage <= 0)
            return damage;

        float curManaRate = (float)(owner.MP * 10000 / owner.MaxMP);

        if(passiveIndex == 0)
        {
            foreach (var item in User.Inst.TBL.PassiveSkill)
            {
                if (item.Value.SkillGroup.Contains(skillTable.Index))
                {
                    passiveIndex = item.Value.Index;
                    break;
                }
            }
        }

        int manaRateIdx = User.Inst.Doc.Skills[passiveIndex].ManaRateIdx;
        float rate = User.Inst.TBL.ManaRate[1].ManaRate;
        if (User.Inst.TBL.ManaRate.ContainsKey(manaRateIdx))
        {
            rate = User.Inst.TBL.ManaRate[manaRateIdx].ManaRate;
        }
        ManaRate = rate * 10000f;

        if (curManaRate < (int)ManaRate)// 마나비율 조절
        {
            ShieldEnd();
            return damage;
        }
        SkillAttack(1);
        System.Numerics.BigInteger remainDamage = new System.Numerics.BigInteger(0);


        var damageRate = (float)(damage * 10000/ owner.OrigMaxHP);
        var remainRate = damageRate - curManaRate;
        if (remainRate > 0)
        {
            remainDamage = damage * (int)remainRate;
            remainDamage = remainDamage / 10000;
        }
        double maxMPD = double.Parse(owner.MaxMP.ToString());
        var decMana = maxMPD * damageRate / 10000;
        owner.UseMp((int)decMana);

        //반격
        if (ActiveCoolTime > 0)
            return remainDamage;

        ActiveCoolTime = Mathf.Max((float)(CoolTime * owner.SkillCoolTime), User.Inst.TBL.Const.CONST_SKILL_COOLTIME_MIN);

        var counterAttackDamage = (decMana / maxMPD) / (skillData.skillTypeOpt / 100d) * skillData.skillDamageRate;
        CalculateDamage((Monster)attacker, counterAttackDamage);

        return remainDamage;
    }
    public void UpdateShield(float deltaTime)
    {
        if (isInit)
            return;
        if (shieldEff == null)
            return;
        Vector3 temp = slotTrans.position;
        temp.z = 0f;
        shieldEff.transform.position = temp;
    }
    public void ShieldEnd()
    {
        decManaValue = 0;
        isInit = true;
        if(shieldEff) shieldEff.SetActive(false);
    }
}
