using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 적의 추상 기반 클래스.
/// 플레이어를 향해 이동하며 체력, EXP, 데미지를 정의합니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public abstract class EnemyBase : MonoBehaviour
{
    // ── 활성 적 목록 (FindNearest용) ────────────────
    private static readonly List<EnemyBase> _activeEnemies = new List<EnemyBase>();

    public static EnemyBase FindNearest(Vector2 origin)
    {
        EnemyBase nearest  = null;
        float     minDist  = float.MaxValue;

        foreach (var e in _activeEnemies)
        {
            if (e == null || !e.gameObject.activeInHierarchy) continue;
            float d = Vector2.Distance(origin, e.transform.position);
            if (d < minDist) { minDist = d; nearest = e; }
        }
        return nearest;
    }

    public static void ClearAll()
    {
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] != null)
                _activeEnemies[i].gameObject.SetActive(false);
        }
        _activeEnemies.Clear();
    }

    // ── 데이터 ──────────────────────────────────────
    protected EnemyData _data;

    public int   CurrentHP { get; protected set; }
    public bool  IsDead    => CurrentHP <= 0;
    public int   ExpValue  => _data != null ? _data.ExpValue : 0;

    public event Action<EnemyBase> OnDied; // EnemySpawner에서 구독

    protected Rigidbody2D _rb;
    private   Transform   _playerTransform;
    private   float       _contactDamageAccum; // 소수 데미지 누산기

    // ── 초기화 ──────────────────────────────────────
    public virtual void Initialize(EnemyData data, Transform playerTransform)
    {
        _data                = data;
        _playerTransform     = playerTransform;
        CurrentHP            = data.MaxHP;
        OnDied               = null; // 풀 재사용 시 이벤트 핸들러 누적 방지
        _contactDamageAccum  = 0f;

        if (_rb == null)
            _rb = GetComponent<Rigidbody2D>();

        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        transform.localScale = Vector3.one * _data.Size;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = ColliderSizeUtil.GetSpriteRadius(gameObject, GetColliderRadius());

        _activeEnemies.Add(this);
    }

    protected virtual float GetColliderRadius() => 0.3f;

    private void OnDisable()
    {
        _activeEnemies.Remove(this);
    }

    // ── 업데이트 ──────────────────────────────────
    private void FixedUpdate()
    {
        if (_data == null || _playerTransform == null || IsDead) return;

        Vector2 dir = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * _data.MoveSpeed;
    }

    // ── 데미지 ────────────────────────────────────
    public void TakeDamage(int amount)
    {
        if (IsDead) return;
        CurrentHP -= amount;
        if (CurrentHP <= 0)
            Die();
    }

    protected virtual void Die()
    {
        CurrentHP = 0;
        _rb.linearVelocity = Vector2.zero;
        OnDied?.Invoke(this);
    }

    // ── 플레이어 접촉 데미지 ─────────────────────
    // ContactDamage는 초당 데미지. float 누산기로 소수 손실 없이 처리.
    private void OnTriggerStay2D(Collider2D other)
    {
        if (_data == null || IsDead) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        _contactDamageAccum += _data.ContactDamage * Time.fixedDeltaTime;
        if (_contactDamageAccum >= 1f)
        {
            int dmg = (int)_contactDamageAccum;
            _contactDamageAccum -= dmg;
            player.TakeDamage(dmg);
        }
    }
}
