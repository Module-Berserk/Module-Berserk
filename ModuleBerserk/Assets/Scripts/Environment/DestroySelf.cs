using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이펙트처럼 애니메이션 끝나면 삭제되어야 하는 오브젝트들이
// 애니메이션 이벤트로 사용할 수 있도록 Destroy 함수를 제공함.
public class DestroySelf : MonoBehaviour
{
    public void Destroy()
    {
        Destroy(gameObject);
    }
}
