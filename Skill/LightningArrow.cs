using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningArrow : CommonAttack
{
    protected int addProjectileId = 0;
    protected TBL.Sheet.CProjectile addProjectileInfo = default(TBL.Sheet.CProjectile);
    protected ObjectPool<ArrowProjectile> additionProjectilePool = null;
    private const int additionProjectileMaxCount = 8;

    protected List<Projectile> curAddProjectiles = new List<Projectile>();
    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);

        projectileId = 0;
        if (skillTable.ProjectileID != 0)
        {
            projectileId = skillTable.ProjectileID;
            projectileInfo = User.Inst.TBL.Projectile[projectileId];

            short projectileCount = 1;
            if (skillData.skillType == 1)
                projectileCount = (short)(skillData.skillRangeOpt * (double)skillData.skillProjectileCount);
            else
                projectileCount = (short)skillData.skillProjectileCount;
            sb.Length = 0;
            sb.AppendFormat("{0}{1}", ResourcePath.Projectile, projectileInfo.PrefabName);
            GameObject resObj = ResManager.Inst.Load<GameObject>(sb.ToString());
            projectilePool = new ObjectPool<Projectile>(projectileCount, () =>
            {
                GameObject obj = GameObject.Instantiate(resObj);
                Projectile projectile = null;
                if (projectileInfo.Type == 1)
                {
                    projectile = obj.AddComponent<DirectFiring>();
                }
                projectile.Init(projectileInfo, BaseCollideAttack, EndSkillAttack);
                projectile.gameObject.SetActive(false);
                return projectile;
            });

            addProjectileId = User.Inst.TBL.Const.CONST_PC_SKILL_SPREAD_P;
            addProjectileInfo = User.Inst.TBL.Projectile[addProjectileId];
            sb.Length = 0;
            sb.AppendFormat("{0}{1}", ResourcePath.Projectile, addProjectileInfo.PrefabName);
            GameObject resObj2 = ResManager.Inst.Load<GameObject>(sb.ToString());
            additionProjectilePool = new ObjectPool<ArrowProjectile>(projectileCount, () =>
            {
                GameObject obj = GameObject.Instantiate(resObj2);
                ArrowProjectile projectile = null;
                projectile = obj.AddComponent<ArrowProjectile>();
                projectile.Init(addProjectileInfo, AdditionCollideAttack, EndSkillAttackAddition);
                projectile.gameObject.SetActive(false);
                return projectile;
            });
        }
    }
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        List<Monster> targets = FindTarget();

        Monster target = targets.Count > 0 ? targets[0] : null;
        Vector3 pos = Vector3.zero;
        if (target != null)
        {
            Transform slotTrans = UtilFunction.FindTransform("main", owner.transform);
            Vector3 startPos = slotTrans != null ? slotTrans.position : owner.transform.position;

            pos = new Vector2(target.transform.position.x, target.transform.position.y) - new Vector2(owner.transform.position.x, owner.transform.position.y);
            pos.Normalize();

            decimal skillRange = (decimal)skillData.skillRange;
            float fSkillRange = (float)skillRange;
            pos = new Vector3(pos.x * fSkillRange, pos.y * fSkillRange, 0f);
            pos += owner.transform.position;

            if (projectilePool != null)
            {
                Projectile projectile = projectilePool.Pop();
                projectile.Attack(owner, startPos, pos);
                curProjectiles.Add(projectile);
            }
            else
            {
                CollideAttack(null, null);
            }

            double projectileCount = 0;
            if (skillData.skillTarget == 1)
                projectileCount = skillData.skillRangeOpt;
            else
                projectileCount = skillData.skillProjectileCount;

            float angle = UtilFunction.GetAngle(new Vector2(owner.transform.position.x, owner.transform.position.y), new Vector2(target.transform.position.x, target.transform.position.y));
            float degree = (float)(skillData.skillTypeOpt / (projectileCount - 1d));
            for (int i = 1; i < projectileCount; i++)
            {
                Projectile projectile = projectilePool.Pop();
                projectile.gameObject.SetActive(true);
                curProjectiles.Add(projectile);

                float multiply = (int)i/2 + 1f;
                if (i % 2 == 0)
                    multiply = -((int)i/2f);

                float projectileDegree = angle + (degree * multiply);
                float radian = projectileDegree * Mathf.Deg2Rad;
                float x = Mathf.Cos(radian);
                float y = Mathf.Sin(radian);

                Vector3 endPos = new Vector3(x * (float)skillData.skillRange, y * (float)skillData.skillRange, 0f);
                endPos += owner.transform.position;
                projectile.Attack(owner, startPos, endPos);
            }
        }
        else
            return false;

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        ShowCastEffect();
        UseMP();
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    protected void BaseCollideAttack(Projectile projectile, Monster target)
    {
        if (skillData.specialType.ContainsKey(2))
        {
            bool isPlaySkill = User.Inst.LocalRandom() < skillData.specialType[2].skillRate * 10000 ? true : false;
            if (isPlaySkill)
            {
                if (additionProjectilePool != null)
                {
                    double projectileCount = 0;
                    if (skillData.skillTarget == 1)
                        projectileCount = skillData.skillRangeOpt;
                    else
                        projectileCount = skillData.skillProjectileCount;

                    for (int i = 0; i < additionProjectileMaxCount; i++)
                    {
                        ArrowProjectile aProjectile = additionProjectilePool.Pop();
                        aProjectile.gameObject.SetActive(true);
                        float degree = 45f * i;
                        float radian = degree * Mathf.PI / 180;
                        float x = Mathf.Cos(radian);
                        float y = Mathf.Sin(radian);
                        Vector3 endPos = new Vector3(x * (float)projectileCount, y * (float)projectileCount, 0f);
                        endPos += target.transform.position;
                        aProjectile.Attack(owner, target.transform.position, endPos, target);
                        curAddProjectiles.Add((Projectile)aProjectile);
                    }
                }
            }
        }
        if (projectile != null && projectileInfo.IsPierce == 0)
        {
            if (curProjectiles.Contains(projectile))
                curProjectiles.Remove(projectile);
            ReleaseProjectile(projectile);
        }
        CollideAttack(projectile, target);
    }
    protected void AdditionCollideAttack(Projectile projectile, Monster target)
    {
        EndSkillAttackAddition(projectile);
        if (target != null)
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


        CalculateDamage(target, skillData.specialType[2].specialOpt);
        if (target != null && (target.ActionCtrl.State != CHAR_ACTION_STATE.DIE))
        {
            target.ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
        }

        if (target != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
        {
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, target.Body.position, owner.transform.localScale.x > 0 ? true : false);
        }

    }

    public override void CollideAttack(Projectile projectile, Monster target)
    {
        List<Monster> targets = GetDamageTarget(target);
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
            if (skillTable.TargetEffectType == 2)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, mon.transform.position, owner.transform.localScale.x > 0 ? true : false);
            }

            ExplosionDamage(mon.transform.position);
        }

    }

    public void EndSkillAttackAddition(Projectile projectile)
    {
        if (projectile != null)
        {
            if (curAddProjectiles.Contains(projectile))
                curAddProjectiles.Remove(projectile);

            projectile.gameObject.SetActive(false);
            additionProjectilePool.Push((ArrowProjectile)projectile);
        }
    }

    public override void ClearSkill()
    {
        base.ClearSkill();
        for (int i = 0; i < curAddProjectiles.Count; i++)
            ReleaseProjectile(curAddProjectiles[i]);
        curAddProjectiles.Clear();
    }
}
