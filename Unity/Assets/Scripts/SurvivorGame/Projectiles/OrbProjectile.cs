using UnityEngine;

/// <summary>
/// 플레이어 주변을 회전하는 오브 발사체.
/// OrbWeapon이 생성 및 각도 제어합니다.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class OrbProjectile : MonoBehaviour
{
    public float    Damage { get; set; } = 15f;
    public Transform Target { get; set; }  // 플레이어 Transform

    public float OrbitalRadius  { get; set; } = 1.5f;
    public float RotateSpeed    { get; set; } = 180f; // 초당 각도

    public float Angle { get; set; } // 현재 각도(도)

    private void Awake()
    {
        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius    = ColliderSizeUtil.GetSpriteRadius(gameObject, 0.18f);
    }

    private void Update()
    {
        if (Target == null) return;

        Angle += RotateSpeed * Time.deltaTime;
        if (Angle >= 360f) Angle -= 360f;

        float rad = Angle * Mathf.Deg2Rad;
        transform.position = (Vector2)Target.position
            + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * OrbitalRadius;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
        enemy.TakeDamage((int)Damage);
    }
}
