using UnityEditor;
using UnityEngine;

public static class ReorganizeDataFolder
{
    [MenuItem("Tools/Reorganize Data Folder")]
    public static void Reorganize()
    {
        // 대상 폴더 생성
        CreateFolder("Assets/Data", "SurvivorGame");
        CreateFolder("Assets/Data/SurvivorGame", "Enemy");
        CreateFolder("Assets/Data/SurvivorGame", "Weapon");
        CreateFolder("Assets/Data/SurvivorGame", "Settings");

        // Enemy 에셋 이동
        Move("Assets/Data/SlimeData.asset",       "Assets/Data/SurvivorGame/Enemy/SlimeData.asset");
        Move("Assets/Data/BatData.asset",         "Assets/Data/SurvivorGame/Enemy/BatData.asset");
        Move("Assets/Data/GolemData.asset",        "Assets/Data/SurvivorGame/Enemy/GolemData.asset");
        Move("Assets/Data/MiniBossData.asset",     "Assets/Data/SurvivorGame/Enemy/MiniBossData.asset");

        // Weapon 에셋 이동
        Move("Assets/Data/ShotgunData.asset",     "Assets/Data/SurvivorGame/Weapon/ShotgunData.asset");
        Move("Assets/Data/OrbData.asset",         "Assets/Data/SurvivorGame/Weapon/OrbData.asset");
        Move("Assets/Data/MissileData.asset",     "Assets/Data/SurvivorGame/Weapon/MissileData.asset");

        // Settings 에셋 이동
        Move("Assets/Data/SurvivorSettings.asset","Assets/Data/SurvivorGame/Settings/SurvivorSettings.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ReorganizeDataFolder] 완료");
    }

    static void CreateFolder(string parent, string name)
    {
        string path = parent + "/" + name;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }

    static void Move(string from, string to)
    {
        if (!System.IO.File.Exists(from)) { Debug.LogWarning($"[ReorganizeDataFolder] 파일 없음: {from}"); return; }
        string err = AssetDatabase.MoveAsset(from, to);
        if (!string.IsNullOrEmpty(err))
            Debug.LogError($"[ReorganizeDataFolder] 이동 실패 {from} → {to}: {err}");
    }
}
