
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
    public int Credits;

    // 지금 보스전을 진행하는 중인지 구분하는 플래그.
    // 보스전이라면 플레이어가 죽어도 바로 게임오버로 넘어가지 않고
    // 데스카운트가 남았는지 확인한 뒤 부활 기회를 준다.
    public bool IsBossFight;

    // 보스전에서 죽은 횟수.
    // 몇 번 정도는 메이플처럼 플레이어만 부활시키고 보스전을 이어서 할 수 있다!
    // 보스전 새로 시작할 때마다 0으로 초기화해줘야 함.
    public int BossDeathCount;

    // 아직 데스카운트를 모두 소진하지 않았다면 true를 반환.
    public bool IsBossFightRevivePossible
    {
        get => BossDeathCount < 5; // TODO: 보스전 데스카운트는 나중에 난이도 보고 수정할 것
    }

    public void StartBossFight()
    {
        IsBossFight = true;
        BossDeathCount = 0;
    }

    public PlayerState PlayerState;
    public SceneState SceneState;

    // TODO: 스토리 진행도 추가

    public static GameState CreateDummyState()
    {
        return new GameState()
        {
            Credits = 1000,
            IsBossFight = false,
            BossDeathCount = 0,
            PlayerState = PlayerState.CreateDummyState(),
            SceneState = SceneState.CreateDummyState()
        };
    }
}
