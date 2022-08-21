using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Net.Impl;
public class BurnUpAtk : Skill
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

            sb.Length = 0;
            sb.AppendFormat("{0}{1}", ResourcePath.Projectile, projectileInfo.PrefabName);
            GameObject resObj = ResManager.Inst.Load<GameObject>(sb.ToString());
            projectilePool = new ObjectPool<Projectile>(1, () =>
            {
                GameObject obj = GameObject.Instantiate(resObj);
                Projectile projectile = null;
                if (projectileInfo.Type == 1)
                    projectile = obj.AddComponent<DirectFiring>();
                else if (projectileInfo.Type == 2)
                    projectile = obj.AddComponent<Howitzer>();
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
        if (skillData.skillTarget == 3)
        {
            if (targets.Count == 0)
                return false;
            CollideAttack(null, null);
        }
        else if (skillData.skillTarget == 2)
        {
            Monster target = targets.Count > 0 ? targets[0] : null;
            Vector3 pos = Vector3.zero;
            if (target != null)
            {
                pos = new Vector2(target.transform.position.x, target.transform.position.y) - new Vector2(owner.transform.position.x, owner.transform.position.y);
                pos.Normalize();
                decimal skillRange = (decimal)skillData.skillRange;
                float fSkillRange = (float)skillRange;
                pos = new Vector3(pos.x * fSkillRange, pos.y * fSkillRange, 0f);
                pos += owner.transform.position;

                string castPosName = "main";
                if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                    castPosName = skillTable.CastEffectPosition;
                Transform slotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
                Vector3 start = slotTrans != null ? slotTrans.position : owner.transform.position;
                if (projectilePool != null)
                {
                    BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                    {
                        Projectile projectile = projectilePool.Pop();
                        projectile.Attack(owner, start, pos);
                    });
                }
                else
                {
                    CollideAttack(null, null);
                }
            }
        }
        else if (skillData.skillTarget == 4)
        {
            if (targets.Count == 0)
                return false;
            Monster target = targets[0];
            if (target != null)
            {
                string castPosName = "main";
                if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                    castPosName = skillTable.CastEffectPosition;
                Transform ownerSlotTrans = UtilFunction.FindTransform(castPosName, owner.transform);
                Vector3 startPos = ownerSlotTrans != null ? ownerSlotTrans.position : owner.transform.position;

                Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
                Vector3 endPos = slotTrans != null ? slotTrans.position : target.transform.position;

                if (projectilePool != null)
                {
                    BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                    {
                        Projectile projectile = projectilePool.Pop();
                        projectile.Attack(owner, startPos, endPos);
                    });
                }
                else
                {
                    CollideAttack(null, target);
                }
            }
        }
        else
        {
            bool isTarget = false;
            for (int i = 0; i < skillData.skillRangeOpt; i++)
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

                    Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
                    Vector3 endPos = slotTrans != null ? slotTrans.position : target.transform.position;

                    if (projectilePool != null)
                    {
                        BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                        {
                            Projectile projectile = projectilePool.Pop();
                            projectile.Attack(owner, startPos, endPos);
                        });
                    }
                    else
                    {
                        CollideAttack(null, target);
                    }
                    isTarget = true;
                }
            }
            if (isTarget == false)
                return false;
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

        List<Monster> targets = GetDamageTarget();
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
        
        string str_maxHp = owner.MaxHP.ToString();
        double maxHp = System.Convert.ToDouble(str_maxHp);
        double maxNeedHp = maxHp * skillData.needHp;
        double burnUpAtkValue = maxNeedHp * skillData.skillTypeOpt;

        var specialOpt = skillData.GetSpecialType(11);
        if (specialOpt > 0f)
        {
            var burnHpValue = maxNeedHp * specialOpt;
            if (burnHpValue < 0)
                burnHpValue = 0;
            maxNeedHp = burnHpValue;
        }

        owner.BurnUpAtkReduceHP(maxNeedHp);

        for (int i = 0; i < targets.Count; i++)
        {
            CalculateDamage(targets[i], skillData.skillDamageRate, true, burnUpAtkValue);
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
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, target.transform.position, owner.transform.localScale.x > 0 ? true : false);
            }
            
            targets.Clear();
            if (skillData.skillExplotionRange > 0)
            {
                var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(target.transform.position, 0, skillData.skillExplotionRange);
                for (int i = 0; i < monsterlist.Count; i++)
                    targets.Add(monsterlist[i]);

                for (int i = 0; i < targets.Count; i++)
                {
                    CalculateDamage(targets[i], skillData.explosionDamageRate, true, burnUpAtkValue);
                    if (targets[i] != null && (targets[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE))
                    {
                        targets[i].ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
                    }

                    if (targets[i] != null && skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
                    {
                        EffectManager.Inst.ShowEffect(skillTable.TargetEffect, targets[i].Body.position, owner.transform.localScale.x > 0 ? true : false);
                    }
                }
            }
        }
    }
    public void EndSkillAttack(Projectile projectile)
    {
        if (projectile != null)
            ReleaseProjectile(projectile);

    }
    
}

