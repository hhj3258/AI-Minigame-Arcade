using System;
using UnityEngine;

/// <summary>
/// 가장 가까운 적을 추적하는 미사일 발사체.
/// ObjectPool로 관리됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class MissileProjectile : MonoBehaviour
{
    public float    Speed    { get; set; } = 8f;
    public float    Damage   { get; set; } = 20f;
    public float    Lifetime { get; set; } = 4f;

    public Action<MissileProjectile> OnReturn;

    private Rigidbody2D _rb;
    private EnemyBase   _target;
    private float       _timer;
    private bool        _returned;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = ColliderSizeUtil.GetSpriteRadius(gameObject, 0.12f);
    }

    public void Launch(EnemyBase target)
    {
        _target   = target;
        _timer    = Lifetime;
        _returned = false;

        if (_target != null)
        {
            Vector2 dir = ((Vector2)_target.transform.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * Speed;
        }
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            ReturnToPool();
            return;
        }

        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            // 타겟 소실 시 가장 가까운 적으로 재탐색
            _target = EnemyBase.FindNearest(transform.position);
        }

        if (_target != null)
        {
            Vector2 dir = ((Vector2)_target.transform.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * Speed;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
        enemy.TakeDamage((int)Damage);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (_returned) return;
        _returned          = true;
        _rb.linearVelocity = Vector2.zero;
        _target            = null;
        OnReturn?.Invoke(this);
    }
}
