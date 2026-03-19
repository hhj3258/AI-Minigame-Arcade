using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// WaveConfig 배열을 순서대로 실행하는 웨이브 기반 스포너.
/// 마지막 웨이브가 끝나면 처음부터 반복합니다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("EnemyData")]
    [SerializeField] private EnemyData _slimeData;
    [SerializeField] private EnemyData _batData;
    [SerializeField] private EnemyData _golemData;

    [Header("Prefabs")]
    [SerializeField] private GameObject _slimePrefab;
    [SerializeField] private GameObject _batPrefab;
    [SerializeField] private GameObject _golemPrefab;

    [Header("풀 루트")]
    [SerializeField] private Transform _poolRoot;

    public event Action<EnemyBase> OnEnemyDied;

    private Transform  _playerTransform;
    private Camera     _camera;
    private WaveConfig[] _waves;

    private bool  _active;
    private int   _waveIndex;
    private float _waveTimer;   // 현재 웨이브 남은 시간
    private float _spawnTimer;  // 다음 스폰까지 남은 시간

    private readonly Queue<EnemyType> _spawnQueue = new();

    private ObjectPool<SlimeEnemy>  _slimePool;
    private ObjectPool<BatEnemy>    _batPool;
    private ObjectPool<GolemEnemy>  _golemPool;

    private enum EnemyType { Slime, Bat, Golem }

    // ── 초기화 ────────────────────────────────────────────
    public void Initialize(Transform playerTransform, Camera camera, WaveConfig[] waves, Transform poolRoot = null)
    {
        _playerTransform = playerTransform;
        _camera          = camera;
        _waves           = (waves != null && waves.Length > 0) ? waves : DefaultWaves();
        _poolRoot        = poolRoot != null ? poolRoot : transform;

        // 재초기화 시 기존 풀 정리
        _slimePool?.Dispose();
        _batPool?.Dispose();
        _golemPool?.Dispose();

        _slimePool = CreatePool<SlimeEnemy>(_slimePrefab, 20);
        _batPool   = CreatePool<BatEnemy>  (_batPrefab,   20);
        _golemPool = CreatePool<GolemEnemy>(_golemPrefab, 10);

        _waveIndex = 0;
        StartWave(_waveIndex);
        _active = true;
    }

    public void SetActive(bool active) => _active = active;

    // ── 업데이트 ──────────────────────────────────────────
    private void Update()
    {
        if (!_active || _waves == null) return;

        _waveTimer  -= Time.deltaTime;
        _spawnTimer -= Time.deltaTime;

        // 스폰 큐에서 한 마리씩 꺼내 스폰
        if (_spawnTimer <= 0f && _spawnQueue.Count > 0)
        {
            SpawnNext();
            _spawnTimer = SpawnInterval();
        }

        // 웨이브 시간 종료 → 다음 웨이브 (마지막 웨이브에 도달하면 반복)
        if (_waveTimer <= 0f)
        {
            if (_waveIndex < _waves.Length - 1)
                _waveIndex++;
            StartWave(_waveIndex);
        }
    }

    // ── 웨이브 시작 ───────────────────────────────────────
    private void StartWave(int index)
    {
        var wave   = _waves[index];
        _waveTimer = wave.Duration;
        _spawnTimer = 0f;
        BuildSpawnQueue(wave);
        bool isLast = index == _waves.Length - 1;
        string suffix = (isLast && _waves.Length > 1) ? " [마지막 웨이브 반복]" : "";
        Debug.Log($"[EnemySpawner] 웨이브 {index + 1} 시작 (지속:{wave.Duration}s, 총{wave.TotalCount}마리){suffix}");
    }

    private float SpawnInterval()
    {
        if (_waves == null || _waves.Length == 0) return 1f;
        var wave  = _waves[_waveIndex];
        int total = wave.TotalCount;
        return total > 0 ? wave.Duration / total : wave.Duration;
    }

    // ── 스폰 큐 구성 (셔플) ───────────────────────────────
    private void BuildSpawnQueue(WaveConfig wave)
    {
        var list = new List<EnemyType>(wave.TotalCount);
        for (int i = 0; i < wave.SlimeCount; i++) list.Add(EnemyType.Slime);
        for (int i = 0; i < wave.BatCount;   i++) list.Add(EnemyType.Bat);
        for (int i = 0; i < wave.GolemCount; i++) list.Add(EnemyType.Golem);

        // Fisher-Yates 셔플
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        _spawnQueue.Clear();
        foreach (var t in list) _spawnQueue.Enqueue(t);
    }

    // ── 스폰 실행 ─────────────────────────────────────────
    private void SpawnNext()
    {
        if (_playerTransform == null) return;

        switch (_spawnQueue.Dequeue())
        {
            case EnemyType.Slime: SpawnEnemy(_slimePool, _slimeData); break;
            case EnemyType.Bat:   SpawnEnemy(_batPool,   _batData);   break;
            case EnemyType.Golem: SpawnEnemy(_golemPool, _golemData); break;
        }
    }

    private void SpawnEnemy<T>(ObjectPool<T> pool, EnemyData data) where T : EnemyBase
    {
        if (pool == null || _playerTransform == null) return;

        T enemy = pool.Get();
        enemy.transform.position = GetSpawnPosition();
        enemy.Initialize(data, _playerTransform);
        enemy.OnDied += e => OnEnemyDied?.Invoke(e);
        enemy.OnDied += e => ReleaseEnemy(e, pool);
    }

    private void ReleaseEnemy<T>(EnemyBase enemy, ObjectPool<T> pool) where T : EnemyBase
    {
        if (enemy is T typed) pool.Release(typed);
    }

    // ── 스폰 위치 ─────────────────────────────────────────
    private Vector2 GetSpawnPosition()
    {
        if (_camera == null) return Vector2.zero;

        float h      = _camera.orthographicSize;
        float w      = h * _camera.aspect;
        float margin = 0.5f;

        int     side = UnityEngine.Random.Range(0, 4);
        Vector2 pos  = side switch
        {
            0 => new Vector2(UnityEngine.Random.Range(-w, w),  h + margin),
            1 => new Vector2(UnityEngine.Random.Range(-w, w), -h - margin),
            2 => new Vector2( w + margin, UnityEngine.Random.Range(-h, h)),
            _ => new Vector2(-w - margin, UnityEngine.Random.Range(-h, h)),
        };
        return (Vector2)_camera.transform.position + pos;
    }

    // ── 풀 생성 ───────────────────────────────────────────
    private ObjectPool<T> CreatePool<T>(GameObject prefab, int defaultCapacity) where T : EnemyBase
    {
        if (prefab == null)
        {
            Debug.LogError($"EnemySpawner: {typeof(T).Name} Prefab이 할당되지 않았습니다.", this);
            return null;
        }

        Transform parent = _poolRoot != null ? _poolRoot : transform;
        return new ObjectPool<T>(
            createFunc:      () => Instantiate(prefab, parent).GetComponent<T>(),
            actionOnGet:     e  => e.gameObject.SetActive(true),
            actionOnRelease: e  => e.gameObject.SetActive(false),
            actionOnDestroy: e  => Destroy(e.gameObject),
            defaultCapacity: defaultCapacity,
            maxSize:          100
        );
    }

    // ── 기본 웨이브 (Settings 미설정 시 폴백) ─────────────
    private static WaveConfig[] DefaultWaves() => new[]
    {
        new WaveConfig { Duration = 30f, SlimeCount = 5, BatCount = 2, GolemCount = 0 },
        new WaveConfig { Duration = 30f, SlimeCount = 6, BatCount = 3, GolemCount = 1 },
    };

    public void Clear()
    {
        _active = false;
        _spawnQueue.Clear();
        EnemyBase.ClearAll();
    }

    private void OnDestroy()
    {
        _slimePool?.Dispose();
        _batPool?.Dispose();
        _golemPool?.Dispose();
    }
}
