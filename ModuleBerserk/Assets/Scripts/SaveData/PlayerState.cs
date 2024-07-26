using System;

// 플레이어의 무기와 특성을 결정하는 속성
public enum PlayerType
{
    Loyal, // 지팡이, 유지력이 좋음
    Knight, // 망치, 느리고 묵직함
    Rogue, // 단검, 날렵함
}

// 플레이어의 런타임 상태를 나타내는 클래스.
//
// GameState의 일부로 저장되어 맵 이동 또는 세이브 데이터
// 불러오기 등의 상황에서 플레이어를 초기화할 때 사용된다.
[Serializable]
public class PlayerState
{
    public PlayerType PlayerType;

    public CharacterStat HP;
    public CharacterStat AttackDamage;
    public CharacterStat AttackSpeed;
    public CharacterStat Defense;
    public CharacterStat MoveSpeed;

    // scene 로딩이 끝난 직후에 이동할 위치.
    // 만약 null이면 맵 상에 존재하는 Player 오브젝트의 위치를 그대로 유지한다.
    //
    // 세이브 데이터를 불러올 때 세이브 포인트에서 시작할 수 있도록 해주는 기능임!
    public string SpawnPointTag;

    public GearSystemState GearSystemState;

    public ItemSlotState Slot1State;
    public ItemSlotState Slot2State;

    // 새 게임을 시작할 때 사용할 초기 플레이어 상태를 준비함
    public PlayerState()
    {
        PlayerType = PlayerType.Loyal;
        HP = new CharacterStat(100f, 0f, 100f);
        AttackDamage = new CharacterStat(10f, 0f);
        AttackSpeed = new CharacterStat(1f, 0f);
        Defense = new CharacterStat(10f, 0f);
        MoveSpeed = new CharacterStat(3.5f, 0f);
        SpawnPointTag = null;
        GearSystemState = new GearSystemState();
        Slot1State = new ItemSlotState();
        Slot2State = new ItemSlotState();
    }

    public static PlayerState CreateDummyState()
    {
        return new PlayerState
        {
            SpawnPointTag = "BossRoomEntrance",
            GearSystemState = GearSystemState.CreateDummyState(),
            Slot1State = ItemSlotState.CreateDummyState(),
            Slot2State = ItemSlotState.CreateDummyState(),
        };
    }
}
