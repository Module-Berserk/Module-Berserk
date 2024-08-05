using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[Serializable]
public class SceneState
{
    public string SceneName;
    public string ActiveVirtualCameraGUID;

    // scene 로딩이 끝난 직후에 이동할 위치.
    // 만약 null이면 맵 상에 존재하는 Player 오브젝트의 위치를 그대로 유지한다.
    //
    // 세이브 데이터를 불러올 때 세이브 포인트에서 시작할 수 있도록 해주는 기능임!
    public string PlayerSpawnPointGUID;

    // 죽었을 때 체크포인트에서 부활해 재도전할 수 있는 횟수
    public int RemainingRevives;
    private const int NUM_REVIVES_PER_MISSION = 5;

    // 부활해서 다시 보스방 들어갈 때는 컷신 스킵하도록 만들어주는 플래그
    public bool IsBossIntroCutscenePlayed;

    // 이미 파괴된 오브젝트의 식별자를 기록하는 곳.
    // 여기에 엔트리가 존재한다면 세이브 데이터를 로딩한 직후에 해당 오브젝트를 파괴해야 한다.
    public HashSet<string> DestroyedObjects;

    // 엘리베이터같은 오브젝트들이 작동된 상태인지 기록하는 곳.
    // 값이 true라면 즉시 작동이 완료된 상태로 복원해야 한다.
    public Dictionary<string, bool> ObjectActivation;

    // 랜덤 아이템 드랍 상태를 기록하는 곳.
    // 미션을 처음 시작할 때에만 랜덤하게 고르고
    // 세이브 데이터를 로딩할 때에는 여기에 기록된 값을 따른다.
    public Dictionary<string, ItemType> ItemSpawner;

    // 새로운 맵에 들어갈 때 SceneState를 초기화해주는 함수.
    public void InitializeSceneState(string sceneName)
    {
        SceneName = sceneName;
        PlayerSpawnPointGUID = null; // scene에 배치된 플레이어 오브젝트의 기본 위치를 그대로 사용
        RemainingRevives = NUM_REVIVES_PER_MISSION;
        IsBossIntroCutscenePlayed = false;
        DestroyedObjects.Clear();
        ObjectActivation.Clear();
        ItemSpawner.Clear();
    }

    
    // 새 게임을 시작할 때 사용할 초기 맵 상태를 준비함
    public SceneState()
    {
        SceneName = "Chapter1"; // TODO: 튜토리얼 스테이지 생기면 해당 scene으로 변경할 것
        ActiveVirtualCameraGUID = null;
        PlayerSpawnPointGUID = null;
        RemainingRevives = NUM_REVIVES_PER_MISSION;
        IsBossIntroCutscenePlayed = false;
        DestroyedObjects = new HashSet<string>();
        ObjectActivation = new Dictionary<string, bool>();
        ItemSpawner = new Dictionary<string, ItemType>();
    }

    public static SceneState CreateDummyState()
    {
        return new SceneState
        {
            SceneName = SceneManager.GetActiveScene().name,

            // 카메라와 플레이어 스폰 지점을 초기화하지 않으면 맵의 기본 배치를 그대로 사용함!
            // 테스트용으로 특정 위치에서 시작하고 싶을 때 주석 풀고
            // 원하는 세이브 포인트와 Follow Camera의 GUID를 여기에 복붙해서 사용할 것.
            ActiveVirtualCameraGUID = "85906752-5a9b-48b3-8776-7e655f353ca9",
            PlayerSpawnPointGUID = "a3f472cb-689e-439e-926a-efc978da4b25",
            IsBossIntroCutscenePlayed = true,
        };
    }
}
