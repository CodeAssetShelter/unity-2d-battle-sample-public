// encoding: UTF-8 (65001)
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;

public interface IPlayerSkillOwner
{
    bool CanBeStolen();
    ChainSlot StealOneSkill();      // 하나 비활성 + 참조 반환
    void ReturnSkill(ISkill s); // 복구
}

public sealed class SkillStealSystem : MonoBehaviour
{
    [SerializeField] bool m_IsStolen = false;
    [SerializeField] PlayerSkills m_BossSkills;
    [SerializeField] PlayerSkills m_PlayerSkills;
    [SerializeField] float m_StolenTime = 10f;
    [SerializeField] float m_Cooldown = 20f;

    float _time = 0;
    float _returnTime = 0;
    float _nextStealTime = 0;
    ChainSlot _stolenSkill = null;
    ChainSlot _stolenSkillOrig = null;

    public void StealOneSkill()
    {
        if (m_IsStolen) return;
        if (m_PlayerSkills == null)
        {
            PlayerSkills[] allSkills = FindObjectsByType<PlayerSkills>(FindObjectsSortMode.None);
            foreach (var item in allSkills)
            {
                if (item.CompareTag("Player"))
                {
                    m_PlayerSkills = item;
                    break;
                }
            }
        }
        var slot = m_PlayerSkills.HijackSkill();
        _stolenSkill = m_BossSkills.SetHijackSkill(slot);
        _stolenSkillOrig = slot;

        m_IsStolen = _stolenSkill != null;
        if (m_IsStolen)
        {
            slot.m_Chain.Freeze();
            _returnTime = Time.time + m_StolenTime;
        }
    }

    private void Awake()
    {
        _time = Time.time;
        _returnTime = _time + m_StolenTime;
        _nextStealTime = _time = m_Cooldown;
        m_IsStolen = true;
    }

    private void Update()
    {
        _time = Time.time;

        if (_stolenSkill != null && m_IsStolen && _time > _returnTime)
        {
            _nextStealTime = _time + m_Cooldown;

            var drv = _stolenSkill.m_ChainDriver;
            if (drv)
            {
                // 스킬 드라이버에 해제용 API가 있다면 호출
                // 입력 바인딩, 이벤트, 코루틴 정리
                // 없으면 최소한 아래처럼 슬롯과 인터페이스를 정리
                _stolenSkill.m_Chain = null;
                Destroy(drv.gameObject);
            }

            // 플레이어 스킬 해제
            _stolenSkillOrig.m_Chain.Unfreeze();

            // 내부 상태 정리
            _stolenSkill = null;
        }
        if (_stolenSkill == null && m_IsStolen && _time > _nextStealTime)
        {
            m_IsStolen = false;
            StealOneSkill();
        }
    }
}
