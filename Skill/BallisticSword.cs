using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallisticSword : Skill
{
    protected int projectileId = 0;
    protected TBL.Sheet.CProjectile projectileInfo = default(TBL.Sheet.CProjectile);
    protected ObjectPool<Projectile> projectilePool = null;
    protected ObjectPool<GameObject> prevEffPool = null;
    protected GameObject posObject;
    protected List<Transform> posItems;
    protected List<Projectile> curProjectiles = new List<Projectile>();
    protected List<GameObject> curEffs = new List<GameObject>();
    bool projectileCoroutine;
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
                projectile = obj.AddComponent<BallisticProjectile>();
                projectile.Init(projectileInfo, CollideAttack, EndSkillAttack);
                projectile.gameObject.SetActive(false);
                return projectile;
            });

            sb.Length = 0;
            sb.AppendFormat("{0}{1}", ResourcePath.Projectile, "eff_PC_skill_guided_sword_c");
            GameObject resObj2 = ResManager.Inst.Load<GameObject>(sb.ToString());
            prevEffPool = new ObjectPool<GameObject>(projectileCount, () =>
            {
                GameObject obj = GameObject.Instantiate(resObj2);
                obj.SetActive(false);
                return obj;
            });

            sb.Length = 0;
            sb.AppendFormat("{0}{1}", ResourcePath.Projectile, "eff_PC_skill_guided_sword_point");
            var resObj3 = ResManager.Inst.Load<GameObject>(sb.ToString());
            posObject = GameObject.Instantiate(resObj3);
           
            
            if (posItems == null) posItems = new List<Transform>();
            for(int i = 0; i < posObject.transform.childCount; i++)
            {
                posItems.Add(posObject.transform.GetChild(i));
            }

            posObject.gameObject.SetActive(false);

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
    public void ReleasePrevEff(GameObject prevEffObj)
    {
        if (prevEffObj && prevEffObj.activeSelf)
        {
            prevEffObj.SetActive(false);
            prevEffPool.Push(prevEffObj);
        }
    }


    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        List<Monster> targets = FindTarget();
        if (targets.Count == 0)
            return false;

        posObject.gameObject.SetActive(true);
        posObject.transform.position = new Vector3(targets[0].transform.position.x, targets[0].transform.position.y, 0);

        Transform slotTrans = UtilFunction.FindTransform("main", targets[0].transform);
        Vector3 endPos = slotTrans != null ? slotTrans.position : targets[0].transform.position;
        

        List<int> randomPosCount = new List<int>();
        for(int i = 0; i < skillData.skillProjectileCount; i++)
        {
            int rand = UnityEngine.Random.Range(0, posItems.Count);
            while(randomPosCount.Contains(rand))
            {
                rand = UnityEngine.Random.Range(0, posItems.Count);
            }
            randomPosCount.Add(rand);
        }

        projectileCoroutine = true;

        for (int i = 0; i < randomPosCount.Count; i++)
        {
            var posCount = randomPosCount[i];
            if (posItems.Count <= posCount)
                posCount = posItems.Count - 1;
            if (posCount < 0)
                posCount = 0;

            Vector3 startPos = new Vector3(posItems[posCount].position.x, posItems[posCount].position.y, endPos.z);
            if (i < 3)
                AttackStart();
            else
                BattleScene.Inst.BattleMode.RequestBallisticSwordSkill(0.1f * i, AttackStart);

            void AttackStart()
            {
                if (projectileCoroutine == false)
                {
                    return;
                }
                GameObject prevObj = prevEffPool.Pop();
                prevObj.transform.position = posItems[posCount].position;
                prevObj.SetActive(true);
                
                BattleScene.Inst.BattleMode.RequestBallisticSwordSkill(0.2f, PrevEnd);
                void PrevEnd()
                {
                    if(targets[0] == null)
                    {
                        ReleasePrevEff(prevObj);
                        if (curEffs.Contains(prevObj))
                            curEffs.Remove(prevObj);
                        return;
                    }
                    if (projectileCoroutine == false)
                    {
                        ReleasePrevEff(prevObj);
                        if (curEffs.Contains(prevObj))
                            curEffs.Remove(prevObj);
                        return;
                    }
                    Projectile projectile = projectilePool.Pop();
                    curProjectiles.Add(projectile);

                    Transform slotTrans = UtilFunction.FindTransform("main", targets[0].transform);
                    endPos = slotTrans != null ? slotTrans.position : targets[0].transform.position;
                    
                    float angle = UtilFunction.GetAngle(endPos, startPos);
                    projectile.transform.localEulerAngles = new Vector3(0, 0, angle);
                    projectile.gameObject.transform.position = startPos;
                    projectile.gameObject.SetActive(true);
                    BattleScene.Inst.BattleMode.RequestBallisticSwordSkill(1f, EndPrevEff);
                    BattleScene.Inst.BattleMode.RequestBallisticSwordSkill(0.1f, ProjectileStart);
                    void ProjectileStart()
                    {
                        if (projectileCoroutine == false)
                        {
                            if (curProjectiles.Contains(projectile))
                                curProjectiles.Remove(projectile);
                            ReleaseProjectile(projectile);
                            return;
                        }
                        Transform slotTrans = UtilFunction.FindTransform("main", targets[0].transform);
                        endPos = slotTrans != null ? slotTrans.position : targets[0].transform.position;
                        projectile.Attack(owner, startPos, endPos);
                    }
                    void EndPrevEff()
                    {
                        ReleasePrevEff(prevObj);
                        if (curEffs.Contains(prevObj))
                            curEffs.Remove(prevObj);
                    }

                }

            }
        }

        posObject.gameObject.SetActive(false);

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
    public void EndSkillAttack(Projectile projectile)
    {
        if (curProjectiles.Contains(projectile))
            curProjectiles.Remove(projectile);
        if (projectile != null)
            ReleaseProjectile(projectile);
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        projectileCoroutine = false;
        for (int i = 0; i < curProjectiles.Count; i++)
            ReleaseProjectile(curProjectiles[i]);
        for (int i = 0; i < curEffs.Count; i++)
            ReleasePrevEff(curEffs[i]);

        curEffs.Clear();
        curProjectiles.Clear();
    }
}
