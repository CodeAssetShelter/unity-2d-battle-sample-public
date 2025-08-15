# Unity 2D Battle Sample

## 개요
이 프로젝트는 Unity 6 기반의 2D 배틀 샘플 게임입니다.

## 지원 플랫폼
Android, PC, UnityEditor

## 주요 기능
1. **플레이어 이동**
   - NewInputSystem 기반 이동 (Pointer, WASD, ArrowKey)
   - Rigidbody 이용 물리 움직임

2. **공격 시스템**
   - 오브젝트 풀링 이용한 공격 처리
   - 공격 패턴별 독립 처리
   - Enemy 와 Player 가 동일한 공격 시스템을 사용

3. **UI 및 게임 흐름**
   - 간단한 HP, 점수 UI 표시
   - 게임 오버 처리 로직 포함

4. **코드 구조**
   - `PlayerMovement`: 이동 및 방향 제어
   - `SkillChainDriver`: 지정된 키에 스킬을 장착하여 사용하는 모듈
   - `Pool`: 오브젝트 풀링 시스템
   - `GameManager`: 전체 게임 간단 제어

## 사용 툴
- Unity 6
- C#
  

## 실행 방법
1. Unity 6 이상 버전에서 프로젝트 열기 or Release 다운받기
2. `unity-2d-battle-sample` 실행 
3. 키보드 방향키 / WASD로 이동, 스페이스바로 공격 또는 터치로 조작

## 라이선스
이 프로젝트는 MIT 라이선스를 따릅니다.
