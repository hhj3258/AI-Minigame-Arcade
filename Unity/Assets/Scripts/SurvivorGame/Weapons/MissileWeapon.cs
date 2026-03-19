using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 미사일 무기. 가장 가까운 적을 향해 유도 발사체를 발사합니다.
/// 레벨업 시 데미지 +50%.
/// </summary>
public class MissileWeapon : WeaponBase
{
    [SerializeField] private GameObject _missilePrefab;

    private ObjectPool<MissileProjectile> _pool;

    protected override void OnInitialize()
    {
        if (_missilePrefab == null)
        {
            Debug.LogError("MissileWeapon: MissilePrefab이 할당되지 않았습니다.", this);
            return;
        }

        _pool = new ObjectPool<MissileProjectile>(
            createFunc:      () => CreateMissile(),
            actionOnGet:     m  => m.gameObject.SetActive(true),
            actionOnRelease: m  => m.gameObject.SetActive(false),
            actionOnDestroy: m  => { if (m != null) Destroy(m.gameObject); },
            defaultCapacity: 10,
            maxSize:          30
        );
    }

    protected override void Fire()
    {
        if (PlayerTransform == null || _pool == null) return;

        EnemyBase target = EnemyBase.FindNearest(PlayerTransform.position);

        var stat   = CurrentStat;
        // 레벨별 데미지: Lv1 기본, Lv2 +50%, Lv3 +100%
        float dmg  = stat != null ? stat.damage * (1f + 0.5f * (Level - 1)) : 20f;

        var missile  = _pool.Get();
        missile.transform.position   = PlayerTransform.position;
        missile.transform.localScale = Vector3.one * (stat != null ? stat.size : 1f);
        missile.Damage   = dmg;
        missile.Launch(target);
    }

    private MissileProjectile CreateMissile()
    {
        var go      = Instantiate(_missilePrefab, ProjectileRoot);
        var missile = go.GetComponent<MissileProjectile>();
        missile.OnReturn = m => _pool.Release(m);
        return missile;
    }

    private void OnDestroy() => _pool?.Dispose();
}
