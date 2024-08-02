using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 일시정지 메뉴의 설정창에서 일어나는 모든 일을 관리하는 클래스.
// 화면 해상도, 볼륨, 화면 흔들림 토글 등의 옵션이 있다.
public class SettingsUI : MonoBehaviour, IUserInterfaceController
{
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private TMP_Dropdown screenResolutionDropdown;

    private List<Resolution> availableResolutions = new();
    private List<string> dropdownOptions = new();

    private void Start()
    {
        fullScreenToggle.isOn = Screen.fullScreen;

        int currentResolutionIndex = FindAllValidResolutions();

        screenResolutionDropdown.ClearOptions();
        screenResolutionDropdown.AddOptions(dropdownOptions);
        screenResolutionDropdown.value = currentResolutionIndex;
        screenResolutionDropdown.RefreshShownValue();
    }

    // IsValidResolution()에서 정의하는 조건에 부합하는 해상도를 모두 찾고
    // availableResolutions와 dropdownOptions 리스트에 각각 해상도와 해상도 설명 문구를 채워넣는다.
    // 반환하는 값은 리스트에서 현재 해상도의 인덱스.
    private int FindAllValidResolutions()
    {
        int currentResolutionIndex = 0;
        Resolution[] allResolutions = Screen.resolutions;
        for (int i = 0; i < allResolutions.Length; ++i)
        {
            if (IsValidResolution(allResolutions[i]))
            {
                AddResolutionOption(allResolutions[i]);

                if (IsCurrentResolution(allResolutions[i]))
                {
                    currentResolutionIndex = i;
                }
            }
        }

        return currentResolutionIndex;
    }

    private bool IsValidResolution(Resolution resolution)
    {
        // 주사율이 다른 해상도는 거부
        if (!resolution.refreshRateRatio.Equals(Screen.currentResolution.refreshRateRatio))
        {
            return false;
        }

        // 16:9 비율이 아니면 거부
        if (resolution.width / 16 != resolution.height / 9)
        {
            return false;
        }

        return true;
    }

    private void AddResolutionOption(Resolution resolution)
    {
        availableResolutions.Add(resolution);
        dropdownOptions.Add($"{resolution.width} x {resolution.height} ({resolution.refreshRateRatio}hz)");
    }

    private bool IsCurrentResolution(Resolution resolution)
    {
        return resolution.width == Screen.currentResolution.width && resolution.height == Screen.currentResolution.height;
    }

    public void SetScreenResolution(int resolutionIndex)
    {
        var resolution = availableResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetFullScreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }

    private void OnEnable()
    {
        UserInterfaceStack.PushUserInterface(this, fullScreenToggle.gameObject);
    }

    private void OnDisable()
    {
        UserInterfaceStack.PopUserInterface(this);
    }

    public void HideSettingsUI()
    {
        gameObject.SetActive(false);
    }

    private void OnEscapeKey(InputAction.CallbackContext context)
    {
        HideSettingsUI();
    }

    void IUserInterfaceController.BindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed += OnEscapeKey;

        fullScreenToggle.interactable = true;
        screenResolutionDropdown.interactable = true;
    }

    void IUserInterfaceController.UnbindInputActions()
    {
        InputManager.InputActions.Common.Escape.performed -= OnEscapeKey;

        fullScreenToggle.interactable = false;
        screenResolutionDropdown.interactable = false;
    }
}
