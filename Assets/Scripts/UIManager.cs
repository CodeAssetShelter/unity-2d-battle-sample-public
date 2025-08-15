using System;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager m_Instance;
    public static UIManager Instance
    {
        get
        {
            if (m_Instance == null)
            {
                m_Instance = FindAnyObjectByType<UIManager>();
                if (m_Instance == null)
                {
                    var obj = new GameObject(nameof(UIManager));
                    m_Instance = obj.AddComponent<UIManager>();
                }
            }
            return m_Instance;
        }
    }

    public enum ButtonType
    {
        Z, X, C, Space
    }

    // UI 요소 연결 (예: HP바, 스킬쿨타임 UI 등)
    [Header("UI")]
    [SerializeField] public Text m_GameOverText;
    [SerializeField] public Text m_ReadyText;


    [Header("HP Bars")]
    [SerializeField] private Image m_PlayerHp;
    [SerializeField] private Image m_EnemyHp;

    [Header("Buttons")]
    [SerializeField] private GameObject m_Stick;
    [SerializeField] private UIButtonBinder m_Z;
    [SerializeField] private UIButtonBinder m_X;
    [SerializeField] private UIButtonBinder m_C;
    [SerializeField] private Button m_Sp;


    void Awake()
    {
        if (m_Instance != null && m_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_Instance = this;
    }

    private void InitButtons()
    {
        bool isMobileOrEditor = true;
#if UNITY_EDITOR
        isMobileOrEditor = true;
#elif UNITY_ANDROID
        isMobileOrEditor = true;
#elif UNITY_IOS
        isMobileOrEditor = true;
#else
        isMobileOrEditor = false;
#endif
        ShowAllButtons(isMobileOrEditor);
    }

    public void ShowAllButtons(bool _isOn)
    {
        m_Stick.SetActive(_isOn);
        m_Z.gameObject.SetActive(_isOn);
        m_X.gameObject.SetActive(_isOn);
        m_C.gameObject.SetActive(_isOn);
        m_Sp.gameObject.SetActive(_isOn);
        m_EnemyHp.gameObject.SetActive(_isOn);
        m_PlayerHp.gameObject.SetActive(_isOn);
    }

    public UIButtonBinder GetUIButtonBinder(ButtonType _buttonType)
    {
        UIButtonBinder btn = _buttonType switch
        {
            ButtonType.Z => m_Z,
            ButtonType.X => m_X,
            ButtonType.C => m_C,
            ButtonType.Space => null,
            _ => null
        };

        if (btn == null) return null;
        return btn;
    }

    // HP UI 갱신
    public void UpdateHP(float ratio, bool _isPlayer)
    {
        if (_isPlayer)
        {
            m_PlayerHp.fillAmount = ratio;
        }
        else m_EnemyHp.fillAmount = ratio;
    }

    // 스킬 쿨타임 UI 갱신
    public void UpdateSkillCooldown(int skillIndex, float cooldown)
    {
        Debug.Log($"스킬 {skillIndex} 쿨타임: {cooldown:F1}초");
        // 실제 구현: UI 텍스트나 이미지로 반영
    }
}
