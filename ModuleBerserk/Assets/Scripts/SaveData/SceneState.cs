using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[Serializable]
public class SceneState
{
    public string SceneName;
    public string ActiveVirtualCameraTag;

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
        DestroyedObjects.Clear();
        ObjectActivation.Clear();
        ItemSpawner.Clear();
    }

    public static SceneState CreateDummyState()
    {
        return new SceneState
        {
            SceneName = SceneManager.GetActiveScene().name,
            ActiveVirtualCameraTag = "",
            DestroyedObjects = new HashSet<string>(),
            ObjectActivation = new Dictionary<string, bool>(),
            ItemSpawner = new Dictionary<string, ItemType>(),
        };
    }
}
