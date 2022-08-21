using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Avatar : Skill
{
    protected ObjectPool<PCAvatar> avatarPool = null;
    protected List<PCAvatar> curAvatars = new List<PCAvatar>();
    
    public override bool SkillAttack(double linkRate)
    {
        if(avatarPool == null)
        {
            avatarPool = new ObjectPool<PCAvatar>(1, () =>
            {
                GameObject mObj = ResManager.Inst.Load<GameObject>("PC/EMT_PC");
                GameObject objPC = GameObject.Instantiate(mObj, owner.transform.position, Quaternion.identity);
                var avatar = objPC.AddComponent<PCAvatar>();
                avatar.InitCharacter();
                avatar.gameObject.SetActive(false);
                return avatar;
            });
        }

        if(avatarPool != null)
        {
            PCAvatar avatar = avatarPool.Pop();
            avatar.gameObject.SetActive(true);
            avatar.StartAvatar(skillData.skillTypeOpt, EndSkill);
            EffectManager.Inst.ShowEffect("eff_PC_avatar_start", avatar.transform.position, false);
            curAvatars.Add(avatar);
        }

        if (string.IsNullOrEmpty(skillTable.SkillSound) == false)
            SoundManager.Inst.PlayEffect(skillTable.SkillSound);
        return true;
    }
    public void EndSkill(PCAvatar avatar)
    {
        if (avatar == null)
        {
            for (int i = 0; i < curAvatars.Count; i++)
            {
                curAvatars[i].gameObject.SetActive(false);
                EffectManager.Inst.ShowEffect("eff_PC_avatar_die", curAvatars[i].transform.position, false);
                avatarPool.Push(curAvatars[i]);
            }
            curAvatars.Clear();
            return;
        }
        EffectManager.Inst.ShowEffect("eff_PC_avatar_die", avatar.transform.position, false);
        avatar.gameObject.SetActive(false);
        curAvatars.Remove(avatar);
        avatarPool.Push(avatar);
    }
    public void SetAvatarRageMode()
    {
        for(int i = 0; i< curAvatars.Count; i++)
        {
            curAvatars[i].SetAvatarRageMode();
        }
    }
    public override void ClearSkill()
    {
        base.ClearSkill();
        EndSkill(null);
    }

}
