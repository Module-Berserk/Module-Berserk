// 적, 파괴 가능한 사물 등 한 번 삭제되면 세이브 데이터를 불러왔을 때
// 바로 없어져야 하는 오브젝트들에게 붙는 컴포넌트.
//
// OnDestroy() 함수는 그냥 씬을 넘어갈 때에도 호출되므로
// 정말 인위적으로 파괴될 때 (ex. 사망) RecordAsDestroyed() 함수를
// 실행해줘야 상태가 기록되도록 만들었음.
public class ObjectExistenceSceneState : ObjectGUID, IPersistentSceneState
{
    public void RecordAsDestroyed()
    {
        GameStateManager.ActiveGameState.SceneState.DestroyedObjects.Add(ID);
    }

    void IPersistentSceneState.Load(SceneState sceneState)
    {
        if (sceneState.DestroyedObjects.Contains(ID))
        {
            Destroy(gameObject);
        }
    }

    void IPersistentSceneState.Save(SceneState sceneState)
    {
        // 이 컴포넌트는 오므젝트가 파괴될 때 상태를 실시간으로
        // 저장해서 여기서 처리할 작업이 없음
    }
}
