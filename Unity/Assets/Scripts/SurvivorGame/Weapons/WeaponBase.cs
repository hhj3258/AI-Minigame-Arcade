using UnityEngine;

/// <summary>
/// 모든 무기의 추상 기반 클래스.
/// 발화 타이머를 관리하며 Fire()를 주기적으로 호출합니다.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected WeaponData _data;

    public int  Level    { get; private set; } = 1;
    public int  MaxLevel => _data != null ? _data.Levels.Length : 3;
    public bool IsActive { get; private set; }

    protected Transform PlayerTransform  { get; private set; }
    protected Transform ProjectileRoot  { get; private set; }

    private float _fireTimer;

    protected WeaponData.LevelStat CurrentStat =>
        (_data != null && Level - 1 < _data.Levels.Length)
            ? _data.Levels[Level - 1]
            : null;

    /// <summary>
    /// 무기 초기화. SurvivorGame에서 호출. _data는 인스펙터에서 미리 연결.
    /// </summary>
    public virtual void Initialize(Transform playerTransform, Transform projectileRoot = null)
    {
        PlayerTransform = playerTransform;
        ProjectileRoot  = projectileRoot;
        Level           = 1;
        IsActive        = true;
        _fireTimer      = 0f;

        OnInitialize();
    }

    public void LevelUp()
    {
        if (Level >= MaxLevel) return;
        Level++;
        OnLevelUp();
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        if (active)  OnActivate();
        else         OnDeactivate();
    }

    private void Update()
    {
        if (!IsActive || PlayerTransform == null || CurrentStat == null) return;

        _fireTimer -= Time.deltaTime;
        if (_fireTimer <= 0f)
        {
            _fireTimer = 1f / Mathf.Max(0.01f, CurrentStat.fireRate);
            Fire();
        }
    }

    protected abstract void Fire();
    protected virtual void OnInitialize() { }
    protected virtual void OnLevelUp()    { }
    protected virtual void OnActivate()   { }
    protected virtual void OnDeactivate() { }
}
