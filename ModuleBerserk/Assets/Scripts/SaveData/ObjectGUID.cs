using UnityEngine;

// 오브젝트별 상태 저장에 사용할 고유한 문자열 식별자를 만들어준다.
// 과정:
// 1. 오브젝트에 이 스크립트 추가
// 2. inspector에서 스크립트 우클릭 -> Generate GUID for this object
// 3. 게임을 저장하는 순간에 ObjectGUID 컴포넌트의 ID 값을 가져와 자신만의 식별자로 사용
//    ex) 아이템을 이미 먹었는지 Dictionary<string, bool>에 저장
public class ObjectGUID : MonoBehaviour
{
    [Header("Object GUID")]
    public string ID = "";
    // 보통은 이 스크립트가 붙어있으면 세이브 데이터에
    // ID를 사용해 정보를 기록하기 때문에 게임이 실행되었는데
    // ID가 아직 기본값인 ""이라면 GenerateGUID()를 해주는걸 까먹었을 확률이 높음.
    //
    // 위와 같은 이유로 Awake()에서 ID가 ""이라면 경고 로그를 자동으로 띄워주는데,
    // 챕터1 보스전 선반에서 무한 생성되는 상자처럼 예외적으로 상태를 저장하지 않는 경우가 종종 생김.
    // 이 경우에는 의도적으로 ID를 사용하지 않으므로 평소처럼 경고 로그가 떠버리면 진짜 오류와 구분하기 어려움.
    //
    // 따라서 ID를 사용하지 않을 임시 오브젝트가 생성될 때에는 이 플래그를 false로 바꿔서
    // GUID 미초기화 경고를 표시하지 않도록 만들 수 있도록 해두었음.
    public bool LogWarningOnNullGUID = true;

    [ContextMenu("Generate GUID for this object")]
    private void GenerateGUID()
    {
        ID = System.Guid.NewGuid().ToString();
    }

    // GUID 생성을 까먹을 수도 있으니 리마인더 로그 남기기
    protected void Start()
    {
        if (ID == "" && LogWarningOnNullGUID)
        {
            Debug.LogWarning("GUID is not generated yet!", this);
        }
    }
}
