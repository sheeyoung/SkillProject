using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThunderDrill : CommonAttack
{
    public override void CollideAttack(Projectile projectile, Monster target)
    {
        if (projectile != null && projectileInfo.IsPierce == 0)
        {
            if (curProjectiles.Contains(projectile))
                curProjectiles.Remove(projectile);
            ReleaseProjectile(projectile);
        }

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

        }
    }
    public override void EndSkillAttack(Projectile projectile)
    {
        ExplosionDamage(projectile.transform.position);
        //폭발 이펙트
        EffectManager.Inst.ShowEffect("eff_PC_skill02_p2_die", projectile.transform.position, false);

        if (projectile != null)
        {
            if (curProjectiles.Contains(projectile))
                curProjectiles.Remove(projectile);
            ReleaseProjectile(projectile);
        }
    }
    #region 썬더드릴(예전)
    /*
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
                projectile = obj.AddComponent<ThunderDrillProjectile>();
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


    public override bool SkillAttack()
    {
        List<Monster> targets = FindTarget();
        bool isTarget = false;
        for (int i = 0; i < 1; i++)// skillData.skillRangeOpt; i++)
        {
            Monster target = targets.Count > i ? targets[i] : null;
            Vector3 pos = Vector3.zero;
            if (target != null)
            {
                string castPosName = "main";
                if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                    castPosName = skillTable.CastEffectPosition;
                Transform ownerSlotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
                Vector3 startPos = ownerSlotTrans != null ? ownerSlotTrans.position : owner.transform.position;

                pos = new Vector2(target.transform.position.x, target.transform.position.y)  - new Vector2(owner.transform.position.x, owner.transform.position.y);
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
                    }
                    else
                    {
                        BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                        {
                            Projectile projectile = projectilePool.Pop();
                            projectile.Attack(owner, startPos, pos);
                        });
                    }
                }
                else
                {
                    CollideAttack(null, null);
                }
                isTarget = true;
            }
        }
        if (isTarget == false)
            return false;

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        ShowCastEffect();
        UseMP();
        // ����Ʈ ������Ʈ
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    public override void CollideAttack(Projectile projectile, Monster target)
    {
    }
    public void EndSkillAttack(Projectile projectile)
    { 
        var endPos = projectile.transform.position;
        if (projectile != null)
            ReleaseProjectile(projectile);

        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(endPos, 0, skillData.skillExplotionRange);
        if (monsterlist.Count > 0)
        {
            // ���� ������ ����
            owner.RageMgr.UpdateRageGauge(User.Inst.TBL.Const.CONST_RAGE_VALUE_ATK);            
        }


        if (skillTable.TargetEffectType == 2)
        {
            EffectManager.Inst.ShowEffect(skillTable.TargetEffect, endPos, owner.transform.localScale.x > 0 ? true : false);
        }

        //����ȭ
        ExplosionDamage(endPos);
    }
    */
    #endregion
}
