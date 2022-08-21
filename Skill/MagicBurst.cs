using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MagicBurst : Skill
{
    protected int projectileId = 0;
    protected TBL.Sheet.CProjectile projectileInfo = default(TBL.Sheet.CProjectile);
    protected ObjectPool<Projectile> projectilePool = null;
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
                if (projectileInfo.Type == 3)
                {
                    projectile = obj.AddComponent<ParabolaProjectile>();
                }
                else if (projectileInfo.Type == 1)
                {
                    projectile = obj.AddComponent<DirectFiring>();
                }
                else if (projectileInfo.Type == 2)
                {
                    projectile = obj.AddComponent<Howitzer>();
                }
                projectile.Init(projectileInfo, CollideAttack, EndSkillAttack);
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

        
        float angle = 360f / (float)skillData.skillProjectileCount;
        for (int i = 0; i < skillData.skillProjectileCount; i++)
        {
            Projectile aProjectile = projectilePool.Pop();
            float degree = angle * i;
            float radian = degree * Mathf.PI / 180;
            float x = Mathf.Cos(radian);
            float y = Mathf.Sin(radian);
            Vector3 endPos = new Vector3(x * (float)skillData.skillRange, y * (float)skillData.skillRange, 0f);
            endPos += startPos;
            aProjectile.Attack(owner, startPos, endPos);
        }

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        ShowCastEffect();
        UseMP();
        
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    public override void CollideAttack(Projectile projectile, Monster target)
    {
        if (projectile != null && projectileInfo.IsPierce == 0)
            ReleaseProjectile(projectile);

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

            //����ȭ
            ExplosionDamage(mon.transform.position);
        }
    }
    public void EndSkillAttack(Projectile projectile)
    {
        if (projectile != null)
            ReleaseProjectile(projectile);
    }
}
