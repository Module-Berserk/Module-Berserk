using UnityEngine;

// 오브젝트별 상태 저장에 사용할 고유한 문자열 식별자를 만들어준다.
// 과정:
// 1. 오브젝트에 이 스크립트 추가
// 2. inspector에서 스크립트 우클릭 -> Generate GUID for this object
// 3. 게임을 저장하는 순간에 ObjectGUID 컴포넌트의 ID 값을 가져와 자신만의 식별자로 사용
//    ex) 아이템을 이미 먹었는지 Dictionary<string, bool>에 저장
public class ObjectGUID : MonoBehaviour
{
    public string ID;

    [ContextMenu("Generate GUID for this object")]
    private void GenerateGUID()
    {
        ID = System.Guid.NewGuid().ToString();
    }
}
