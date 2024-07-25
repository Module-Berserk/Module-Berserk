
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class GameStateManager
{
    private static GameState activeGameState = null;
    public static GameState ActiveGameState
    {
        get
        {
            // 게임을 메인 화면부터 시작하지 않고 미션 scene을 바로
            // 테스트하는 경우 activeGameState가 존재하지 않으므로
            // 임시로 사용할 수 있는 데이터를 만들어줘야 함.
            if (activeGameState == null)
            {
                Debug.Log("Using a dummy GameState for test purpose!");
                activeGameState = GameState.CreateDummyState();
            }

            return activeGameState;
        }
        set
        {
            activeGameState = value;
        }
    }

    public static void SaveActiveGameState()
    {
        RecordAllPersistentData();
        WriteSaveDataToFile(ActiveGameState.SaveFileName);
    }

    public static List<GameState> LoadSavedGameStates()
    {
        List<GameState> states = new();

        GameState state = ReadSaveDataFromFile("slot0.savedata");  // TODO: 세이브 파일 여러개인 상황 고려하기
        if (state != null)
        {
            states.Add(state);
        }

        return states;
    }

    private static void WriteSaveDataToFile(string filename)
    {
        BinaryFormatter formatter = new();
        string path = Application.persistentDataPath + "/" + filename;

        FileStream stream = new(path, FileMode.Create);
        formatter.Serialize(stream, activeGameState);
        stream.Close();
    }

    private static GameState ReadSaveDataFromFile(string filename)
    {
        string path = Application.persistentDataPath + "/" + filename;
        if (File.Exists(path))
        {
            BinaryFormatter formatter = new();
            FileStream stream = new(path, FileMode.Open);
            GameState state = formatter.Deserialize(stream) as GameState;
            stream.Close();

            return state;
        }
        else
        {
            return null;
        }
    }

    private static void RecordAllPersistentData()
    {
        // 맵에서 세이브 데이터 저장할 놈들 전부 찾고 SceneState에 기록하기
        var statefulObjects = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPersistentSceneState>();
        foreach (var statefulObject in statefulObjects)
        {
            statefulObject.Save(activeGameState.SceneState);
        }
    }

    private static void RestoreAllPersistentData()
    {
        // 맵에서 세이브 데이터 저장했던 놈들 전부 찾고 SceneState에 기록하기
        var statefulObjects = GameObject.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IPersistentSceneState>();
        foreach (var statefulObject in statefulObjects)
        {
            statefulObject.Load(activeGameState.SceneState);
        }
    }

    public static async UniTask RestoreGameStateAsync(GameState gameState)
    {
        activeGameState = gameState;

        await SceneManager.LoadSceneAsync(gameState.SceneState.SceneName);
        
        RestoreAllPersistentData();
    }

    // 플레이어가 미션에서 부활할 때 사용하는 함수.
    // 마지막으로 저장된 세이브 포인트로 돌아간다.
    public static async UniTask RestoreLastSavePointAsync()
    {
        GameState lastSavePointState = ReadSaveDataFromFile(ActiveGameState.SaveFileName);
        await GameStateManager.RestoreGameStateAsync(lastSavePointState);
    }
}
