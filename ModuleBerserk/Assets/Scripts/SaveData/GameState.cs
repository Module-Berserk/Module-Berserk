
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
}
