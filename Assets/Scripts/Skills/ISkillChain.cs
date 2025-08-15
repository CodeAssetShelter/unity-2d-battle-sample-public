public interface ISkillChain
{
    // 슬롯에서 호출: 지금 사용 시도 (입력/버튼)
    bool TryUse();

    // 상태 질의 (UI/게임 로직용)
    bool IsUsableNow();           // 강탈/쿨다운/락아웃/GCD 포함 종합 판정
    float GetCooldownRemaining(); // 표시용(체인 기준)
    int GetCurrentStageIndex();   // -1이면 Idle

    // 외부 제어
    void Freeze();     // 강탈 등으로 동결 (타이머/입력 정지)
    void Unfreeze();   // 복귀
    void Interrupt();  // 피격/회피 등으로 콤보 중단
    SkillChainDriver.SkillType GetSkillType();
}
