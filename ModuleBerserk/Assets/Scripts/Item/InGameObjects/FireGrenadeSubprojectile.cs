using UnityEngine;

public class FireGrenadeSubprojectile : MonoBehaviour
{
    [SerializeField] private GameObject DOTAreaPrefab;

    private void OnCollisionEnter2D(Collision2D other)
    {
        // TODO: 지면에 평행하게 도트데미지 영역 생성
        var rotation = Quaternion.LookRotation(Vector3.forward, other.contacts[0].normal);
        Instantiate(DOTAreaPrefab, other.contacts[0].point, rotation);

        Destroy(gameObject);
    }
}
