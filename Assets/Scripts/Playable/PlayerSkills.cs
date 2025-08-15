using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public enum SkillSlot : int { Z = 0, X = 1, C = 2 }

[Serializable]
public sealed class ChainSlot
{
    public string m_Label = "Z";
    public MonoBehaviour m_ChainDriver; // SkillChainDriver (ISkillChain)
    [NonSerialized] public ISkillChain m_Chain;
}

public class PlayerSkills : MonoBehaviour, ILockPlayer
{
    [Header("Slots (P : Z / X / C)")]
    [SerializeField] private List<ChainSlot> m_Slots = new();
    public List<ChainSlot> Slots { get { return m_Slots; } }

    [Header("State")]
    [SerializeField] private bool m_EnableDebug = false;
    [SerializeField] private bool m_Hijacked = false; // 보스 강탈
    [SerializeField] private bool m_Lock = false;

    private InputAction m_ZAction, m_XAction, m_CAction;

    private void Awake()
    {
        for (int i = 0; i < m_Slots.Count; i++)
        {
            var drv = m_Slots[i]?.m_ChainDriver;
            
            m_Slots[i].m_Chain = drv as ISkillChain;

            if (drv is IChainOwnerSettable ownerSetter)
                ownerSetter.SetOwner(transform); // 또는 gameObject, transform, Player 등 필요한 참조

            if (m_Slots[i].m_Chain == null && drv != null)
                Debug.LogError($"[PlayerSkills] Slot {i} driver does not implement ISkillChain.");
        }
        CreateInputActions();
    }

    private void OnEnable()
    {
        m_ZAction?.Enable(); m_ZAction.performed += OnZ;
        m_XAction?.Enable(); m_XAction.performed += OnX;
        m_CAction?.Enable(); m_CAction.performed += OnC;
    }

    private void OnDisable()
    {
        if (m_ZAction != null) m_ZAction.performed -= OnZ;
        if (m_XAction != null) m_XAction.performed -= OnX;
        if (m_CAction != null) m_CAction.performed -= OnC;

        m_ZAction?.Disable();
        m_XAction?.Disable();
        m_CAction?.Disable();
    }

    private void CreateInputActions()
    {
        m_ZAction = new InputAction("SkillZ", InputActionType.Button);
        m_ZAction.AddBinding("<Keyboard>/z");
        m_XAction = new InputAction("SkillX", InputActionType.Button);
        m_XAction.AddBinding("<Keyboard>/x");
        m_CAction = new InputAction("SkillC", InputActionType.Button);
        m_CAction.AddBinding("<Keyboard>/c");
    }

    // UI Button에 연결할 메서드
    public void OnPressSkillZ() => TryUse(SkillSlot.Z);
    public void OnPressSkillX() => TryUse(SkillSlot.X);
    public void OnPressSkillC() => TryUse(SkillSlot.C);

    // 키 입력 핸들러
    private void OnZ(InputAction.CallbackContext _) => TryUse(SkillSlot.Z);
    private void OnX(InputAction.CallbackContext _) => TryUse(SkillSlot.X);
    private void OnC(InputAction.CallbackContext _) => TryUse(SkillSlot.C);

    private void TryUse(SkillSlot slot)
    {
        if (m_Hijacked)
        {
            if (m_EnableDebug) Debug.Log("[PlayerSkills] Blocked: hijacked");
            return;
        }
        ActiveSkill((int)slot);
    }

    public bool TryUse() => ActiveSkill(UnityEngine.Random.Range(0, m_Slots.Count));
    public bool TryUse(int _idx)
    {
        if (m_Hijacked)
        {
            if (m_EnableDebug) Debug.Log("[PlayerSkills] Blocked: hijacked");
            return false;
        }
        return ActiveSkill(_idx);
    }

    private bool ActiveSkill(int _idx)
    {
        if (m_Lock) return false;

        if (_idx < 0 || _idx >= m_Slots.Count) return false;

        var chain = m_Slots[_idx].m_Chain;
        if (chain == null) return false;

        bool ok = chain.TryUse();

        if (m_EnableDebug) Debug.Log($"[PlayerSkills] {m_Slots[_idx].m_Label} -> {(ok ? "OK" : "FAIL")}");
        return ok;
    }

    // 보스 강탈/반납
    public ChainSlot HijackSkill()
    {
        List<ChainSlot> slots = (from data in m_Slots
                                 where data != null && data.m_Chain.GetSkillType() == SkillChainDriver.SkillType.Attack
                                 select data).ToList();

        ChainSlot slot = slots[UnityEngine.Random.Range(0, slots.Count)];
        slot.m_Chain.Freeze();
        if (m_EnableDebug) Debug.Log($"[PlayerSkills] {slot.m_Label} Hijacked={true}");

        return slot;
    }


    public ChainSlot SetHijackSkill(ChainSlot slot)
    {
        for (int i = 0; i < m_Slots.Count; i++)
        {
            if (m_Slots[i].m_ChainDriver == null)
            {
                m_Slots[i] = new ChainSlot();
                m_Slots[i].m_ChainDriver = Instantiate(slot.m_ChainDriver, transform);
                m_Slots[i].m_Chain = m_Slots[i]?.m_ChainDriver as ISkillChain;
                if (m_Slots[i].m_Chain is IChainOwnerSettable ownerSetter)
                    ownerSetter.SetOwner(transform); // 또는 gameObject, transform, Player 등 필요한 참조
                m_Slots[i].m_Label = new(slot.m_Label);
                return m_Slots[i];
            }
        }

        return null;
    }

    public void ReturnAllSkill()
    {
        foreach (var item in m_Slots)
        {
            item.m_Chain.Unfreeze();
        }
    }

    public void LockPlayer(bool _isOn)
    {
        m_Lock = _isOn;
    }
}
