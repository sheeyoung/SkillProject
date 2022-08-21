using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CommonAttack : Skill
{
    protected int projectileId = 0;
    protected TBL.Sheet.CProjectile projectileInfo = default(TBL.Sheet.CProjectile);
    protected ObjectPool<Projectile> projectilePool = null;
    protected float[] angles = { 20f,-30f, 50f, -60f, 90f, -110f};
    protected List<Projectile> curProjectiles = new List<Projectile>();
    Coroutine skillCorutine;


    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);

        projectileId = 0;
        skillCorutine = null;
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
                if(projectileInfo.Type == 3)
                {
                    projectile = obj.AddComponent<ParabolaProjectile>();
                }
                else if (projectileInfo.Type == 1)
                {
                    projectile = obj.AddComponent<DirectFiring>();
                }
                else if(projectileInfo.Type == 2)
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

        if (skillData.skillTarget == 3) //자기주변광역
        {
            if (targets.Count == 0)
                return false;
            
            CollideAttack(null, null);
        }
        else if (skillData.skillTarget == 2)//대상 방향
        {
            Monster target = targets.Count > 0 ? targets[0] : null;
            if (target != null)
            {
                Vector3 endPos = new Vector2(target.transform.position.x, target.transform.position.y) - new Vector2(owner.transform.position.x, owner.transform.position.y);
                endPos.Normalize();
                decimal skillRange = (decimal)skillData.skillRange;
                float fSkillRange = (float)skillRange;
                endPos = new Vector3(endPos.x * fSkillRange, endPos.y * fSkillRange, 0f);
                endPos += owner.transform.position;
                if (projectilePool != null)
                {
                    if (projectileInfo.Type == 3)
                    {
                        int angleCount = 0;

                        skillCorutine = BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                        {
                            double degree = 0d;
                            if (angleCount >= angles.Length)
                                angleCount = 0;
                            degree = angles[angleCount++];
                            ParabolaProjectile projectile = (ParabolaProjectile)projectilePool.Pop();
                            projectile.SetDegree((float)degree);
                            projectile.AttackAngle(owner, startPos, endPos, (float)degree);
                            curProjectiles.Add((Projectile)projectile);
                        });
                    }
                    else
                    {
                        skillCorutine = BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                        {
                            Projectile projectile = projectilePool.Pop();
                            projectile.Attack(owner, startPos, endPos);
                            curProjectiles.Add(projectile);
                        });
                    }
                }
                else
                {
                    CollideAttack(null, null);
                }
            }
        }
        else if (skillData.skillTarget == 4)//대상 주변 광역
        {
            if (targets.Count == 0)
                return false;
            Monster target = targets[0];
            if (target != null)
            {
                Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
                Vector3 endPos = slotTrans != null ? slotTrans.position : target.transform.position;

                if (projectilePool != null)
                {
                    if (projectileInfo.Type == 3)
                    {
                        int angleCount = 0;

                        skillCorutine = BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                        {
                            double degree = 0d;
                            if (angleCount >= angles.Length)
                                angleCount = 0;
                            degree = angles[angleCount++];
                            ParabolaProjectile projectile = (ParabolaProjectile)projectilePool.Pop();
                            projectile.SetDegree((float)degree);
                            projectile.AttackAngle(owner, startPos, endPos, (float)degree);
                            curProjectiles.Add(projectile);
                        });
                    }
                    else
                    {
                        skillCorutine = BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                        {
                            Projectile projectile = projectilePool.Pop();
                            projectile.Attack(owner, startPos, endPos);
                            curProjectiles.Add(projectile);
                        });
                    }
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
                
                if (target != null)
                {
                    Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
                    Vector3 endPos = slotTrans != null ? slotTrans.position : target.transform.position;

                    if (projectilePool != null)
                    {
                        if (projectileInfo.Type == 3)
                        {
                            int angleCount = 0;

                            skillCorutine = BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                            {
                                double degree = 0d;
                                if (angleCount >= angles.Length)
                                    angleCount = 0;
                                degree = angles[angleCount++];

                                ParabolaProjectile projectile = (ParabolaProjectile)projectilePool.Pop();
                                projectile.SetDegree((float)degree);
                                projectile.AttackAngle(owner, startPos, endPos, (float)degree);
                                curProjectiles.Add(projectile);
                            });
                        }
                        else
                        {
                            skillCorutine = BattleScene.Inst.BattleMode.RequestProjectileCount(skillData.skillProjectileCount, (int i) =>
                            {
                                Projectile projectile = projectilePool.Pop();
                                projectile.Attack(owner, startPos, endPos);
                                curProjectiles.Add(projectile);
                            });
                        }
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

            //폭발
            ExplosionDamage(mon.transform.position);
        }
    }
    public virtual void EndSkillAttack(Projectile projectile)
    {
        if (projectile != null)
        {
            if (curProjectiles.Contains(projectile))
                curProjectiles.Remove(projectile);
            skillCorutine = null;
            ReleaseProjectile(projectile);
        }
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        if (skillCorutine != null)
            BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutine);
        for (int i = 0; i < curProjectiles.Count; i++)
            ReleaseProjectile(curProjectiles[i]);
        curProjectiles.Clear();
    }
}
