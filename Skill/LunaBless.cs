using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LunaBless :Skill
{
    //캐릭터 입는 피해 감소 
    //몬스터 디버프 부여
    //피격시 확률적 회복
    private Coroutine tickEvent;
    private GameObject HpAbsorbEff;
    private Transform slotTrans;
    float effTime;

    // 항상 쫓아오는 이펙트
    private GameObject lunaBlessEff;
    private LunaBlessObj lunaBlessObj;
    private Transform lunaBlessSlotTrans;
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        owner.MaxHPRate = (float)skillData.skillTypeOpt;
        tickEvent = BattleScene.Inst.BattleMode.RequestLunaBlessSkill(SetDebuff);
        isHpAbsorbEffShow = false;

        if (lunaBlessEff == null)
        {
            string castPosName = "eff_head";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            lunaBlessSlotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            Vector3 pos = lunaBlessSlotTrans != null ? lunaBlessSlotTrans.position : owner.transform.position;

            lunaBlessEff = EffectManager.Inst.GetEffectObject(skillTable.CastEffect, pos, false);
            if (lunaBlessEff.GetComponent<LunaBlessObj>() == null)
                lunaBlessObj = lunaBlessEff.AddComponent<LunaBlessObj>();
            else
                lunaBlessObj = lunaBlessEff.GetComponent<LunaBlessObj>();
        }
        lunaBlessObj.Init(MoveEff);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        return true;
    }
    private void SetDebuff()
    {
        List<Monster> targets = FindTarget();
        if (targets.Count > 0)
        {
            List<Monster> damageTargets = GetDamageTarget(targets[0]);
            for(int i = 0; i < damageTargets.Count; i++)
                OnDebuff(damageTargets[i], 0, 0);
        }
    }
    public void EndSkill()
    {
        owner.MaxHPRate = 0;
        if (lunaBlessObj) lunaBlessObj.EndEff();
        if (tickEvent != null)
        {
            BattleScene.Inst.BattleMode.EndSkillCorutine(tickEvent);
            tickEvent = null;
        }
        lunaBlessEff = null;
    }
    public override void ResetSkill()
    {
        base.ResetSkill();
    }
    public void RecoveryHp()
    {
        if (isHpAbsorbEffShow)
            return;
        bool isRecoveryHp = User.Inst.LocalRandom() < owner.skillCtr.GetSandGlass_LinkRate(skillData.linkSkillRate[0]) * 10000 ? true : false;
        if (isRecoveryHp == false)
            return;
        var recoveryHp = skillData.skillDamageRate * owner.PerHP;
        
        owner.DrainHP_LunaBless(new System.Numerics.BigInteger((int)recoveryHp));
        ShowHpAbsorbEff();
    }
    bool isHpAbsorbEffShow;
    private bool ShowHpAbsorbEff()
    {
        if (isHpAbsorbEffShow)
            return false;
        if (HpAbsorbEff == null)
        {
            string castPosName = "eff_head";
            slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            UnityEngine.Vector3 pos = slotTrans.position;

            HpAbsorbEff = EffectManager.Inst.GetEffectObject(User.Inst.TBL.Const.CONST_EFFECT_LUNARBLESS_HEAL, pos, false);
            effTime = EffectManager.Inst.GetEffectDurationTime(User.Inst.TBL.Const.CONST_EFFECT_LUNARBLESS_HEAL);
        }
        HpAbsorbEff.SetActive(true);
        isHpAbsorbEffShow = true;
        BattleScene.Inst.BattleMode.RequestCycloneEffect(HpAbsorbEff, effTime, EndHpAbsorbEff);
        return true;
    }
    private void EndHpAbsorbEff()
    {
        HpAbsorbEff.SetActive(false);
        isHpAbsorbEffShow = false;
    }
    private void MoveEff()
    {
        lunaBlessEff.transform.position = lunaBlessSlotTrans.position;
    }

}
