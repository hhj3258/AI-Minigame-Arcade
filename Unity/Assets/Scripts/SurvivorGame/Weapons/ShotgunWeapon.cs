using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 샷건 무기. 3방향 산탄 발사. 레벨업 시 발사 수 +1.
/// </summary>
public class ShotgunWeapon : WeaponBase
{
    [SerializeField] private GameObject _bulletPrefab;

    private ObjectPool<Bullet> _pool;

    protected override void OnInitialize()
    {
        if (_bulletPrefab == null)
        {
            Debug.LogError("ShotgunWeapon: BulletPrefab이 할당되지 않았습니다.", this);
            return;
        }

        _pool = new ObjectPool<Bullet>(
            createFunc:        () => CreateBullet(),
            actionOnGet:       b  => b.gameObject.SetActive(true),
            actionOnRelease:   b  => b.gameObject.SetActive(false),
            actionOnDestroy:   b  => { if (b != null) Destroy(b.gameObject); },
            collectionCheck:   false,
            defaultCapacity:   20,
            maxSize:            50
        );
    }

    protected override void Fire()
    {
        if (PlayerTransform == null || _pool == null) return;

        var stat  = CurrentStat;
        int count = stat != null ? stat.projectileCount : 3;
        float damage = stat != null ? stat.damage : 10f;

        // 가장 가까운 적 방향 탐색
        Vector2 baseDir = GetNearestEnemyDirection();

        // 부채꼴 분산: 중앙 기준 ±15°씩
        float spread = 15f;
        float totalArc = spread * (count - 1);
        float startAngle = -totalArc / 2f;

        for (int i = 0; i < count; i++)
        {
            float   angle  = startAngle + spread * i;
            Vector2 dir    = Rotate(baseDir, angle);

            Bullet bullet  = _pool.Get();
            bullet.transform.position    = PlayerTransform.position;
            bullet.transform.localScale  = Vector3.one * (stat != null ? stat.size : 1f);
            bullet.Damage   = damage;
            bullet.Launch(dir);
        }
    }

    private Vector2 GetNearestEnemyDirection()
    {
        EnemyBase nearest = EnemyBase.FindNearest(PlayerTransform.position);
        if (nearest != null)
            return (nearest.transform.position - PlayerTransform.position).normalized;
        return Vector2.up;
    }

    private Bullet CreateBullet()
    {
        var go     = Instantiate(_bulletPrefab, ProjectileRoot);
        var bullet = go.GetComponent<Bullet>();
        bullet.OnReturn = b => _pool.Release(b);
        return bullet;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private void OnDestroy()
    {
        _pool?.Dispose();
    }
}
