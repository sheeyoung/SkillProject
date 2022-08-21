using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Spine;
using Spine.Unity;
public class RandomAttack : Skill
{
    Coroutine skillCorutine;
    List<float> aniEventTime;
    protected Dictionary<CHAR_ACTION_STATE, Dictionary<int, List<AnimEventInfo>>> AnimEventTimes = new Dictionary<CHAR_ACTION_STATE, Dictionary<int, List<AnimEventInfo>>>();
    public override void InitSkill(PC o, int skillID)
    {
        base.InitSkill(o, skillID);

    }


    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        skillCorutine = BattleScene.Inst.BattleMode.RequestRandomAttack(PlayAttackSkill, User.Inst.TBL.Const.CONST_THUNDERSTORM_DELAY, (float)skillData.skillTypeOpt);

        UseMP();
        // ����Ʈ ������Ʈ
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3))) 
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
   
    public void PlayAttackSkill()
    {
        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0f, skillData.skillRange);
        var targets = ((PCActionController)owner.ActionCtrl).FindTargetByDist(monsterlist, true, false);

        if (targets.Count == 0)
            return;

        Monster target = targets.Count > 0 ? targets[0] : null;
        Vector3 pos = Vector3.zero;
        Transform slotTrans = UtilFunction.FindTransform("main", target.transform);
        pos = slotTrans != null ? slotTrans.position : target.transform.position;

        var effect = EffectManager.Inst.GetEffectObject(skillTable.CastEffect);
        if (effect == null)
            return;
        effect.transform.position = pos;

        float totalTime = EffectManager.Inst.GetEffectDurationTime(skillTable.CastEffect);
        
        //release
        EffectManager.Inst.ReleaseEffect(skillTable.CastEffect, effect, totalTime);

        SkeletonAnimation AnimInfo = effect.GetComponent<SkeletonAnimation>();
        if (aniEventTime == null)
        {
            aniEventTime = new List<float>();
            
            var ani = AnimInfo.skeleton.Data.FindAnimation("attack");
            if (ani != null)
            {
                foreach (var timeline in ani.Timelines)
                {
                    EventTimeline eventTimeline = timeline as Spine.EventTimeline;
                    if (eventTimeline != null)
                    {
                        foreach (var spineEvent in eventTimeline.Events)
                        {
                            aniEventTime.Add(spineEvent.Time);
                            
                        }
                    }
                }
            }
        }
        AnimInfo.state.SetAnimation(0, "attack", false);
        BattleScene.Inst.BattleMode.RequestRandomAttack2(Hit, pos, aniEventTime);
    }

    public void Hit(Vector2 position)
    {
        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(position, 0f, skillData.skillRangeOpt);
        var targets = ((PCActionController)owner.ActionCtrl).FindTargetByDist(monsterlist, true, false);

        for(int i = 0; i < targets.Count; i++)
        {
            CollideAttack(null, targets[i]);
        }
    }




    public override void CollideAttack(Projectile projectile, Monster target)
    {
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

            CalculateDamage(target, skillData.skillDamageRate);
            if (target.ActionCtrl.State != CHAR_ACTION_STATE.DIE)
            {
                target.ActionCtrl.State = CHAR_ACTION_STATE.DAMAGED;
            }

            if (skillTable.TargetEffectType == 1 && string.IsNullOrEmpty(skillTable.TargetEffect) == false)
            {
                EffectManager.Inst.ShowEffect(skillTable.TargetEffect, target.Body.position, owner.transform.localScale.x > 0 ? true : false);
            }
            ExplosionDamage(target.transform.position);
        }
    }

    public override void ClearSkill()
    {
        base.ClearSkill();
        if (skillCorutine != null)
            BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutine);
    }

}
