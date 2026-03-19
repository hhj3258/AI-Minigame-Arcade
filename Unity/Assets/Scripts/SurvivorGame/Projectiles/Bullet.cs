using System;
using UnityEngine;

/// <summary>
/// 샷건 발사체. 직선 이동 후 OnTriggerEnter2D로 데미지 처리.
/// ObjectPool로 관리됩니다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Bullet : MonoBehaviour
{
    public float    Speed    { get; set; } = 12f;
    public float    Damage   { get; set; } = 10f;
    public float    Lifetime { get; set; } = 2f;

    public Action<Bullet> OnReturn; // Pool 반환 콜백

    private Rigidbody2D _rb;
    private float       _timer;
    private bool        _returned;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale   = 0f;
        _rb.freezeRotation = true;

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = ColliderSizeUtil.GetSpriteRadius(gameObject, 0.1f);
    }

    public void Launch(Vector2 direction)
    {
        _timer             = Lifetime;
        _returned          = false;
        _rb.linearVelocity  = direction.normalized * Speed;
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            ReturnToPool();
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
        OnReturn?.Invoke(this);
    }
}
