using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
public class AreaDeclare : Skill
{
    private GameObject effObject;
    private Transform effObjectParent;
    Coroutine skillCorutine;
    public override bool SkillAttack(double linkRate)
    {
        this.linkRate = linkRate;
        if(effObject == null)
        {
            effObject = EffectManager.Inst.GetEffectObject(skillTable.CastEffect);
            string castPosName = "main";
            if (string.IsNullOrEmpty(skillTable.CastEffectPosition) == false)
                castPosName = skillTable.CastEffectPosition;
            effObjectParent = UtilFunction.FindTransform(castPosName, owner.transform);
            effObject.transform.position = effObjectParent.position;
        }
        if (effObject)
        {
            effObject.gameObject.SetActive(true);
            effObject.transform.localScale = UnityEngine.Vector3.one * (float)skillData.skillRangeOpt;
        }
        tickTime = 0;
        if(tableTickTime == 0)
        {
            var debuffTableInfo = User.Inst.TBL.Debuff[20];
            tableTickTime = debuffTableInfo.Time;
        }
        skillCorutine = BattleScene.Inst.BattleMode.RequestAreaDeclare(UpdateSkill, EndSkill);

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);

        // 퀘스트 업데이트
        if (owner.GuideQuestMgr != null && !(skillData.skillGroup == 1 && (skillData.skillsort == 1 || skillData.skillsort == 3)))
            owner.GuideQuestMgr.UpdateGuideQuest(Doc.Api.GUIDE_QUEST_TYPE.SKILL_USE, skillId, 1);
        return true;
    }
    float tickTime = 0;
    float tableTickTime =0;
    private void UpdateSkill()
    {
        tickTime -= Time.deltaTime;
        effObject.transform.position = effObjectParent.position;
        if (BattleScene.Inst.BattleMode.Pc.RageMgr.IsRageMode == false)
        {
            EndSkill();
            return;
        }

        if (tickTime <= 0)
        {
            SetDebuff();
            tickTime = tableTickTime;
        }

        if (effObject)
        {
            if(effObject.activeSelf == false)
                effObject.gameObject.SetActive(true);
        }
    }
    private void SetDebuff()
    {
        List<Monster> targets = new List<Monster>();
        var monsterlist = NowMode.FindMonsterListByMonsterPosCircleRange(owner.transform.position, 0, skillData.skillRangeOpt);
        for (int i = 0; i < monsterlist.Count; i++)
            targets.Add(monsterlist[i]);
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i].ActionCtrl.State == CHAR_ACTION_STATE.DIE)
                continue;
            OnDebuff(targets[i], 0, 0);
        }
    }
    private void EndSkill()
    {
        if (effObject) effObject.gameObject.SetActive(false);
    }

    

    public override void ResetSkill()
    {
        base.ResetSkill();
        if (effObject) effObject.gameObject.SetActive(false);
        skillCorutine = null;
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        if (skillCorutine != null) BattleScene.Inst.BattleMode.EndSkillCorutine(skillCorutine);
        if (effObject) effObject.gameObject.SetActive(false);
    }
}
