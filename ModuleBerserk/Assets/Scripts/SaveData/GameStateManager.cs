
using System;
using System.Collections.Generic;
using UnityEngine;

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
        // TODO: 파일에 게임 상태 저장하기
        throw new NotImplementedException();
    }

    public static List<GameState> LoadGameStates()
    {
        // TODO: 파일에서 저장된 게임 상태 모두 불러오기
        throw new NotImplementedException();
    }
}
