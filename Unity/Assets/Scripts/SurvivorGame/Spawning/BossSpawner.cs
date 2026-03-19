using System;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 120초 주기로 미니보스를 스폰합니다.
/// </summary>
public class BossSpawner : MonoBehaviour
{
    [SerializeField] private EnemyData  _bossData;
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private Transform  _poolRoot;

    private float _spawnInterval = 120f;

    public event Action<EnemyBase> OnBossDied;

    private Transform _playerTransform;
    private Camera    _camera;
    private float     _timer;
    private bool      _active;

    private ObjectPool<MiniBossEnemy> _pool;

    public void Initialize(Transform playerTransform, Camera camera, SurvivorSettings settings)
    {
        _playerTransform = playerTransform;
        _camera          = camera;
        _spawnInterval   = settings != null ? settings.BossSpawnInterval : 120f;
        _timer           = _spawnInterval;
        _active          = true;

        if (_bossPrefab == null)
        {
            Debug.LogError("BossSpawner: BossPrefab이 할당되지 않았습니다.", this);
            return;
        }

        // 재초기화 시 기존 풀 정리
        _pool?.Dispose();

        Transform parent = _poolRoot != null ? _poolRoot : transform;
        _pool = new ObjectPool<MiniBossEnemy>(
            createFunc:      () =>
            {
                var go = Instantiate(_bossPrefab, parent);
                return go.GetComponent<MiniBossEnemy>();
            },
            actionOnGet:     e => e.gameObject.SetActive(true),
            actionOnRelease: e => e.gameObject.SetActive(false),
            actionOnDestroy: e => Destroy(e.gameObject),
            defaultCapacity: 2,
            maxSize:          5
        );
    }

    public void SetActive(bool active) => _active = active;

    private void Update()
    {
        if (!_active) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            SpawnBoss();
            _timer = _spawnInterval;
        }
    }

    private void SpawnBoss()
    {
        if (_playerTransform == null || _pool == null || _bossData == null) return;

        MiniBossEnemy boss = _pool.Get();
        boss.transform.position = GetSpawnPosition();
        boss.Initialize(_bossData, _playerTransform);
        boss.OnDied += e =>
        {
            OnBossDied?.Invoke(e);
            _pool.Release(boss);
        };
    }

    private Vector2 GetSpawnPosition()
    {
        if (_camera == null) return Vector2.zero;

        float h      = _camera.orthographicSize;
        float w      = h * _camera.aspect;
        float margin = 1f;
        int   side   = UnityEngine.Random.Range(0, 4);

        return side switch
        {
            0 => (Vector2)_camera.transform.position + new Vector2(UnityEngine.Random.Range(-w, w), h + margin),
            1 => (Vector2)_camera.transform.position + new Vector2(UnityEngine.Random.Range(-w, w), -h - margin),
            2 => (Vector2)_camera.transform.position + new Vector2(w + margin, UnityEngine.Random.Range(-h, h)),
            _ => (Vector2)_camera.transform.position + new Vector2(-w - margin, UnityEngine.Random.Range(-h, h)),
        };
    }

    public void Clear()
    {
        _active = false;
    }

    private void OnDestroy() => _pool?.Dispose();
}
