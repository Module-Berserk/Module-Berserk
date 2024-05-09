using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 일시정지 메뉴를 띄우는 클래스
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;

    private ModuleBerserkActionAssets actionAssets;
    private bool isPaused = false;

    private void Awake()
    {
        actionAssets = new();
        actionAssets.UI.Enable();

        actionAssets.UI.Escape.performed += TogglePauseMenu;
    }

    private void TogglePauseMenu(InputAction.CallbackContext context)
    {
        isPaused = !isPaused;

        pauseMenuUI.SetActive(isPaused);

        if (isPaused)
        {
            TimeManager.PauseGame();
        }
        else
        {
            TimeManager.ResumeGame();
        }
    }
}
