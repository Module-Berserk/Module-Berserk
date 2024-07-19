
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
    public int Credits {get; set;}
    public PlayerState PlayerState {get; set;}

    // TODO: 스토리 진행도 추가

    public static GameState CreateDummyState()
    {
        return new GameState()
        {
            Credits = 1000,
            PlayerState = PlayerState.CreateDummyState()
        };
    }
}
