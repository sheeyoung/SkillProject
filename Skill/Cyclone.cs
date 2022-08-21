using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.Events;

public class Cyclone : Skill
{
    protected int projectileId = 0;
    protected TBL.Sheet.CProjectile projectileInfo = default(TBL.Sheet.CProjectile);
    protected ObjectPool<Projectile> projectilePool = null;
    private Projectile curProjectile;
    private GameObject HpAbsorbEff;
    private Transform slotTrans;
    float effTime;
    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);

        projectileId = 0;
        if (skillTable.ProjectileID != 0)
        {
            projectileId = skillTable.ProjectileID;
            projectileInfo = User.Inst.TBL.Projectile[projectileId];

            short projectileCount = 1;
            sb.Length = 0;
            sb.AppendFormat("{0}{1}", ResourcePath.Projectile, projectileInfo.PrefabName);
            GameObject resObj = ResManager.Inst.Load<GameObject>(sb.ToString());
            projectilePool = new ObjectPool<Projectile>(projectileCount, () =>
            {
                GameObject obj = GameObject.Instantiate(resObj);
                Projectile projectile = null;
                projectile = obj.AddComponent<CycloneProjectile>();
                projectile.Init(projectileInfo, CollideAttack, EndSkillAttack);
                projectile.gameObject.SetActive(false);
                return projectile;
            });
        }
        isHpAbsorbEffShow = false;
    }
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;

        if (projectilePool != null)
        {
            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            Transform slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            UnityEngine.Vector3 pos = slotTrans != null ? slotTrans.position: owner.transform.position;

            BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
            {
                Projectile projectile = projectilePool.Pop();
                projectile.Attack(owner, pos, UnityEngine.Vector3.zero, (float)skillData.skillTypeOpt);
                projectile.transform.localScale = new UnityEngine.Vector3(1, 1, 1);
                curProjectile = projectile;
            });
        }

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    public void EndSkillAttack(Projectile projectile)
    {
        if (curProjectile == projectile)
            curProjectile = null;
        ReleaseProjectile(projectile);
    }
    public void ReleaseProjectile(Projectile projectile)
    {
        if (projectile && projectile.gameObject.activeSelf && projectileId != 0)
        {
            projectile.gameObject.SetActive(false);
            projectilePool.Push(projectile);
        }
    }

    public override void CollideAttack(Projectile projectile, Monster target)
    {
        if (target == null)
            return;
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

        CalculateDamage(target, skillData.skillDamageRate);
        if (target.ActionCtrl.State != CHAR_ACTION_STATE.DIE)
        {
            target.ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
        }

        if (skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
        {
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, target.Body.position, owner.transform.localScale.x > 0 ? true : false);
        }
        if (skillTable.TargetEffectType == 2)
        {
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, target.transform.position, owner.transform.localScale.x > 0 ? true : false);
        }
    }
    protected override void SetDrainLife(BigInteger damage)
    {
        if (isHpAbsorbEffShow)
            return;
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
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            UnityEngine.Vector3 pos = slotTrans.position;

            HpAbsorbEff = EffectManager.Inst.GetEffectObject(User.Inst.TBL.Const.CONST_EFFECT_HPABSORB, pos,false);
            effTime = EffectManager.Inst.GetEffectDurationTime(User.Inst.TBL.Const.CONST_EFFECT_HPABSORB);
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
    
    public override void ResetSkill()
    {
        base.ResetSkill();
        isHpAbsorbEffShow = false;
        if (curProjectile != null)
            ReleaseProjectile(curProjectile);
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        isHpAbsorbEffShow = false;
        if (curProjectile != null)
            ReleaseProjectile(curProjectile);
    }
}
