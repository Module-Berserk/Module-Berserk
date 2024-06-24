using System;
using UnityEngine;

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
    public PlayerType PlayerType {get; set;}

    public CharacterStat HP {get; set;}
    public CharacterStat AttackDamage {get; set;}
    public CharacterStat AttackSpeed {get; set;}
    public CharacterStat Defense {get; set;}
    public CharacterStat MoveSpeed {get; set;}

    // scene 로딩이 끝난 직후에 이동할 위치.
    // 만약 null이면 현재 위치를 유지한다.
    //
    // 포탈을 타고 이동하면 같은 scene이라고 해도
    // 다른 위치에서 시작할 수 있기 때문에 필요하다.
    //
    // TODO: 좌표 대신 tag 이름으로 처리하는 방안 고려하기.
    // 맵에 "SpawnPoint1"같은 태그를 가진 오브젝트를 배치하고 이 좌표를 이용하도록 하는게 더 좋을 것 같음.
    public Vector2? SpawnPosition {get; set;}

    public GearSystemState GearSystemState {get; set;}
    
    // TODO: 인벤토리 상태 추가

    public static PlayerState CreateDummyState()
    {
        return new PlayerState
        {
            PlayerType = PlayerType.Loyal,
            HP = new CharacterStat(100f, 0f),
            AttackDamage = new CharacterStat(1f, 0f),
            AttackSpeed = new CharacterStat(1f, 0f),
            Defense = new CharacterStat(10f, 0f),
            MoveSpeed = new CharacterStat(3.5f, 0f),
            SpawnPosition = null,
            GearSystemState = GearSystemState.CreateDummyState(),
        };
    }
}
