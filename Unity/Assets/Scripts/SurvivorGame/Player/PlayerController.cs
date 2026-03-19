using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[RequireComponent(typeof(CircleCollider2D))]

/// <summary>
/// 뱀서라이크 플레이어. Rigidbody2D 이동, HP 관리, 무기 목록 보유.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("무기 프리팹 (Addressable)")]
    [SerializeField] private AssetReference _shotgunRef;
    [SerializeField] private AssetReference _orbRef;
    [SerializeField] private AssetReference _missileRef;

    // 로드된 무기 인스턴스
    public ShotgunWeapon ShotgunWeapon { get; private set; }
    public OrbWeapon     OrbWeapon     { get; private set; }
    public MissileWeapon MissileWeapon { get; private set; }

    // 외부에서 설정
    public float        MoveSpeed  { get; set; } = 5f;
    public VirtualJoystick Joystick { get; set; }

    // HP
    public int  MaxHP      { get; private set; }
    public int  CurrentHP  { get; private set; }
    public bool IsDead     => CurrentHP <= 0;

    public event Action<int, int> OnHPChanged;   // (currentHP, maxHP)
    public event Action           OnDead;

    // 무기
    public List<WeaponBase> Weapons { get; } = new List<WeaponBase>();

    private Rigidbody2D     _rb;
    private CircleCollider2D _col;
    private bool             _active;

    private void Awake()
    {
        _rb  = GetComponent<Rigidbody2D>();
        _col = GetComponent<CircleCollider2D>();
    }

    public void Initialize(int maxHP)
    {
        if (_rb == null) _rb = GetComponent<Rigidbody2D>();
        MaxHP  = maxHP;
        CurrentHP = maxHP;
        _active = true;

        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;
        _rb.interpolation  = RigidbodyInterpolation2D.Interpolate;

        if (_col != null)
        {
            _col.isTrigger = false;
            _col.radius    = ColliderSizeUtil.GetSpriteRadius(gameObject, 0.3f);
        }
    }

    private void FixedUpdate()
    {
        if (!_active || Joystick == null)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        _rb.linearVelocity = Joystick.Direction * MoveSpeed;
    }

    public void TakeDamage(int amount)
    {
        if (!_active || IsDead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnHPChanged?.Invoke(CurrentHP, MaxHP);

        if (IsDead)
        {
            _active = false;
            _rb.linearVelocity = Vector2.zero;
            OnDead?.Invoke();
        }
    }

    public void SetActive(bool active)
    {
        _active = active;
        if (!active && _rb != null)
            _rb.linearVelocity = Vector2.zero;
    }

    // ── 무기 Addressable 로드 ──────────────────────────────
    public async UniTask LoadWeaponsAsync()
    {
        ShotgunWeapon = await LoadWeapon<ShotgunWeapon>(_shotgunRef);
        OrbWeapon     = await LoadWeapon<OrbWeapon>(_orbRef);
        MissileWeapon = await LoadWeapon<MissileWeapon>(_missileRef);
    }

    private async UniTask<T> LoadWeapon<T>(AssetReference assetRef) where T : WeaponBase
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
        {
            Debug.LogError($"PlayerController: {typeof(T).Name} AssetReference가 설정되지 않았습니다.", this);
            return null;
        }

        AsyncOperationHandle<GameObject> handle = assetRef.LoadAssetAsync<GameObject>();
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"PlayerController: {typeof(T).Name} 로드 실패.", this);
            return null;
        }

        var go = Instantiate(handle.Result, transform);
        return go.GetComponent<T>();
    }

    private void OnDestroy()
    {
        if (_shotgunRef != null && _shotgunRef.IsValid()) _shotgunRef.ReleaseAsset();
        if (_orbRef     != null && _orbRef.IsValid())     _orbRef.ReleaseAsset();
        if (_missileRef != null && _missileRef.IsValid()) _missileRef.ReleaseAsset();
    }
}
