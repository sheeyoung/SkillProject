using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using Net.Impl;

public abstract class Skill
{
    protected StringBuilder sb = new StringBuilder(0);
    protected PC owner = null;
    protected int skillId = 0;
    protected double linkRate = 0;

    public int MaxLevel { private set; get; }
    public virtual void UpdateTarget() { }
    public abstract bool SkillAttack(double linkRate);
    public virtual void CollideAttack(Projectile projectile, Monster target) { }
    public virtual void SkillEffect() { }

    protected BattleMode NowMode
    {
        get { return BattleScene.Inst.BattleMode; }
    }
    public ActiveSkillStat skillData { get { return PCTableManager.Inst.ActiveSkillInfo[skillId]; } }
    protected TBL.Sheet.CActiveSkill skillTable = default(TBL.Sheet.CActiveSkill);
    // 테이블 쿨타임 시간
    public float CoolTime
    {
        get
        {
            if (skillData.Equals(default(TBL.Sheet.CActiveSkill))) return 0f;
            decimal coolTime = (decimal)skillData.skillCoolTime;
            return (float)coolTime;

            //if (skillData.skillGroup != 4)
            //    return (float)coolTime;

            //var specialOpt = skillData.GetSpecialType(12);
            //if (specialOpt == 0)
            //    return (float)coolTime;

            //var beforeCoolTime = skillData.skillCoolTime * (1 - (double)specialOpt);
            //decimal beforeCoolTimed = (decimal)beforeCoolTime;

            //return (float)beforeCoolTimed;
        }
    }

    //현재 쿨타임
    public float ActiveCoolTime;

    protected bool isCheckBonusSkill = false;
    public bool IsUseBonusSkill { set; get; }
    protected float bonusSkillRate = 0f;            // 보너스 스킬 발동 확률

    public int SkillLv { get { if (User.Inst.Doc == null || (User.Inst.Doc != null && User.Inst.Doc.Skills.ContainsKey(skillId) == false)) return 0; return User.Inst.Doc.Skills[skillId].Level; } }

    public int TargetPriority { get { return skillData.targetPriority; } }

    // 피해 범위 표시 이펙트
    protected GameObject groundEffect = null;

    public virtual void InitSkill(PC o, int skillID)
    {
        owner = o;
        skillId = skillID;
        ActiveCoolTime = 0f;
        skillTable = User.Inst.TBL.ActiveSkill[skillID];
    }

    public bool CheckNeedMP()
    {
        return owner.MP >= new System.Numerics.BigInteger(skillData.needMp);
    }
    public int GetSkillId() { return skillId; }
    public virtual void DrawGroundEffect(Vector3 pos) { }
    public void ReleaseGroundEffect(string effectName)
    {
        groundEffect = null;
    }
    public virtual bool IsSkillTarget(Dictionary<MONSTER_GRADE, int> conditions)
    {
        return IsSkillTarget(conditions, owner.transform.position);
    }

    public virtual bool IsSkillTarget(Dictionary<MONSTER_GRADE, int> conditions, Vector3 pos)
    {
        owner.Target = null;
        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(pos, 0, skillData.skillRange);
        if (conditions != null)
        {
            foreach (var c in conditions)
            {
                var newlist = monsterlist.FindAll(m => m.MonGrade >= c.Key);
                if (newlist != null && newlist.Count >= c.Value)
                {
                    owner.Target = newlist[0];
                    return true;
                }
            }
        }
        else
        {
            owner.Target = monsterlist.Count > 0 ? monsterlist[0] : null;
            return monsterlist.Count > 0;
        }
        return false;
    }

    public double GetDebuffIncAtk(Monster target)
    {
        double incatk = 0;
        if (skillData.debuffAddDamageType != 0)
        {
            if (target.DebuffMgr.Debuffs.ContainsKey(skillData.debuffAddDamageType))
                incatk += skillData.debuffAddDamageRate;
        }
        return incatk;
    }

    public virtual void OnDebuff(Monster target, double dam, System.Numerics.BigInteger bigDam)
    {
        if (skillData.debuffPassives.Count <= 0)
            return;
        for (int i = 0; i < skillData.debuffPassives.Count; i++)
        {
            if (skillData.debuffPassives[i] == null)
                continue;
            if (skillData.debuffPassives[i].specialOpt * 10000 < User.Inst.LocalRandom())
                return;
            SKILL_SPECIAL_TYPE skillSpecialType = (SKILL_SPECIAL_TYPE)skillData.debuffPassives[i].specialType;
            switch (skillSpecialType)
            {
                case SKILL_SPECIAL_TYPE.INC_DAMAGE:
                    {
                        target.DebuffMgr.AddDebuff(owner, 1, 0d);
                    }
                    break;
                case SKILL_SPECIAL_TYPE.DEBUFF_STUN:
                    {
                        target.DebuffMgr.AddDebuff(owner, 2, 0d);
                    }
                    break;
                case SKILL_SPECIAL_TYPE.DEBUFF_SHOCK:
                    {
                        target.DebuffMgr.AddDebuff(owner, 3, dam);
                    }
                    break;
                case SKILL_SPECIAL_TYPE.DEBUFF_BURN:
                    {
                        target.DebuffMgr.AddDebuff(owner, 4, dam);
                    }
                    break;
                case SKILL_SPECIAL_TYPE.DEBUFF_CHAOS:
                    {
                        target.DebuffMgr.AddDebuff(owner, 5, 0d);
                    }
                    break;
                case SKILL_SPECIAL_TYPE.DEBUFF_MARK:
                    {
                        target.DebuffMgr.AddDebuff(owner, 6, 0d);
                    }
                    break;
                case SKILL_SPECIAL_TYPE.DEBUFF_OVERLOAD:
                    {
                        target.DebuffMgr.AddDebuff(owner, 19, bigDam);
                    }
                    break;
                case SKILL_SPECIAL_TYPE.DEBUFF_DECLARE_AREA:
                    {
                        var specialOpt = skillData.GetSpecialType(207);
                        target.DebuffMgr.AddDebuff(owner, 20, specialOpt);
                    }
                    break;
            }
        }
    }



    protected void ShowCastEffect()
    {
        if (string.IsNullOrEmpty(skillTable.CastEffect) == false)
        {
            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            Transform slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            Vector3 pos = slotTrans != null ? slotTrans.position : owner.transform.position;

            EffectManager.Inst.ShowEffect(skillTable.CastEffect, owner.transform.position, owner.transform.localScale.x > 0 ? true : false);
        }
    }

    protected virtual void CalculateDamage(Monster monster, double damageRate, bool isDebuff = true, double burnUpAtkValue = 0d)
    {
        double totalSkillDamageRate = damageRate;

        if (linkRate > 1)
        {
            // 모래 시계
            double sandGlassDamageRate = 0d;
            if (owner.Type == CHARACTER_TYPE.AVATAR) 
            {
                sandGlassDamageRate = BattleScene.Inst.BattleMode.Pc.skillCtr.GetSandGlass_DamageRate();
            }
            else
            {
                sandGlassDamageRate = owner.skillCtr.GetSandGlass_DamageRate();
            }
            if (sandGlassDamageRate > 0)
            {
                double overRate = linkRate - 1d;
                if (overRate < 0)
                    overRate = 0;
                overRate *= 100f;
                sandGlassDamageRate *= overRate;
                totalSkillDamageRate = totalSkillDamageRate * (1 + sandGlassDamageRate);
            }
        }

        double damage = 1;

        bool isCri = User.Inst.LocalRandom() < owner.CriRate * 10000 ? true : false;
        bool isDCri = false;
        bool isTCri = false;
        if (isCri)
        {
            if (owner.CriDam != 0)
            {
                damage *= owner.CriDam;
            }

            isDCri = User.Inst.LocalRandom() < owner.D_CriRate * 10000 ? true : false;
            if (isDCri)
            {
                if (owner.D_CriDam != 0)
                {
                    damage *= owner.D_CriDam;
                }
                isTCri = User.Inst.LocalRandom() < owner.T_CriRate * 10000 ? true : false;
                if (isTCri)
                {
                    if (owner.T_CriDam != 0)
                    {
                        damage *= owner.T_CriDam;
                    }
                }
            }
        }
        UI.DAMAGE_TYPE damageType = isTCri ? UI.DAMAGE_TYPE.MON_TRIPLE_CRI : (isDCri ? UI.DAMAGE_TYPE.MON_DOUBLE_CRI : (isCri ? UI.DAMAGE_TYPE.MON_CRI : UI.DAMAGE_TYPE.None));
        
        long totalSkillDamageRate_Int = (int)(totalSkillDamageRate * 100);
        System.Numerics.BigInteger attackSkillDmg = owner.AttackPower;

        attackSkillDmg = (totalSkillDamageRate_Int * owner.AttackPower) / 100;
        if(burnUpAtkValue > 0d)
        {
            attackSkillDmg = (long)burnUpAtkValue + attackSkillDmg;
        }

        if (attackSkillDmg < 0)
            attackSkillDmg = 1;

        double debuffincatk = GetDebuffIncAtk(monster);
        double debuffincdam = monster.DebuffMgr.TotalIncDam;

        if(owner.Type == CHARACTER_TYPE.AVATAR)
        {
            damage = damage * (1 + debuffincatk) * (1 + debuffincdam) * (1 - BattleScene.Inst.BattleMode.Pc.DebuffMgr.TotalDecATK);
        }
        else
        {
            damage = damage * (1 + debuffincatk) * (1 + debuffincdam) * (1 - owner.DebuffMgr.TotalDecATK);
        }

        attackSkillDmg = SetManaBoost(attackSkillDmg * new System.Numerics.BigInteger(damage), damageType);
        attackSkillDmg = SetAtkUPSpecialType(attackSkillDmg);

        if (!(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            attackSkillDmg = new System.Numerics.BigInteger((double)attackSkillDmg * (1 + owner.GetStatValue(Doc.STAT.SKILLATK)));

        // 마나압축
        if (owner.Type == CHARACTER_TYPE.AVATAR)
        {
            if (BattleScene.Inst.BattleMode.Pc.ComPressionMPRate != 100)
            {
                attackSkillDmg = attackSkillDmg * (1 + (int)(((double)BattleScene.Inst.BattleMode.Pc.ComPressionMPRate) * BattleScene.Inst.BattleMode.Pc.skillCtr.SPECIALTYPE_13_VALUE));
            }
        }
        else
        {
            if (owner.ComPressionMPRate != 100)
            {
                attackSkillDmg = attackSkillDmg * (1 + (int)(((double)owner.ComPressionMPRate) * owner.skillCtr.SPECIALTYPE_13_VALUE));
            }
        }

        // 윈드슈즈
        int windShoeDamageRate = 0;
        if (owner.Type == CHARACTER_TYPE.AVATAR)
        {
            windShoeDamageRate = (int)BattleScene.Inst.BattleMode.Pc.skillCtr.WindShoe_DamageRate();
        }
        else
        {
            windShoeDamageRate = (int)owner.skillCtr.WindShoe_DamageRate();
        }
        if (windShoeDamageRate <= 0)
            windShoeDamageRate = 1;
        attackSkillDmg = attackSkillDmg * windShoeDamageRate;

        attackSkillDmg = owner.ApplyAtkAmplify(skillId, monster, attackSkillDmg);

        if (owner.Type == CHARACTER_TYPE.AVATAR)
        {
            // 아바타의 공격력
            double avatarDamageRate = BattleScene.Inst.BattleMode.Pc.skillCtr.GetAvatarDamageRate();
            int damageRateInt = (int)avatarDamageRate;
            attackSkillDmg = attackSkillDmg * damageRateInt;
        }
        monster.OnDamaged(owner, damageType, attackSkillDmg, skillId);
        
        if (isDebuff)
            OnDebuff(monster, damage, attackSkillDmg);
        // 체력 흡수 : 6
        SetDrainLife(attackSkillDmg);

    }

    protected void ExplosionDamage(Vector3 centerPos)
    {
        //폭발화
        if (skillData.skillExplotionRange > 0)
        {
            var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(centerPos, 0, skillData.skillExplotionRange);

            for (int i = 0; i < monsterlist.Count; i++)
            {
                CalculateDamage(monsterlist[i], skillData.explosionDamageRate);
                if (monsterlist[i] != null && (monsterlist[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE))
                {
                    monsterlist[i].ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
                }

                if (monsterlist[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
                {
                    EffectManager.Inst.ShowEffect(skillTable.TargetEffect, monsterlist[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
                }
            }
        }
    }

    protected virtual List<Monster> FindTarget()
    {
        List<Monster> targets = new List<Monster>();
        if (skillData.skillTarget == 3) // 자기 주변 광역
        {
            var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0, skillData.skillRange);
            for (int i = 0; i < monsterlist.Count; i++)
                targets.Add(monsterlist[i]);
        }
        else
        {
            var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0f, skillData.skillRange);
            if (skillData.targetPriority == 1)
            {
                targets = ((PCActionController)owner.ActionCtrl).FindTargetByDist(monsterlist, true, false);
            }
            else
            {
                targets = ((PCActionController)owner.ActionCtrl).FindTargetByGrade(monsterlist);
            }
        }
        if (targets.Count > 0)
            owner.Target = targets[0];
        return targets;
    }
    protected List<Monster> GetDamageTarget(Monster target = null)
    {
        List<Monster> targets = new List<Monster>();
        if (skillData.skillTarget == 1) //대상
        {
            targets.Add(target);
        }
        else if (skillData.skillTarget == 2)
        {
            targets.Add(target);
        }
        else if (skillData.skillTarget == 3) // 자기 주변 광역
        {
            var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0, skillData.skillRangeOpt);
            for (int i = 0; i < monsterlist.Count; i++)
                targets.Add(monsterlist[i]);
        }
        else if (skillData.skillTarget == 4) //대상주변
        {
            if (target == null)
                return targets;
            var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(target.transform.position, 0, skillData.skillRangeOpt);
            for (int i = 0; i < monsterlist.Count; i++)
                targets.Add(monsterlist[i]);
        }
        else if (skillData.skillTarget == 6) // 부채꼴
        {
            var monsterlist = NowMode.FindMonsterListByMonsterPosFanShapeRange(owner, skillData.skillRange, owner.LockTargetDir, skillData.skillRangeOpt);
            for (int i = 0; i < monsterlist.Count; i++)
                targets.Add(monsterlist[i]);
        }
        return targets;
    }
    protected System.Numerics.BigInteger SetManaBoost(System.Numerics.BigInteger attackSkillDmg, UI.DAMAGE_TYPE damageType)
    {
        System.Numerics.BigInteger damage = attackSkillDmg;
        if (User.Inst.Doc.Skills.ContainsKey(PCTableManager.Inst.MANABOOST_SKILLINDEX)
               && PCTableManager.Inst.PassiveSkillInfo.ContainsKey(PCTableManager.Inst.MANABOOST_SKILLINDEX))
        {
            var tableInfo = PCTableManager.Inst.PassiveSkillInfo[PCTableManager.Inst.MANABOOST_SKILLINDEX];
            double manaRate = 0d;
            if(owner.Type == CHARACTER_TYPE.AVATAR)
            {
                manaRate = (double)((BattleScene.Inst.BattleMode.Pc.MP * 100) / BattleScene.Inst.BattleMode.Pc.MaxMP);
            }
            else
            {
                manaRate = (double)((owner.MP * 100) / owner.MaxMP);
            }
            
            double addRate = 0d;
            for (int i = 0; i < tableInfo.specialOpt.Count; i++)
            {
                if (tableInfo.specialType[i] == 4)
                {
                    addRate = tableInfo.specialOpt[i];
                    break;
                }
            }
            if (addRate > 0d)
            {
                manaRate *= addRate;

                damage = damage * (int)((1d + manaRate) * 100d);
                damage /= 100;
            }
            
        }
        return damage;
    }

    protected virtual void SetDrainLife(System.Numerics.BigInteger damage)
    {
        var specialOpt = skillData.GetSpecialType(6);
        if (specialOpt == 0)
            return;

        float rate = User.Inst.TBL.Const.CONST_SKILL_HBABSORB_RATE;
        rate *= 100;
        damage *= (int)rate;
        damage /= 100;

        var maxHP = owner.OrigMaxHP;
        specialOpt *= 100f;
        maxHP *= (int)specialOpt;
        maxHP /= 100;
        if (damage > maxHP)
            damage = maxHP;

        owner.DrainHP(damage);
    }

    protected System.Numerics.BigInteger SetAtkUPSpecialType(System.Numerics.BigInteger damage)
    {
        var specialOpt = skillData.GetSpecialType(9);
        if (specialOpt == 0)
            return damage;
        specialOpt = (1f + specialOpt) * 100f;
        damage *= (int)specialOpt;
        damage /= 100;

        return damage;
    }

    protected void UseMP()
    {
        var specialOpt = skillData.GetSpecialType(10);
        if (specialOpt == 0)
        {
            owner.UseMp(new System.Numerics.BigInteger(skillData.needMp));
            return;
        }
        var useMpValue = skillData.needMp * (1f - specialOpt);
        if (useMpValue < 0)
            useMpValue = 0;
        owner.UseMp(new System.Numerics.BigInteger(useMpValue));
    }

    public virtual void ResetSkill()
    {
        ActiveCoolTime = 0f;
    }
    public virtual void ClearSkill()
    {

    }

    public bool CheckNeedHP()
    {
        if (skillData.needHp == 0)
            return true;
        string str_maxHp = owner.OrigMaxHP.ToString();
        double maxHp = System.Convert.ToDouble(str_maxHp);
        double maxNeedHp = maxHp * skillData.needHp;

        var specialOpt = skillData.GetSpecialType(11);
        if (specialOpt > 0f)
        {
            var burnHpValue = maxNeedHp * specialOpt;
            if (burnHpValue < 0)
                burnHpValue = 0;
            maxNeedHp = burnHpValue;
        }

        if (new System.Numerics.BigInteger(maxNeedHp) >= owner.HP)
        {
            return false;
        }
        return true;
    }

    protected void SetPrevTarget(Monster centerMob, List<Monster> monsters)
    {
        if (owner.holePrevTargets == null)
            owner.holePrevTargets = new List<Monster>();
        if (centerMob == null)
            return;
        owner.holePrevTargets.Clear();
        if (centerMob.ActionCtrl.State != CHAR_ACTION_STATE.DIE)
            owner.holePrevTargets.Add(centerMob);
        for (int i = 0; i < monsters.Count; i++)
        {
            if (monsters[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE
                && owner.holePrevTargets.Contains(monsters[i]) == false)
                owner.holePrevTargets.Add(monsters[i]);
        }
    }
}


