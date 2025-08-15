using UnityEngine;

public interface ISkill
{
    // owner: 스킬을 시전하는 주체(플레이어/보스)
    // returns true if executed successfully
    bool Execute(Transform owner);
    string SkillName { get; }
}
