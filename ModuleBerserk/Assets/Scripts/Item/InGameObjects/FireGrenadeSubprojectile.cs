using UnityEngine;

public class FireGrenadeSubprojectile : MonoBehaviour
{
    [SerializeField] private GameObject DOTAreaPrefab;

    private bool isDOTAreaSpawned = false;

    private void OnCollisionEnter2D(Collision2D other)
    {
        // 경사로에서 둘 이상의 콜라이더에 동시에 충돌하는
        // 상황에서도 한 번만 장판을 생성하도록 제한함.
        if (!isDOTAreaSpawned)
        {
            isDOTAreaSpawned = true;

            // TODO: 지면에 평행하게 도트데미지 영역 생성
            var rotation = Quaternion.LookRotation(Vector3.forward, other.contacts[0].normal);
            Instantiate(DOTAreaPrefab, other.contacts[0].point, rotation);

            Destroy(gameObject);
        }
    }
}
