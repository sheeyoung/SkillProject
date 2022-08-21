using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingThunder : CommonAttack
{
    protected override List<Monster> FindTarget()
    {
        List<Monster> targets = new List<Monster>();
        if (owner.holePrevTargets != null && owner.holePrevTargets.Count > 0)
        {
            for(int i = 0; i < owner.holePrevTargets.Count; i++)
            {
                if(owner.holePrevTargets[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE)
                {
                    targets.Add(owner.holePrevTargets[i]);
                }
            }
        }
        if (targets.Count > 0)
            return targets;
        
        return base.FindTarget();
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

        if (skillId == 3100)
        {
            owner.fallingThunderTarget.Clear();
            owner.fallingThunderTarget.AddRange(targets);
        }
        else
        {
            List<Monster> remainMons = new List<Monster>();
            for (int i = 0; i < targets.Count; i++)
            {
                if (owner.fallingThunderTarget.Contains(targets[i]))
                {
                    if(targets[i].ActionCtrl.State != CHAR_ACTION_STATE.DIE)
                        remainMons.Add(targets[i]);
                }
            }
            owner.fallingThunderTarget.Clear();
            owner.fallingThunderTarget.AddRange(remainMons);
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
    public override void OnDebuff(Monster target, double dam, System.Numerics.BigInteger bigDam)
    {
        if (skillData.debuffPassives.Count <= 0)
            return;
        for (int i = 0; i < skillData.debuffPassives.Count; i++)
        {
            if (skillData.debuffPassives[i] == null)
                continue;
            SKILL_SPECIAL_TYPE skillSpecialType = (SKILL_SPECIAL_TYPE)skillData.debuffPassives[i].specialType;
            switch (skillSpecialType)
            {
                
                case SKILL_SPECIAL_TYPE.DEBUFF_OVERLOAD:
                    {
                        {
                            if (owner.fallingThunderTarget.Contains(target) && target.ActionCtrl.State != CHAR_ACTION_STATE.DIE)
                            {
                                base.OnDebuff(target, dam, bigDam);
                            }
                        }
                    }
                    break;
                default:
                    base.OnDebuff(target, dam, bigDam);
                    break;
            }
        }
    }
}
