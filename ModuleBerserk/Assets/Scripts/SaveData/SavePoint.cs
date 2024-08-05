using UnityEngine;

// 주인공이 범위 안에 들어오면 자동으로 상태를 저장해주는 오브젝트.
// 세이브 데이터를 불러올 때 세이브 포인트의 위치로 돌아와야 하므로
// 해당 세이브 포인트를 찾을 수 있는 고유한 태그가 달려있어야 한다!
//
// 같이 딸려오는 PlayerContactTrigger의 OnActivate에 TryAutoSave 함수를 추가해주면 된다.
[RequireComponent(typeof(PlayerContactTrigger))]
public class SavePoint : ObjectGUID
{
    // 세이브 데이터를 불러온 직후나 범위 경계면에서 왔다갔다 할 때
    // 저장이 계속 되는 상황을 막기 위해 일정 시간 쿨타임을 부여함.
    private float saveCooltime = 0.1f; // 세이브 포인트 위에서 스폰되자마자 다시 저장되는 것 방지
    private const float DELAY_BETWEEN_AUTO_SAVE = 10f;

    public void TryAutoSave()
    {
        if (saveCooltime <= 0f)
        {
            Debug.Log($"세이브 포인트 ({ID})에서 저장됨");
            saveCooltime = DELAY_BETWEEN_AUTO_SAVE;

            // 플레이어가 세이브 포인트에서 스폰되도록 설정
            GameStateManager.ActiveGameState.SceneState.PlayerSpawnPointGUID = ID;

            // 나머지 모든 상태 저장
            GameStateManager.SaveActiveGameState();
        }
    }

    private void Update()
    {
        saveCooltime -= Time.deltaTime;
    }
}
