using System.Collections;
using UnityEngine;

public class ChaseSkill : MonoBehaviour, ISkill
{
    public string SkillName => throw new System.NotImplementedException();

    [Header("Chase Settings")]
    public float m_Speed = 1.5f;           // 이동 스케일(입력 축에 곱해질 값)
    public float m_ChaseTime = 2.0f;       // 추적 지속 시간
    public float m_FollowSharpness = 8f;   // 1~12 권장. 클수록 더 민감(덜 완만)

    public bool Execute(Transform owner)
    {
        var pm = owner.GetComponent<PlayerMovement>();
        pm.ActiveChaseMode(m_Speed, m_ChaseTime, m_FollowSharpness);
        return true;
    }
}
