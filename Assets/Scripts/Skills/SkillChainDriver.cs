// encoding: UTF-8 (65001)
using System;
using System.Collections;
using UnityEngine;

public interface IChainOwnerSettable
{
    void SetOwner(Transform owner);
}

public class SkillChainDriver : MonoBehaviour, ISkillChain, IChainOwnerSettable
{
    public enum SkillType
    {
        None, Attack, Util
    }

    public SkillType m_Type;

    [Header("- Chain Levels")]
    [SerializeField] private SkillStageData[] m_Stages;

    [Header("- Global CoolDown / Chain CoolDown")]
    [SerializeField] private float m_GlobalCooldown = 0f; // 모든 단계 공용 대기
    [SerializeField] private float m_ChainCooldown = 0f;  // 콤보 전체 쿨다운(마지막 단계 후)

    [Header("- UIBinderAction")]
    [SerializeField] private Action<float, bool, int, int> m_ExecuteAct = null;

    [Header("- Option / Debug")]
    [SerializeField] private bool m_EnableDebug = false;

    // 내부 상태
    private int m_CurrentStage = -1;       // -1: Idle
    private float m_NextUsableTime = 0f;   // 체인 기준 사용 가능 시각(GCD/체인쿨/락아웃 반영)
    private float m_ComboWindowEnd = 0f;   // 콤보 입력 타임아웃 경계
    private float m_LockoutEnd = 0f;       // 후딜 종료 시각
    private bool m_Frozen = false;         // 강탈/동결

    private Transform m_Owner; // 실행 주체(슬롯 보유자). 필요 시 외부 SetOwner로 교체 가능

    public void SetUIBinder(Action<float, bool, int, int> _act)
    {
        m_ExecuteAct = _act;
    }

    public void SetOwner(Transform owner)
    {
        m_Owner = owner;
        StartCoroutine(CorWaitForExecuteAct());
    }

    IEnumerator CorWaitForExecuteAct()
    {
        yield return new WaitUntil(() => m_ExecuteAct != null);
        m_ExecuteAct?.Invoke(-2, true, m_Stages.Length, -1);
    }

    float now = 0;
    public bool TryUse()
    {
        if (m_Frozen) { if (m_EnableDebug) Debug.Log("[SkillChain] Frozen"); return false; }

        now = Time.time;
        if (now < m_NextUsableTime) { if (m_EnableDebug) Debug.Log("[SkillChain] GCD/Chain CD/Lockout"); return false; }

        // Idle → Stage0, 진행 중이면 창구 내에서만 전진
        if (m_CurrentStage < 0)
        {
            return ExecuteStage(0, now);
        }
        else
        {
            if (now <= m_ComboWindowEnd)
            {
                int next = Mathf.Min(m_CurrentStage + 1, (m_Stages?.Length ?? 1) - 1);
                return ExecuteStage(next, now);
            }
            else
            {
                ResetChainInternal();
                return ExecuteStage(0, now);
            }
        }
    }

    private bool ExecuteStage(int stageIndex, float now)
    {
        if (m_Stages == null || m_Stages.Length == 0) return false;
        if (stageIndex < 0 || stageIndex >= m_Stages.Length) return false;

        var data = m_Stages[stageIndex];
        var skill = data?.GetSkill();
        if (skill == null) { if (m_EnableDebug) Debug.LogWarning($"[SkillChain] Stage {stageIndex} has no ISkill."); return false; }

        // 실행
        bool ok = skill.Execute(m_Owner);
        if (!ok) return false;

        // 상태 갱신
        m_CurrentStage = stageIndex;

        // 락아웃(후딜)과 콤보 타임아웃
        m_LockoutEnd = now + Mathf.Max(0f, data.m_Recovery);
        m_ComboWindowEnd = now + Mathf.Max(0f, data.m_ComboTimeout);

        // 개별/공용 쿨다운 반영
        float indiv = Mathf.Max(0f, data.m_StageCooldown);
        float gcd = Mathf.Max(0f, m_GlobalCooldown);
        m_NextUsableTime = Mathf.Max(m_LockoutEnd, now + Mathf.Max(indiv, gcd));

        // 마지막 단계면 체인 쿨다운 후 리셋
        bool last = (stageIndex >= m_Stages.Length - 1);
        if (last)
        {
            if (m_ChainCooldown > 0f)
            {
                m_NextUsableTime = Mathf.Max(m_NextUsableTime, now + m_ChainCooldown);
            }
            m_ExecuteAct?.Invoke(m_NextUsableTime, last, m_Stages.Length, m_CurrentStage);
            ResetChainInternal();
        }
        else
        {
            m_ExecuteAct?.Invoke(m_NextUsableTime, last, m_Stages.Length, m_CurrentStage);
        }


        if (m_EnableDebug)
        {
            Debug.Log($"[SkillChain] Exec {skill.SkillName} (stage {stageIndex}) " +
                      $"lockout→{m_LockoutEnd:0.00}, window→{m_ComboWindowEnd:0.00}, next→{m_NextUsableTime:0.00}");
        }
        return true;
    }

    private void ResetChainInternal()
    {
        m_CurrentStage = -1;
        m_ComboWindowEnd = 0f;
        // 락아웃/쿨다운은 m_NextUsableTime에 반영된 상태
    }

    // ===== ISkillChain 구현 =====
    public bool IsUsableNow()
    {
        if (m_Frozen) return false;
        return Time.time >= m_NextUsableTime;
    }

    public float GetCooldownRemaining()
    {
        float remain = m_NextUsableTime - Time.time;
        return (remain > 0f) ? remain : 0f;
    }

    public int GetCurrentStageIndex() => m_CurrentStage;

    public void Freeze() 
    { 
        m_Frozen = true;
        m_ExecuteAct?.Invoke(-1, false, -2, -3);
    }
    public void Unfreeze() 
    { 
        m_Frozen = false;
        m_ExecuteAct?.Invoke(-3, true, -2, -1);
    }
    public void Interrupt()
    {
        ResetChainInternal(); // 쿨다운 유지, 콤보만 종료
        if (m_EnableDebug) Debug.Log("[SkillChain] Interrupted / reset to idle");
    }

    public SkillType GetSkillType()
    {
        return m_Type;
    }
}
