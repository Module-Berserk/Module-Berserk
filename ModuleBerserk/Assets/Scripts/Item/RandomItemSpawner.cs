using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

// 맵에 처음 진입할 때 랜덤한 아이템을 골라서 DroppedItem으로 생성해주는 스크립트.
// 상태 저장이 가능하며, 세이브 데이터를 로딩하는 경우 저장된 아이템 타입을 복원해준다.
public class RandomItemSpawner : ObjectGUID, IPersistentSceneState
{
    [SerializeField] private ActiveItemDatabase activeItemDatabase;
    [SerializeField] private GameObject droppedItemPrefab;

    private ItemType spawnedItemType;
    private DroppedItem spawnedItem = null;

    // 스폰 대기 도중에 맵을 이동하는 경우 아이템 스폰 취소...
    // 디버깅 용도로 세이브 데이터 로딩을 매우 빠르게 반복할 때
    // 오브젝트는 파괴되고 UniTask만 살아남아서
    // MissingReferenceException이 뜨는 것을 막아준다.
    //
    // Note: 실제로 이런 일이 일어날 가능성은 매우 낮음.
    private CancellationTokenSource cancellationTokenSource = new();

    private void Start()
    {
        SpawnItemIfNotLoadedAsync().Forget();
    }

    private void OnDestroy()
    {
        cancellationTokenSource.Cancel();
    }

    private async UniTask SpawnItemIfNotLoadedAsync()
    {
        // 세이브 데이터를 로딩해서 온 맵일 수도 있으니 혹시 모를 Load() 실행을 기다림.
        await UniTask.WaitForSeconds(0.05f, cancellationToken: cancellationTokenSource.Token);

        // 세이브 데이터를 불러오지 않은 경우에만 이 시점까지 spawnedItem이 null로 유지됨
        if (spawnedItem == null)
        {
            spawnedItemType = activeItemDatabase.ChooseRandomItemType();
            SpawnItem();
        }
    }

    private void SpawnItem()
    {
        spawnedItem = Instantiate(droppedItemPrefab, transform.position, Quaternion.identity).GetComponent<DroppedItem>();
        spawnedItem.ItemPrefab = activeItemDatabase.GetItemPrefab(spawnedItemType);
    }

    void IPersistentSceneState.Load(SceneState sceneState)
    {
        if (sceneState.ItemSpawner.ContainsKey(ID))
        {
            spawnedItemType = sceneState.ItemSpawner[ID];
            SpawnItem();
        }
        else
        {
            // 세이브 데이터에 키가 없으면 뭔가 저장에 문제가 생겼을 가능성이 있음.
            //
            // 예외가 발생할 수 있는 특수한 상황:
            // 1. 테스트용으로 타이틀이 아닌 임의의 scene에서 플레이를 시작한 경우
            // - dummy game state를 만들고 적용하는 과정에서 Load가 호출되므로 키가 없어도 정상임
            // 2. 유니콘에 제출할 목적으로 만든 튜토리얼 겸 챕터1 스테이지가 새 게임의 시작 scene으로 지정된 경우
            // - 랜덤 아이템 스포너는 의뢰 맵에서만 존재하므로 최초로 맵에 진입할 때 Load가 실행되는 경우는 없음
            // - 하지만 유니콘용 빌드는 첫 맵이 예외적으로 튜토리얼을 챕터1에 끼워넣은 구조로 구성되므로 저장된 키가 없는게 정상임
            Debug.LogWarning("랜덤 아이템 스포너의 ID가 세이브 데이터에 존재하지 않습니다.\n해당 경고가 발생할 수 있는 특수한 상황에 대해서는 RandomItemSpawner 클래스의 Load() 함수를 참고하십시오.", gameObject);
        }
    }

    void IPersistentSceneState.Save(SceneState sceneState)
    {
        sceneState.ItemSpawner.TryAdd(ID, spawnedItemType);
    }
}
