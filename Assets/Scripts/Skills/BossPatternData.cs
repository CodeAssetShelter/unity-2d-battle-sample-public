using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "NPC/BossPatternData", fileName = "BossPatternData")]
public class BossPatternData : ScriptableObject
{
    [System.Serializable]
    public class BossPatternClass
    {
        public bool m_IgnoreOrder = false;
        public float m_InputCooldown = 0.1f; // 바인딩 된 버튼을 n초마다 누름
        public float m_MaintainInput = 0f;   // m_InputCooldown 이 총 n 초 동안 작동함
        public float m_PatternCooldown = 0f; // m_MaintainInput 이 지난 후 쉬는 시간
    }
    
    [Header("- Label")]
    public string m_Label = "Boss";

    [Header("- Pattern Preset")]
    public List<BossPatternClass> m_Patterns = new();
}
