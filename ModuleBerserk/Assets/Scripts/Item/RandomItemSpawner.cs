using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            ChooseRandomItemType();
            SpawnItem();
        }
    }

    private void ChooseRandomItemType()
    {
        // TODO: 희귀도에 따른 랜덤 선택으로 바꿀 것
        spawnedItemType = UnityEngine.Random.Range(0, 2) == 0 ? ItemType.FireGrenade : ItemType.SmokeGrenade;
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
            // 세이브 데이터에 키가 없으면 뭔가 저장에 문제가 생긴 것...
            throw new NotImplementedException();
        }
    }

    void IPersistentSceneState.Save(SceneState sceneState)
    {
        sceneState.ItemSpawner.TryAdd(ID, spawnedItemType);
    }
}
