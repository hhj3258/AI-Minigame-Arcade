using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UObject = UnityEngine.Object;

/// <summary>
/// Survivor 게임 런타임 디버그 에디터 윈도우 (UI Toolkit 기반).
/// 로직 코드를 수정하지 않고 리플렉션으로 내부 상태에 접근합니다.
/// </summary>
public class SurvivorDebugWindow : EditorWindow
{
    [MenuItem("Tools/서바이버 디버그 창")]
    public static void OpenWindow()
    {
        var wnd = GetWindow<SurvivorDebugWindow>("Survivor Debug");
        wnd.minSize = new Vector2(300, 580);
    }

    private const BindingFlags Priv = BindingFlags.NonPublic | BindingFlags.Instance;

    // 반복 조회 방지용 FieldInfo 캐시
    private static readonly FieldInfo EnemyPlayerTransformField =
        typeof(EnemyBase).GetField("_playerTransform", Priv);

    // ── 라이브 레이블 ──────────────────────────────────────
    private Label  _statusLabel;
    private Label  _waveLabel;
    private Label  _hpLabel;
    private Label  _killLabel;
    private Label  _timeLabel;
    private Label  _expLabel;
    private Label  _timeScaleLabel;
    private Slider _timeScaleSlider;
    private bool   _freezeEnemies;

    // ── 상태 버튼 ────────────────────────────────────────
    private Button _toggleAllBtn;
    private Button _shotgunBtn;
    private Button _orbBtn;
    private Button _missileBtn;
    private Button _spawnerBtn;
    private Button _freezeBtn;

    // ── CreateGUI ─────────────────────────────────────────
    public void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.paddingLeft = root.style.paddingRight =
        root.style.paddingTop  = root.style.paddingBottom = 10;

        root.Add(SectionTitle("SURVIVOR DEBUG"));

        _statusLabel = InfoLabel("상태: --");
        root.Add(_statusLabel);
        root.Add(Divider());

        // 게임 정보
        root.Add(SectionTitle("게임 정보"));
        _waveLabel = InfoLabel("웨이브: --");
        _hpLabel   = InfoLabel("HP: --");
        _killLabel = InfoLabel("킬: --");
        _timeLabel = InfoLabel("경과: --");
        _expLabel  = InfoLabel("EXP: --");
        root.Add(_waveLabel);
        root.Add(_hpLabel);
        root.Add(_killLabel);
        root.Add(_timeLabel);
        root.Add(_expLabel);
        root.Add(Divider());

        // 무기
        root.Add(SectionTitle("무기 상태"));
        root.Add(Space(4));
        _toggleAllBtn = StateBtn("전체", ToggleAllWeapons);
        root.Add(_toggleAllBtn);
        var weaponRow = Row();
        _shotgunBtn = StateBtn("샷건", () => ToggleWeapon<ShotgunWeapon>());
        _orbBtn     = StateBtn("오브",  () => ToggleWeapon<OrbWeapon>());
        _missileBtn = StateBtn("미사일", () => ToggleWeapon<MissileWeapon>());
        weaponRow.Add(_shotgunBtn);
        weaponRow.Add(_orbBtn);
        weaponRow.Add(_missileBtn);
        root.Add(weaponRow);
        root.Add(Divider());

        // 에너미
        root.Add(SectionTitle("에너미"));
        var enemyRow = Row();
        _spawnerBtn = StateBtn("웨이브 스폰", ToggleEnemySpawner);
        _freezeBtn  = StateBtn("이동",        ToggleEnemyFreeze);
        enemyRow.Add(_spawnerBtn);
        enemyRow.Add(_freezeBtn);
        root.Add(enemyRow);
        root.Add(Space(4));
        var waveRow = Row();
        waveRow.Add(ActionBtn("◀ 이전 웨이브", () => ChangeWave(-1)));
        waveRow.Add(ActionBtn("다음 웨이브 ▶", () => ChangeWave(+1)));
        root.Add(waveRow);
        root.Add(ActionBtn("모든 적 제거", () => EnemyBase.ClearAll()));
        root.Add(Divider());

        // 플레이어
        root.Add(SectionTitle("플레이어"));
        var playerRow = Row();
        playerRow.Add(ActionBtn("HP 최대 회복", HealPlayer));
        playerRow.Add(ActionBtn("강제 레벨업",  ForceAddExp));
        root.Add(playerRow);
        root.Add(Divider());

        // 게임
        root.Add(SectionTitle("게임"));
        root.Add(ActionBtn("재시작", RestartGame));
        root.Add(Space(4));

        // 게임 속도
        _timeScaleLabel  = InfoLabel("게임 속도: 1.0x");
        _timeScaleSlider = new Slider(0.1f, 3f) { value = 1f };
        _timeScaleSlider.RegisterValueChangedCallback(e =>
        {
            Time.timeScale       = e.newValue;
            _timeScaleLabel.text = $"게임 속도: {e.newValue:F1}x";
        });
        root.Add(_timeScaleLabel);
        root.Add(_timeScaleSlider);
    }

    // ── 업데이트 (에디터 루프) ─────────────────────────────
    private void Update()
    {
        if (_statusLabel == null) return;

        if (!Application.isPlaying)
        {
            _statusLabel.text = "상태: 플레이 모드 아님";
            return;
        }

        _statusLabel.text = "상태: 실행 중 ▶";

        // PlayerController
        var player = Find<PlayerController>();
        _hpLabel.text = player != null
            ? $"HP: {player.CurrentHP} / {player.MaxHP}"
            : "HP: --";

        // SurvivorGame → _runData, _elapsed, _levelSystem
        var game = Find<SurvivorGame>();
        if (game != null)
        {
            var runData = Fld<SurvivorRunData>(game, "_runData");
            var elapsed = Fld<float>(game, "_elapsed");
            var ls      = Fld<LevelSystem>(game, "_levelSystem");

            _killLabel.text = runData != null ? $"킬: {runData.KillCount}" : "킬: --";
            _timeLabel.text = $"경과: {elapsed:F0}s";
            _expLabel.text  = ls != null
                ? $"EXP: {ls.CurrentExp} / {ls.MaxExp}  (Lv {ls.Level})"
                : "EXP: --";
        }

        // EnemySpawner → _waveIndex
        var spawner = Find<EnemySpawner>();
        if (spawner != null)
        {
            int waveIdx = Fld<int>(spawner, "_waveIndex") + 1;
            bool active = Fld<bool>(spawner, "_active");
            _waveLabel.text = $"웨이브: {waveIdx}  [{(active ? "ON" : "OFF")}]";
        }
        else
        {
            _waveLabel.text = "웨이브: --";
        }

        // 무기 버튼 상태 업데이트
        UpdateWeaponBtn<ShotgunWeapon>(_shotgunBtn,  "샷건");
        UpdateWeaponBtn<OrbWeapon>    (_orbBtn,      "오브");
        UpdateWeaponBtn<MissileWeapon>(_missileBtn,  "미사일");
        UpdateToggleAllBtn();

        // 에너미 버튼 상태 업데이트
        UpdateStateBtn(_spawnerBtn, "웨이브 스폰", GetSpawnerActive());
        UpdateStateBtn(_freezeBtn,  "이동",        !_freezeEnemies);

        // 에너미 이동 정지 — FixedUpdate가 velocity를 덮어쓰므로 _playerTransform을 null로 유지
        if (_freezeEnemies)
        {
            foreach (var e in UObject.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                EnemyPlayerTransformField?.SetValue(e, null);
                var rb = e.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }

        Repaint();
    }

    // ── 액션 ──────────────────────────────────────────────
    private static void ToggleWeapon<T>() where T : WeaponBase
    {
        var w = Find<T>();
        if (w != null) w.SetActive(!w.IsActive);
    }

    private static void ToggleAllWeapons()
    {
        var shotgun = Find<ShotgunWeapon>();
        var orb     = Find<OrbWeapon>();
        var missile = Find<MissileWeapon>();

        // 하나라도 ON이면 전체 OFF, 모두 OFF면 전체 ON
        bool anyOn = (shotgun?.IsActive ?? false) ||
                     (orb?.IsActive     ?? false) ||
                     (missile?.IsActive ?? false);
        bool next = !anyOn;

        shotgun?.SetActive(next);
        orb?.SetActive(next);
        missile?.SetActive(next);
    }

    private void ToggleEnemyFreeze()
    {
        _freezeEnemies = !_freezeEnemies;

        // OFF → 살아있는 모든 에너미의 _playerTransform 복구
        if (!_freezeEnemies)
        {
            var pc = Find<PlayerController>();
            var playerT = pc != null ? pc.transform : null;
            foreach (var e in UObject.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                EnemyPlayerTransformField?.SetValue(e, playerT);
        }
    }

    private static void ToggleEnemySpawner()
    {
        var s = Find<EnemySpawner>();
        if (s == null) return;
        s.SetActive(!Fld<bool>(s, "_active"));
    }

    private static void ChangeWave(int delta)
    {
        var s = Find<EnemySpawner>();
        if (s == null) return;

        var waves = Fld<WaveConfig[]>(s, "_waves");
        if (waves == null || waves.Length == 0) return;

        int cur  = Fld<int>(s, "_waveIndex");
        int next = (cur + delta + waves.Length) % waves.Length;

        // private StartWave(int) 호출
        s.GetType().GetMethod("StartWave", Priv)?.Invoke(s, new object[] { next });
    }

    private static void HealPlayer()
    {
        var p = Find<PlayerController>();
        if (p == null) return;

        // private set CurrentHP 접근
        typeof(PlayerController)
            .GetProperty("CurrentHP")
            ?.GetSetMethod(nonPublic: true)
            ?.Invoke(p, new object[] { p.MaxHP });

        // HP 변경 이벤트 발행 (private 메서드 없으므로 TakeDamage(0)으로 트리거)
        p.TakeDamage(0);
    }

    private static void ForceAddExp()
    {
        var game = Find<SurvivorGame>();
        if (game == null) return;
        Fld<LevelSystem>(game, "_levelSystem")?.AddExp(9999);
    }

    private static void RestartGame()
    {
        var game = Find<SurvivorGame>();
        if (game == null) return;
        typeof(SurvivorGame)
            .GetMethod("OnRestartClicked", Priv)
            ?.Invoke(game, null);
    }

    // ── 헬퍼 ──────────────────────────────────────────────
    private static readonly Color OnColor  = new(0.25f, 0.60f, 0.25f);
    private static readonly Color OffColor = new(0.22f, 0.22f, 0.22f);

    private static void UpdateWeaponBtn<T>(Button btn, string name) where T : WeaponBase
    {
        if (btn == null) return;
        var w = Find<T>();
        if (w == null)
        {
            btn.text = $"{name} (없음)";
            btn.style.backgroundColor = new StyleColor(OffColor);
        }
        else if (w.IsActive)
        {
            btn.text = $"{name} Lv{w.Level} ON";
            btn.style.backgroundColor = new StyleColor(OnColor);
        }
        else
        {
            btn.text = $"{name} Lv{w.Level} OFF";
            btn.style.backgroundColor = new StyleColor(OffColor);
        }
    }

    private static void UpdateStateBtn(Button btn, string label, bool isOn)
    {
        if (btn == null) return;
        btn.text = isOn ? $"{label} ON" : $"{label} OFF";
        btn.style.backgroundColor = new StyleColor(isOn ? OnColor : OffColor);
    }

    private static bool GetSpawnerActive()
    {
        var s = Find<EnemySpawner>();
        return s != null && Fld<bool>(s, "_active");
    }

    private void UpdateToggleAllBtn()
    {
        if (_toggleAllBtn == null) return;
        var shotgun = Find<ShotgunWeapon>();
        var orb     = Find<OrbWeapon>();
        var missile = Find<MissileWeapon>();
        bool anyOn  = (shotgun != null && shotgun.IsActive) ||
                      (orb     != null && orb.IsActive)     ||
                      (missile != null && missile.IsActive);
        _toggleAllBtn.text = anyOn ? "전체 ON" : "전체 OFF";
        _toggleAllBtn.style.backgroundColor = new StyleColor(anyOn ? OnColor : OffColor);
    }

    private static T Find<T>() where T : UObject
        => UObject.FindFirstObjectByType<T>();

    private static T Fld<T>(object obj, string name)
    {
        var fi = obj.GetType().GetField(name, Priv);
        return fi != null ? (T)fi.GetValue(obj) : default;
    }

    // ── UI 빌더 ───────────────────────────────────────────
    private static Label SectionTitle(string text)
    {
        var l = new Label(text);
        l.style.unityFontStyleAndWeight = FontStyle.Bold;
        l.style.fontSize  = 12;
        l.style.color     = new StyleColor(new Color(0.85f, 0.78f, 1f));
        l.style.marginTop = 4;
        return l;
    }

    private static Label InfoLabel(string text)
    {
        var l = new Label(text);
        l.style.color       = new StyleColor(Color.white);
        l.style.marginLeft  = 8;
        return l;
    }

    private static Button ActionBtn(string text, Action onClick)
    {
        var b = new Button(onClick) { text = text };
        b.style.flexGrow     = 1;
        b.style.marginBottom = 2;
        return b;
    }

    // ON/OFF 상태를 색상으로 표현하는 버튼 (초기값 OFF)
    private static Button StateBtn(string label, Action onClick)
    {
        var b = new Button(onClick) { text = $"{label} OFF" };
        b.style.flexGrow          = 1;
        b.style.marginBottom      = 2;
        b.style.backgroundColor   = new StyleColor(OffColor);
        return b;
    }

    private static VisualElement Row()
    {
        var e = new VisualElement();
        e.style.flexDirection = FlexDirection.Row;
        return e;
    }

    private static VisualElement Divider()
    {
        var e = new VisualElement();
        e.style.height          = 1;
        e.style.backgroundColor = new StyleColor(new Color(0.35f, 0.35f, 0.35f));
        e.style.marginTop = e.style.marginBottom = 6;
        return e;
    }

    private static VisualElement Space(float h)
    {
        var e = new VisualElement();
        e.style.height = h;
        return e;
    }
}
