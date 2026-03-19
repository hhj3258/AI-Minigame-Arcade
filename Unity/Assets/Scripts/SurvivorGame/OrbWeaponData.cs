using UnityEngine;

[CreateAssetMenu(fileName = "OrbWeaponData", menuName = "AIMiniArcade/OrbWeaponData")]
public class OrbWeaponData : WeaponData
{
    [Header("오브 무기 전용")]
    [Tooltip("오브가 플레이어로부터 회전하는 반경")]
    public float OrbitalRadius  = 1.5f;
    [Tooltip("오브 회전 속도 (초당 각도)")]
    public float OrbRotateSpeed = 180f;
}
