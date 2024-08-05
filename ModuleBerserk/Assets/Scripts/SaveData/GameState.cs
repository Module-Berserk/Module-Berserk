
using System;

// 게임 세션동안 유지되는 모든 저장 가능한 데이터를 관리한다.
// 스토리 진행도, 보유한 크레딧, 플레이어의 체력 등을 포함한다.
//
// 모든 scene을 self-contained하게 만들기 위해 플레이어나 UI 등의 오브젝트는
// 각 scene마다 존재하며, 맵이 로딩된 후에 GameState의 내용대로 초기화된다.
//
// 정상적으로 게임을 진행하는 경우에는 세이브 데이터에서 불러온 GameState가 존재하며,
// 개발 과정에서 임의의 scene을 테스트하는 경우 테스트용 더미 GameState를 임시로 생성해 사용한다.
[Serializable]
public class GameState
{
    public string SaveFileName;
    public int Credits;

    // 선택한 의뢰에 해당하는 scene 이름.
    // 은신처 오른쪽 끝으로 가서 미션을 시작할 때 사용된다.
    public string NextMissionSceneName;

    public PlayerState PlayerState;
    public SceneState SceneState;

    // TODO: 스토리 진행도 추가

    // 새 게임을 시작할 때 사용할 초기 게임 상태를 준비함
    public GameState(int slotIndex)
    {
        SaveFileName = GameStateManager.GetSaveFileName(slotIndex);
        Credits = 0;
        NextMissionSceneName = "Chapter1"; // TODO: 의뢰처 생기면 null로 설정하기. 지금은 일단 챕터1로 직행하도록 해놨음
        PlayerState = new PlayerState();
        SceneState = new SceneState();
    }

    public static GameState CreateDummyState()
    {
        return new GameState(0)
        {
            Credits = 1000,
            NextMissionSceneName = "Chapter1",
            PlayerState = PlayerState.CreateDummyState(),
            SceneState = SceneState.CreateDummyState()
        };
    }

    // 은신처에서 미션 scene으로 이동하기 직전에 실행되는 함수.
    // 부활 가능 횟수, 오브젝트 파괴 현황 등 세이브 데이터에 들어갈 내용들의 상태를 초기화해준다.
    public void InitializeStateOnMissionStart()
    {
        PlayerState.GearSystemState.NeedInitialRampUp = true; // 기어 0단계에서 1단계로 쭉 올리는 연출 보여줌
        SceneState.InitializeSceneState(NextMissionSceneName);
    }

    // 미션 성공/실패 공통으로 은신처로 돌아간 직후 실행되는 함수.
    // 미션 도중에만 유효한 상태들을 모두 정리해준다.
    public void CleanupStateOnMissionEnd()
    {
        // 세이브 포인트 기록 제거
        SceneState.PlayerSpawnPointGUID = null;

        // 체력 리필
        PlayerState.HP.ResetToMaxValue();

        // 각종 버프/디버프 제거
        PlayerState.AttackDamage.ResetModifiers();
        PlayerState.MoveSpeed.ResetModifiers();
        PlayerState.Defense.ResetModifiers();

        // 기어 시스템과 아이템 초기화
        PlayerState.GearSystemState.Reset();
        PlayerState.Slot1State.Reset();
        PlayerState.Slot2State.Reset();
    }
}
