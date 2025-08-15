using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Skill Stage Data", fileName = "SkillStageData")]
public class SkillStageData : ScriptableObject
{
    [Header("- Label")]
    public string m_Label = "Stage1";

    // 프리팹이나 에셋에 붙은 ISkill 구현 컴포넌트를 지정하세요.
    // (씬 오브젝트 참조 지양, 프로젝트 에셋 참조 권장)
    public MonoBehaviour m_SkillComponent; // ISkill

    [Header("- Timer / CoolDown (Chain inline)")]
    public float m_StageCooldown = 0f;   // 이 단계만의 쿨다운(선택)
    public float m_Recovery = 0.1f;      // 후딜/락아웃 시간
    public float m_InputBuffer = 0.2f;   // 다음 단계 입력 버퍼(필요시 확장)
    public float m_ComboTimeout = 0.8f;  // 다음 입력 없으면 콤보 리셋

    public ISkill GetSkill()
    {
        return m_SkillComponent as ISkill;
    }
}
