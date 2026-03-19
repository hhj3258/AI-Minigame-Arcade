using UnityEngine;

/// <summary>
/// 스프라이트 크기 기반으로 CircleCollider2D 반지름을 계산하는 유틸리티.
/// sprite.bounds는 로컬 공간 기준이며, CircleCollider2D.radius도 로컬 공간이므로 scale 보정 불필요.
/// </summary>
public static class ColliderSizeUtil
{
    /// <summary>
    /// SpriteRenderer의 sprite.bounds.extents(min)를 반지름으로 반환.
    /// 스프라이트가 없으면 fallback 값을 반환합니다.
    /// </summary>
    public static float GetSpriteRadius(GameObject go, float fallback = 0.3f)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return fallback;

        Vector2 extents = sr.sprite.bounds.extents;
        return Mathf.Min(extents.x, extents.y);
    }
}
