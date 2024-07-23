public interface IPersistentSceneState
{
    void Save(SceneState sceneState);
    void Load(SceneState sceneState);
}
