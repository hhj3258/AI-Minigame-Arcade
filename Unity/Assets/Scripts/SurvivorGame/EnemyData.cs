using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "AIMiniArcade/EnemyData")]
public class EnemyData : ScriptableObject
{
    public int   MaxHP;
    public float MoveSpeed;
    public int   ExpValue;
    public int   PoolSize = 20;
    public float ContactDamage = 10f;
    public float Size = 1f; // 스프라이트 및 콜라이더 크기 배율
}
