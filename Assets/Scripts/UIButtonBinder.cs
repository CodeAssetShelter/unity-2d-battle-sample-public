using System;
using UnityEngine;
using UnityEngine.UI;

public class UIButtonBinder : MonoBehaviour
{
    public static UIButtonBinder Instance;

    [SerializeField] private GameObject m_HiJacked;
    [SerializeField] private Button m_Button;
    [SerializeField] private Image m_CooldownImage;
    [SerializeField] private Text m_StockText;

    float _now = 0;
    float _expireTime = 0;
    int _stageStock = 0;
    bool _last = false;

    private void Awake()
    {
        Instance = this;
    }

    public void SetAll(float _NextUsableTime, bool _last, int _stageLength, int _currentStage)
    {
        // 특수 커맨드
        // 이벤트 따로 연결하기에 공수가 많이듬
        if (_NextUsableTime == -1 && !_last &&
            _stageLength == -2 && _currentStage == -3)
        {
            m_HiJacked.SetActive(true);
            return;
        }
        else if (_NextUsableTime == -3 && _last &&
            _stageLength == -2 && _currentStage == -1)
        {
            m_HiJacked.SetActive(false);
            return;
        }

        if (_stageLength > -2)
            SetData(_stageLength);

        if (_NextUsableTime > -2)
            SetCooldown(_NextUsableTime, _last);
        
        if (_currentStage > -2)
            SetStockIdx(_currentStage);
    }

    public void SetData(int _stageStock)
    {
        this._stageStock = _stageStock;
    }

    public void SetCooldown(float _expireTime, bool _last)
    {
        _now = Time.time;
        this._last = _last;
        this._expireTime = _expireTime;
    }

    public void SetStockIdx(int _stock)
    {
        m_StockText.text = (_stageStock - ++_stock).ToString();
    }

    private void Update()
    {
        float totalDuration = _expireTime - _now; // 남은시간
        float elapsed = totalDuration <= 0 ? 1f : 1f - ((_expireTime - Time.time) / totalDuration);        
        m_CooldownImage.fillAmount = 1 - elapsed;
        if (m_CooldownImage.fillAmount == 0 && _last)
        {
            SetStockIdx(-1);
        }
    }
}
