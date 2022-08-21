using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RotationLightning : Skill
{
    protected int projectileId = 0;
    protected TBL.Sheet.CProjectile projectileInfo = default(TBL.Sheet.CProjectile);
    protected ObjectPool<Projectile> projectilePool = null;
    private List<Projectile> curProjectiles = new List<Projectile>();
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
        if (projectilePool != null)
        {
            Projectile projectile = projectilePool.Pop();

            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            Transform ownerSlotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
            Vector3 startPos = ownerSlotTrans != null ? ownerSlotTrans.position : owner.transform.position;

            float time = 0f;
            ParticleSystem[] arrPs = projectile.gameObject.GetComponentsInChildren<ParticleSystem>();
            if (arrPs.Length > 0)
            {
                var list = arrPs.ToList();
                list.Sort((a, b) => (b.main.duration + b.main.startDelay.constant).CompareTo(a.main.duration + a.main.startDelay.constant));
                time = list[0].main.duration + list[0].main.startDelay.constant;
            }
            projectile.Attack(owner, startPos, Vector3.zero, time);
            curProjectiles.Add(projectile);
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
    
    public void EndSkillAttack(Projectile projectile)
    {
        if (curProjectiles.Contains(projectile))
            curProjectiles.Remove(projectile);
        if (projectile != null)
            ReleaseProjectile(projectile);
    }
    public override void ResetSkill()
    {
        base.ResetSkill();
        for(int i = 0; i < curProjectiles.Count; i++)
            ReleaseProjectile(curProjectiles[i]);
        curProjectiles.Clear();
    }
}
