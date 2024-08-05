using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TitleSceneSecondaryUI : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private List<Button> buttons;
    [SerializeField] private TitleSceneSelectSaveDataUI selectSaveDataUI; // 새로하기 또는 이어하기를 누르면 뜨는 세이브 슬롯 선택 창
    [SerializeField] private YesNoSelectionUI dataOverrideWarningUI; // 새 게임을 데이터가 존재하는 슬롯에서 시작하려는 경우 뜨는 경고창
    [SerializeField] private FadeEffect fadeEffect;

    private void OnEnable()
    {
        UserInterfaceStack.PushUserInterface(this, buttons[0].gameObject);
    }

    private void OnDisable()
    {
        UserInterfaceStack.PopUserInterface(this);
    }

    void IUserInterfaceController.BindInputActions()
    {
        foreach (var button in buttons)
        {
            button.interactable = true;
        }
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        foreach (var button in buttons)
        {
            button.interactable = false;
        }
    }

    public void OnQuitGameButtonClick()
    {
        Application.Quit();
    }

    public void OnNewGameButtonClick()
    {
        selectSaveDataUI.gameObject.SetActive(true);

        selectSaveDataUI.OnSelectEmptySlot.AddListener(StartNewGame);
        selectSaveDataUI.OnSelectExistingSlot.AddListener(ShowDataOverrideWarning);
    }

    private void StartNewGame(int slotIndex)
    {
        Debug.Log($"슬롯 {slotIndex}에서 새로운 게임을 시작");
        GameState emptyGameState = new(slotIndex);

        LoadGameStateAsync(emptyGameState).Forget();
    }

    private void ShowDataOverrideWarning(int slotIndex)
    {
        // 덮어쓰기 일어난다고 경고창 띄우고 그래도 괜찮다 하면 StartNewGame 하기
        dataOverrideWarningUI.gameObject.SetActive(true);
        dataOverrideWarningUI.OnSelect.AddListener((isYesClicked) => {
            if (isYesClicked)
            {
                StartNewGame(slotIndex);
            }
        });
    }

    public void OnContinueButtonClick()
    {
        selectSaveDataUI.gameObject.SetActive(true);

        selectSaveDataUI.OnSelectExistingSlot.AddListener(ContinueGame);
    }

    private void ContinueGame(int slotIndex)
    {
        var savedStates = GameStateManager.LoadSavedGameStates();
        LoadGameStateAsync(savedStates[slotIndex]).Forget();
    }

    // 새로하기 또는 이어하기를 선택한 경우 페이드 아웃 -> 게임 시작
    private async UniTask LoadGameStateAsync(GameState gameState)
    {
        // UI 스택에서 순서대로 pop되도록 오브젝트 비활성화
        dataOverrideWarningUI.gameObject.SetActive(false);
        selectSaveDataUI.gameObject.SetActive(false);
        gameObject.SetActive(false);

        fadeEffect.FadeOut();
        await UniTask.WaitForSeconds(1f);
        await GameStateManager.RestoreGameStateAsync(gameState);
    }
}