using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SurvivorSettings", menuName = "AIMiniArcade/SurvivorSettings")]
public class SurvivorSettings : ScriptableObject
{
    [Header("웨이브")]
    [Tooltip("순서대로 진행되는 웨이브 목록. 마지막 웨이브가 끝나면 반복됩니다.")]
    public WaveConfig[] Waves;

    [Header("미니보스")]
    public float BossSpawnInterval = 120f;

    [Header("EXP")]
    public int[] ExpThresholds;
}

[Serializable]
public class WaveConfig
{
    [Tooltip("이 웨이브가 지속되는 시간(초)")]
    public float Duration = 30f;

    [Tooltip("웨이브 동안 스폰할 슬라임 수")]
    public int SlimeCount = 5;

    [Tooltip("웨이브 동안 스폰할 배트 수")]
    public int BatCount = 3;

    [Tooltip("웨이브 동안 스폰할 골렘 수")]
    public int GolemCount = 1;

    public int TotalCount => SlimeCount + BatCount + GolemCount;
}
