using System;
using UnityEngine;

/// <summary>
/// 적 사망 시 드랍되는 EXP 오브. 플레이어에 가까워지면 자석처럼 흡수됩니다.
/// ObjectPool로 관리됩니다.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class ExpOrb : MonoBehaviour
{
    public int  ExpValue { get; set; }

    public Action<ExpOrb> OnReturn;

    private Transform _playerTransform;
    private bool      _attracting;
    private float     _attractSpeed = 8f;
    private float     _attractRange = 2f;

    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = 0.15f;
    }

    public void Initialize(int expValue, Transform playerTransform)
    {
        ExpValue         = expValue;
        _playerTransform = playerTransform;
        _attracting      = false;
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, _playerTransform.position);

        if (dist <= _attractRange)
            _attracting = true;

        if (_attracting)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                _playerTransform.position,
                _attractSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        OnReturn?.Invoke(this);
    }
}
