using UnityEngine;

// 설치형 아이템은 하는 짓이 다 instantiate로 똑같아서 그냥 스크립트 공유함.
// 각 아이템의 prefab에서 turretPrefab만 다르게 넣어주는 방식.
public class TurretInstaller : ActiveItemBase
{
    [SerializeField] private GameObject turretPrefab;

    public override void Use()
    {
        var player = FindObjectOfType<PlayerManager>();
        player.InstallTurret(turretPrefab);
    }
}
