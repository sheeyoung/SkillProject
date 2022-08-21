using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShockWall : Skill
{
    protected int projectileId = 0;
    protected TBL.Sheet.CProjectile projectileInfo = default(TBL.Sheet.CProjectile);
    protected WallProjectile projectile = null;

    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);

        projectileId = 0;

        sb.Length = 0;
        sb.AppendFormat("{0}{1}", ResourcePath.Projectile, "eff_PC_skill01_L3_c");
        GameObject resObj = ResManager.Inst.Load<GameObject>(sb.ToString());
        GameObject obj = GameObject.Instantiate(resObj);
        projectile = obj.AddComponent<WallProjectile>();
        projectile.Init(projectileInfo, null, EndSkillAttack);
        projectile.gameObject.SetActive(false);
    }
    public void ReleaseProjectile(Projectile projectile)
    {
        if (projectile && projectile.gameObject.activeSelf)
        {
            projectile.gameObject.SetActive(false);
        }
    }


    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;

        projectile.gameObject.SetActive(true);
        float range = (float)skillData.skillRangeOpt / 2f;
        projectile.Attack(owner, (float)skillData.skillTypeOpt, AttackRange);
        projectile.transform.localScale = new Vector3(range, range, range);

        UseMP();
        // Äù½ºÆ® ¾÷µ¥ÀÌÆ®
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    public void AttackRange()
    {
        List<Monster> targets = new List<Monster>();
        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0, skillData.skillRangeOpt);
        for (int i = 0; i < monsterlist.Count; i++)
            targets.Add(monsterlist[i]);

        var mon = targets.Count > 0 ? targets[0] : null;

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

            //Æø¹ßÈ­
            ExplosionDamage(mon.transform.position);
        }
    }
    
    public void EndSkillAttack(Projectile projectile)
    {
        if (projectile != null)
            ReleaseProjectile(projectile);
    }
}
