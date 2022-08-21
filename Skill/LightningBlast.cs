using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightningBlast : Skill
{
    protected int projectileId = 0;
    protected TBL.Sheet.CProjectile projectileInfo = default(TBL.Sheet.CProjectile);
    protected ObjectPool<Projectile> projectilePool = null;

    protected ObjectPool<ArrowProjectile> additionProjectilePool = null;
    private const int additionProjectileMaxCount = 8;

    protected List<Projectile> curProjectiles = new List<Projectile>();
    protected List<Projectile> curAddProjectiles = new List<Projectile>();
    Coroutine skillCorutine;

    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);
        skillCorutine = null;
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
                    projectile = obj.AddComponent<DirectFiring>();
                else if (projectileInfo.Type == 2)
                    projectile = obj.AddComponent<Howitzer>();
                projectile.Init(projectileInfo, null, BaseAttackEnd);
                projectile.gameObject.SetActive(false);
                return projectile;
            });

            if (!User.Inst.TBL.Projectile.ContainsKey(User.Inst.TBL.Const.CONST_PC_ATK3_L3_P))
                return;

            sb.Length = 0;
            sb.AppendFormat("{0}{1}", ResourcePath.Projectile, User.Inst.TBL.Projectile[User.Inst.TBL.Const.CONST_PC_ATK3_L3_P].PrefabName);
            GameObject resAddObj = ResManager.Inst.Load<GameObject>(sb.ToString());
            additionProjectilePool = new ObjectPool<ArrowProjectile>(projectileCount, () =>
            {
                GameObject obj = GameObject.Instantiate(resAddObj);
                ArrowProjectile projectile = null;
                projectile = obj.AddComponent<ArrowProjectile>();
                projectile.Init(projectileInfo, AdditionCollideAttack, EndSkillAttackAddition);
                projectile.gameObject.SetActive(false);
                return projectile;
            });
        }
    }
    public void ReleaseProjectile(Projectile projectile)
    {
        if (projectile && projectile.gameObject.activeSelf && projectileId != 0)
        {
            projectile.gameObject.SetActive(false);
            projectilePool.Push(projectile);
        }
    }
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        List<Monster> targets = FindTarget();

        string castPosName = "main";
        if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
            castPosName = skillTable.CastEffectPosition;
        Transform ownerSlotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
        Vector3 startPos = ownerSlotTrans != null ? ownerSlotTrans.position : owner.transform.position;

        Monster centerTarget = targets.Count > 0 ? targets[0] : null;
        Vector3 pos = Vector3.zero;
        if (centerTarget != null)
        {
            pos = new Vector2(centerTarget.transform.position.x, centerTarget.transform.position.y) - new Vector2(owner.transform.position.x, owner.transform.position.y);
            pos.Normalize();
            decimal skillRange = (decimal)skillData.skillRange;
            float fSkillRange = (float)skillRange;
            pos = new Vector3(pos.x * fSkillRange, pos.y * fSkillRange, 0f);
            pos += owner.transform.position;
            if (projectilePool != null)
            {
                if (skillData.skillProjectileCount == 1)
                {
                    Projectile projectile = projectilePool.Pop();
                    projectile.Attack(owner, startPos, pos);
                    curProjectiles.Add(projectile);
                }
                else
                {
                    skillCorutine = BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, User.Inst.TBL.Const.CONST_SKILL_LE_DELAY, (int i) =>
                    {
                        Projectile projectile = projectilePool.Pop();
                        projectile.Attack(owner, startPos, pos);
                        curProjectiles.Add(projectile);
                    });
                }
            }
            else
            {
                CollideAttack(null, null);
            }
        }

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        ShowCastEffect();
        UseMP();
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);        
        return true;
    }
    protected void BaseAttackEnd(Projectile projectile)
    {
        Vector3 pos = projectile.transform.position;
        if (projectile != null)
        {
            if (curProjectiles.Contains(projectile))
                curProjectiles.Remove(projectile);
            ReleaseProjectile(projectile);
        }

        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(pos, 0, skillData.skillRangeOpt);
        if (monsterlist.Count > 0)
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

        for (int i = 0; i < monsterlist.Count; i++)
        {
            CalculateDamage(monsterlist[i], skillData.skillDamageRate);
            if (monsterlist[i] != null && (monsterlist[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE))
            {
                monsterlist[i].ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
            }

            if (monsterlist[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, monsterlist[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
            }
        }
        if (string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, pos, owner.transform.localScale.x > 0 ? true : false);

        if (additionProjectilePool != null)
        {
            for (int i = 0; i < additionProjectileMaxCount; i++)
            {
                ArrowProjectile aProjectile = additionProjectilePool.Pop();
                float degree = 45f * i;
                float radian = degree * Mathf.PI / 180;
                float x = Mathf.Cos(radian);
                float y = Mathf.Sin(radian);
                Vector3 endPos = new Vector3(x * (float)skillData.skillRange, y * (float)skillData.skillRange, 0f);
                endPos += pos;
                aProjectile.Attack(owner, projectile.transform.position, endPos, null);
                curAddProjectiles.Add(aProjectile);
            }
        }
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

        CalculateDamage(target, skillData.skillDamageRate);
        if (target != null && (target.ActionCtrl.State != CHAR_ACTION_STATE.DIE))
        {
            target.ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
        }

        EffectManager.Inst.ShowEffect(User.Inst.TBL.Const.CONST_PC_ATK3_L3_T, target.Body.position, owner.transform.localScale.x > 0 ? true : false);

    }
   
    public void EndSkillAttackAddition(Projectile projectile)
    {
        if (projectile != null)
        {
            projectile.gameObject.SetActive(false);
            if (curAddProjectiles.Contains(projectile))
                curAddProjectiles.Remove(projectile);
            additionProjectilePool.Push((ArrowProjectile)projectile);
        }
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        if (skillCorutine != null)
            BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutine);
        for (int i = 0; i < curProjectiles.Count; i++)
            ReleaseProjectile(curProjectiles[i]);
        for (int i = 0; i < curAddProjectiles.Count; i++)
            ReleaseProjectile(curAddProjectiles[i]);
        curProjectiles.Clear();
        curAddProjectiles.Clear();
    }
}
