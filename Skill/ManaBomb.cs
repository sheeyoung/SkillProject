using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManaBomb : Skill
{
    public float ManaRate = 50;
    private int passiveIndex = 0;
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        return true;
    }
    public void PlayManaBomb(Character attacker, System.Numerics.BigInteger damage)
    {
        var curManaRate = owner.MP * 100 / owner.MaxMP;


        if (passiveIndex == 0)
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

        ManaRate = rate * 100f;

        if (curManaRate < (int)ManaRate)
            return;

        bool isBomb = User.Inst.LocalRandom() < owner.skillCtr.GetSandGlass_LinkRate(skillData.linkSkillRate[0]) * 10000 ? true : false;
        if (isBomb == false)
            return;


        var reduceHpRate = damage * 10000 / owner.OrigMaxHP;

        double reduceHpRateD = double.Parse(reduceHpRate.ToString());

        double maxMPD = double.Parse(owner.MaxMP.ToString());
        var reduceMpValue = maxMPD * reduceHpRateD / 10000;
        owner.UseMp((int)reduceMpValue);
        var attackDamage = (reduceMpValue/ skillData.skillTypeOpt) * skillData.skillDamageRate;
        if (attackDamage <= 0)
            return;
        if (attacker != null)
        {
            List<Monster> damageTargets = GetDamageTarget((Monster)attacker);
            for (int i = 0; i < damageTargets.Count; i++)
            {
                CalculateDamage(damageTargets[i], attackDamage);
                if (damageTargets[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
                {
                    EffectManager.Inst.ShowEffect(skillTable.TargetEffect, damageTargets[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
                }
            }

            if (attacker != null)
            {
                if (skillTable.TargetEffectType == 2)
                {
                    EffectManager.Inst.ShowEffect(skillTable.TargetEffect, attacker.transform.position, owner.transform.localScale.x > 0 ? true : false);
                }
            }
        }
        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);
    }
}
