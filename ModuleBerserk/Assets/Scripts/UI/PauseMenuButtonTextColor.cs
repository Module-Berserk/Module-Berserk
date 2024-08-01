using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class PauseMenuButtonTextColor : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;

    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == button.gameObject)
        {
            text.color = selectedColor;
        }
        else
        {
            text.color = normalColor;
        }
    }
}
