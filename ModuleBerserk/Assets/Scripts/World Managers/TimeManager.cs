using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static void PauseGame()
    {
        Time.timeScale = 0f;
        TrySetPlayerInputEnabled(false);
    }

    public static void ResumeGame()
    {
        Time.timeScale = 1f;
        TrySetPlayerInputEnabled(true);
    }

    private static void TrySetPlayerInputEnabled(bool enabled)
    {
        // 플레이어 오브젝트가 맵에 없을 수도 있으니 null 체크 필요
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.GetComponent<PlayerManager>().SetInputEnabled(enabled);
        }
    }
}
