using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class TestSaveLoad : MonoBehaviour
{
    ModuleBerserkActionAssets inputActions;

    private void Awake()
    {
        inputActions = new();
    }

    private void OnEnable()
    {
        inputActions.Debug.Enable();
        inputActions.Debug.Save.performed += SaveGame;
        inputActions.Debug.Load.performed += LoadGame;
    }

    private void OnDisable()
    {
        inputActions.Debug.Disable();
        inputActions.Debug.Save.performed -= SaveGame;
        inputActions.Debug.Load.performed -= LoadGame;
    }

    private void SaveGame(InputAction.CallbackContext context)
    {
        Debug.Log("데이터 저장하는 중...");
        GameStateManager.SaveActiveGameState();
    }

    private void LoadGame(InputAction.CallbackContext context)
    {
        List<GameState> states = GameStateManager.LoadSavedGameStates();
        Debug.Log("데이터 읽어오는 중...");
        Debug.Log($"세이브 데이터의 수: {states.Count}");

        if (states.Count > 0)
        {
            Debug.Log("첫 번째 세이브 데이터를 사용!");
            GameStateManager.RestoreGameStateAsync(states[0]).Forget();
        }
    }
}
