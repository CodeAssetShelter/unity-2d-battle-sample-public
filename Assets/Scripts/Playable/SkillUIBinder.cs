using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��ų ����̹��� ���¸� ��ư UI �� ���ε�
/// </summary>
public class SkillUIBinder : MonoBehaviour
{
    public PlayerSkills m_PlayerSkills;
    private void Awake()
    {
        if (TryGetComponent<PlayerSkills>(out var playerSkills))
        {
            var list = playerSkills.Slots;
            foreach (var item in list)
            {
                var cd = item.m_ChainDriver.GetComponent<SkillChainDriver>();
                var uibb = UIManager.Instance.GetUIButtonBinder
                    ((UIManager.ButtonType)Enum.Parse(typeof(UIManager.ButtonType), item.m_Label));

                cd.SetUIBinder(uibb.SetAll);
            }
        }
    }
}
