using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

/// <summary>
/// 뱀서라이크 게임 컨트롤러. IMinigame 구현체.
/// SurvivorCamera, GameWorld, UIDocument를 총괄 관리합니다.
/// </summary>
public class SurvivorGame : MonoBehaviour, IMinigame
{
    [Header("설정")]
    [SerializeField] private SurvivorSettings _settings;
    [SerializeField] private PlayerData       _playerData;

    [Header("씬 참조")]
    [SerializeField] private Camera           _survivorCamera;
    [SerializeField] private PlayerController _player;
    [SerializeField] private EnemySpawner     _enemySpawner;
    [SerializeField] private BossSpawner      _bossSpawner;

    [Header("EXP 오브")]
    [SerializeField] private GameObject       _expOrbPrefab;

    [Header("풀 루트")]
    [SerializeField] private Transform        _poolRoot;

    // ── 런타임 상태 ────────────────────────────────────────
    // 무기 인스턴스: PlayerController.LoadWeaponsAsync() 후 할당
    private ShotgunWeapon _shotgunWeapon;
    private OrbWeapon     _orbWeapon;
    private MissileWeapon _missileWeapon;

    private SurvivorRunData _runData;
    private LevelSystem     _levelSystem;
    private VirtualJoystick _joystick;
    private SurvivorHUD     _hud;
    private UpgradePanel    _upgradePanel;
    private SurvivorResultPanel _resultPanel;

    private ObjectPool<ExpOrb> _expOrbPool;
    private readonly HashSet<ExpOrb> _activeOrbs = new HashSet<ExpOrb>(); // 회수용 추적

    private bool   _isActive;
    private float  _elapsed;
    private bool   _weaponsLoaded;

    // ── IMinigame ──────────────────────────────────────────
    public async UniTask InitializeAsync()
    {
        // 무기 프리팹 Addressable 로드 (최초 1회만)
        if (!_weaponsLoaded)
        {
            await _player.LoadWeaponsAsync();
            _shotgunWeapon = _player.ShotgunWeapon;
            _orbWeapon     = _player.OrbWeapon;
            _missileWeapon = _player.MissileWeapon;
            _weaponsLoaded = true;
        }
    }

    public void OnGameStart()
    {
        ResetGame();
        SetGameActive(false); // 카드 진입 전엔 비활성 (ActivateCard에서 활성화)
    }

    public void OnGameEnd()
    {
        SetGameActive(false);
    }

    // ── 라이프사이클 ────────────────────────────────────────
    private void Awake()
    {
        var doc  = GetComponent<UIDocument>();
        var root = doc != null ? doc.rootVisualElement : null;

        if (root != null)
        {
            _hud          = new SurvivorHUD(root);
            _upgradePanel = new UpgradePanel(root);
            _resultPanel  = new SurvivorResultPanel(root);

            // 가상 조이스틱
            var joystickArea   = root.Q("joystick-area");
            var joystickHandle = root.Q("joystick-handle");
            if (joystickArea != null && joystickHandle != null)
                _joystick = new VirtualJoystick(joystickArea, joystickHandle);
        }

        // 업그레이드 패널 이벤트
        if (_upgradePanel != null)
            _upgradePanel.OnWeaponSelected += OnWeaponSelected;

        // 결과 패널 재시작
        if (_resultPanel != null)
            _resultPanel.OnRestartClicked += OnRestartClicked;

        // EXP 오브 풀
        if (_expOrbPrefab != null)
        {
            _expOrbPool = new ObjectPool<ExpOrb>(
                createFunc:      () =>
                {
                    var go = Instantiate(_expOrbPrefab, _poolRoot != null ? _poolRoot : transform);
                    return go.GetComponent<ExpOrb>();
                },
                actionOnGet:     o => o.gameObject.SetActive(true),
                actionOnRelease: o => o.gameObject.SetActive(false),
                actionOnDestroy: o => { if (o != null) Destroy(o.gameObject); },
                defaultCapacity: 30,
                maxSize:          200
            );
        }
    }

    private void Start()
    {
        SetGameActive(false);
    }

    // ── 게임 활성화 ────────────────────────────────────────
    public void ActivateCard()
    {
        if (_runData == null) ResetGame();
        SetGameActive(true);
    }

    public void DeactivateCard()
    {
        SetGameActive(false);
    }

    private void SetGameActive(bool active)
    {
        _isActive = active;

        if (_survivorCamera != null) _survivorCamera.enabled = active;
        if (_enemySpawner  != null) _enemySpawner.SetActive(active);
        if (_bossSpawner   != null) _bossSpawner.SetActive(active);
        if (_player        != null) _player.SetActive(active);

        // 무기 활성화: 비활성화 시 전체, 활성화 시 플레이어 보유 무기만
        foreach (var w in GetWeapons())
        {
            if (!active)
                w.SetActive(false);
            else if (_player != null && _player.Weapons.Contains(w))
                w.SetActive(true);
        }
    }

    // ── 업데이트 ──────────────────────────────────────────
    private void Update()
    {
        if (!_isActive || _runData == null) return;

        _elapsed         += Time.deltaTime;
        _runData.SurviveTime = _elapsed;

        _hud?.UpdateTimer(_elapsed);
    }

    // ── 레벨 시스템 ────────────────────────────────────────
    private void OnEnemyDied(EnemyBase enemy)
    {
        if (_runData == null || enemy.IsDead == false) return;

        _runData.KillCount++;
        _hud?.UpdateKillCount(_runData.KillCount);

        // EXP 오브 드랍
        if (_expOrbPool != null)
        {
            ExpOrb orb = _expOrbPool.Get();
            _activeOrbs.Add(orb);
            orb.transform.position = enemy.transform.position;
            orb.Initialize(enemy.GetComponent<EnemyBase>()
                             is EnemyBase e ? GetExpValue(e) : 5,
                           _player?.transform);
            orb.OnReturn = o =>
            {
                _activeOrbs.Remove(o);
                _levelSystem?.AddExp(o.ExpValue);
                _expOrbPool?.Release(o);
            };
        }
    }

    private int GetExpValue(EnemyBase enemy) => enemy.ExpValue;

    private void OnLevelUp(int newLevel)
    {
        var maxLevels = new System.Collections.Generic.Dictionary<string, int>
        {
            ["shotgun"] = _shotgunWeapon != null ? _shotgunWeapon.MaxLevel : 3,
            ["orb"]     = _orbWeapon     != null ? _orbWeapon.MaxLevel     : 3,
            ["missile"] = _missileWeapon != null ? _missileWeapon.MaxLevel : 3,
        };
        _upgradePanel?.Show(_runData, newLevel, maxLevels);
    }

    private void OnWeaponSelected(string weaponId)
    {
        if (_runData == null) return;

        switch (weaponId)
        {
            case "shotgun":
                _runData.ShotgunLevel++;
                ApplyWeaponLevel(_shotgunWeapon, _runData.ShotgunLevel);
                break;
            case "orb":
                _runData.OrbLevel++;
                ApplyWeaponLevel(_orbWeapon, _runData.OrbLevel);
                break;
            case "missile":
                _runData.MissileLevel++;
                ApplyWeaponLevel(_missileWeapon, _runData.MissileLevel);
                break;
        }

        _levelSystem?.ResumeAfterLevelUp();
    }

    private void ApplyWeaponLevel(WeaponBase weapon, int level)
    {
        if (weapon == null) return;

        if (level == 1)
        {
            // 최초 획득: 초기화 (_data는 각 무기 컴포넌트에 인스펙터에서 연결)
            weapon.Initialize(_player?.transform, _poolRoot != null ? _poolRoot : transform);
            weapon.SetActive(_isActive);
            _player?.Weapons.Add(weapon);
        }
        else
        {
            // 강화
            weapon.LevelUp();
        }
    }

    // ── 플레이어 HP ────────────────────────────────────────
    private void OnPlayerHPChanged(int current, int max)
    {
        _runData.CurrentHP = current;
        _hud?.UpdateHP(current, max);
    }

    private void OnPlayerDead()
    {
        _isActive = false;
        Time.timeScale = 1f;   // 레벨업 도중 사망해도 timeScale 복구
        _upgradePanel?.Hide(); // 업그레이드 패널 강제 닫기
        SetGameActive(false);
        ShowResult();
    }

    private void ShowResult()
    {
        _resultPanel?.Show(_runData);
    }

    private void OnRestartClicked()
    {
        _resultPanel?.Hide();
        ResetGame();
        SetGameActive(true);
    }

    // ── 초기화 ─────────────────────────────────────────────
    private void ResetGame()
    {
        // timeScale 복구 (레벨업 도중 리셋되는 경우 대비)
        Time.timeScale = 1f;

        int maxHP = _playerData != null ? _playerData.MaxHP : 100;

        // 이전 이벤트 핸들러 제거 (누적 방지)
        if (_player != null)
        {
            _player.OnHPChanged -= OnPlayerHPChanged;
            _player.OnDead      -= OnPlayerDead;
        }
        if (_enemySpawner != null) _enemySpawner.OnEnemyDied -= OnEnemyDied;
        if (_bossSpawner  != null) _bossSpawner.OnBossDied   -= OnEnemyDied;

        // 필드에 남은 ExpOrb 전체 회수
        foreach (var orb in _activeOrbs)
        {
            if (orb != null) _expOrbPool?.Release(orb);
        }
        _activeOrbs.Clear();

        // 적 전체 비활성화
        EnemyBase.ClearAll();

        _runData = new SurvivorRunData();
        _runData.Reset(maxHP);
        _elapsed = 0f;

        int[] thresholds = _settings != null ? _settings.ExpThresholds : null;
        _levelSystem = new LevelSystem(thresholds);
        _levelSystem.OnLevelUp    += OnLevelUp;
        _levelSystem.OnExpChanged += (cur, max2) =>
        {
            _hud?.UpdateExp(cur, max2, _levelSystem.Level);
        };

        // 플레이어 초기화
        if (_player != null)
        {
            _player.transform.position = Vector3.zero;
            _player.Initialize(maxHP);
            _player.Joystick     = _joystick;
            _player.MoveSpeed    = _playerData != null ? _playerData.MoveSpeed : 5f;
            _player.transform.localScale = Vector3.one * (_playerData != null ? _playerData.Size : 1f);
            _player.OnHPChanged  += OnPlayerHPChanged;
            _player.OnDead       += OnPlayerDead;
        }

        // 스포너 초기화
        if (_enemySpawner != null && _player != null)
        {
            _enemySpawner.Initialize(_player.transform, _survivorCamera, _settings != null ? _settings.Waves : null);
            _enemySpawner.OnEnemyDied += OnEnemyDied;
        }

        if (_bossSpawner != null && _player != null)
        {
            _bossSpawner.Initialize(_player.transform, _survivorCamera, _settings);
            _bossSpawner.OnBossDied += OnEnemyDied;
        }

        // 무기 초기화 (모두 비활성화 후 샷건만 기본 지급)
        foreach (var w in GetWeapons())
            w.SetActive(false);

        // 시작 시 샷건 자동 지급
        if (_shotgunWeapon != null && _player != null)
        {
            _runData.ShotgunLevel = 1;
            _shotgunWeapon.Initialize(_player.transform, _poolRoot != null ? _poolRoot : transform);
            _shotgunWeapon.SetActive(true);
            _player.Weapons.Clear();
            _player.Weapons.Add(_shotgunWeapon);
        }

        // HUD 초기화
        _hud?.UpdateHP(maxHP, maxHP);
        _hud?.UpdateExp(0, 1, 1);
        _hud?.UpdateTimer(0f);
        _hud?.UpdateKillCount(0);
    }

    private IEnumerable<WeaponBase> GetWeapons()
    {
        if (_shotgunWeapon != null) yield return _shotgunWeapon;
        if (_orbWeapon     != null) yield return _orbWeapon;
        if (_missileWeapon != null) yield return _missileWeapon;
    }

    private void OnDestroy()
    {
        _joystick?.Unregister();
        _expOrbPool?.Dispose();
    }
}
