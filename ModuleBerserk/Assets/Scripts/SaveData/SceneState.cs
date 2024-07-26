using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[Serializable]
public class SceneState
{
    public string SceneName;
    public string ActiveVirtualCameraTag;

    // 죽었을 때 체크포인트에서 부활해 재도전할 수 있는 횟수
    public int RemainingRevives;
    private const int NUM_REVIVES_PER_MISSION = 5;

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
        RemainingRevives = NUM_REVIVES_PER_MISSION;
        DestroyedObjects.Clear();
        ObjectActivation.Clear();
        ItemSpawner.Clear();
    }

    
    // 새 게임을 시작할 때 사용할 초기 맵 상태를 준비함
    public SceneState()
    {
        SceneName = "Hideout"; // TODO: 튜토리얼 스테이지 생기면 해당 scene으로 변경할 것
        RemainingRevives = NUM_REVIVES_PER_MISSION;
        ActiveVirtualCameraTag = "FollowCamera1";
        DestroyedObjects = new HashSet<string>();
        ObjectActivation = new Dictionary<string, bool>();
        ItemSpawner = new Dictionary<string, ItemType>();
    }

    public static SceneState CreateDummyState()
    {
        return new SceneState
        {
            SceneName = SceneManager.GetActiveScene().name,
            RemainingRevives = NUM_REVIVES_PER_MISSION,
            ActiveVirtualCameraTag = "FollowCamera1",
            DestroyedObjects = new HashSet<string>(),
            ObjectActivation = new Dictionary<string, bool>(),
            ItemSpawner = new Dictionary<string, ItemType>(),
        };
    }
}
