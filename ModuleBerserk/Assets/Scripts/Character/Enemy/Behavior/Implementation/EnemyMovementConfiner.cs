using UnityEngine;

public class EnemyMovementConfiner : MonoBehaviour
{
    [SerializeField] private PlayerDetectionRange moveRestrictionArea;

    private void Awake()
    {
        if (moveRestrictionArea == null)
        {
            throw new ReferenceNotInitializedException("moveRestrictionArea");
        }
    }

    public bool IsPlayerInRange()
    {
        return moveRestrictionArea.IsPlayerInRange;
    }

    public bool IsMovingOutsideRestrictedArea(float direction)
    {
        if (moveRestrictionArea == null)
        {
            return false;
        }

        // 왼쪽 범위를 이미 넘었는데 더 왼쪽으로 가려는 경우
        float leftEndX = moveRestrictionArea.Boundary.min.x;
        if (transform.position.x < leftEndX && direction < 0f)
        {
            return true;
        }

        // 오른쪽 범위를 이미 넘었는데 더 오른쪽으로 가려는 경우
        float rightEndX = moveRestrictionArea.Boundary.max.x;
        if (transform.position.x > rightEndX && direction > 0f)
        {
            return true;
        }

        // 범위 안에 있거나 안으로 돌아오는 방향인 경우
        return false;
    }

    // 행동 반경 제한은 붉은색 테두리로 보여줌
    private void OnDrawGizmos()
    {
        var bounds = moveRestrictionArea.GetComponent<Collider2D>().bounds;
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}