using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "AIMiniArcade/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("기본 스탯")]
    public int   MaxHP     = 100;
    public float MoveSpeed = 5f;
    public float Size      = 1f;
}
