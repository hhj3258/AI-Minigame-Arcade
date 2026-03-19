using UnityEditor;
using UnityEngine;

public static class WireWeaponRefs
{
    [MenuItem("Tools/Wire Weapon & Spawner Refs")]
    public static void Wire()
    {
        // ── 에셋 로드 ─────────────────────────────────────
        var shotgunData  = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/SurvivorGame/Weapon/ShotgunData.asset");
        var orbData      = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/SurvivorGame/Weapon/OrbData.asset");
        var missileData  = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/Data/SurvivorGame/Weapon/MissileData.asset");
        var settings     = AssetDatabase.LoadAssetAtPath<SurvivorSettings>("Assets/Data/SurvivorGame/Settings/SurvivorSettings.asset");

        var slimeData    = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/SurvivorGame/Enemy/SlimeData.asset");
        var batData      = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/SurvivorGame/Enemy/BatData.asset");
        var golemData    = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/SurvivorGame/Enemy/GolemData.asset");
        var bossData     = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/Data/SurvivorGame/Enemy/MiniBossData.asset");

        var slimePrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/SlimeEnemy.prefab");
        var batPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BatEnemy.prefab");
        var golemPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GolemEnemy.prefab");
        var bossPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MiniBossEnemy.prefab");
        var bulletPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");
        var orbPrefab     = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/OrbProjectile.prefab");
        var missilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MissileProjectile.prefab");
        var expOrbPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExpOrb.prefab");

        // ── Player 무기 컴포넌트 연결 ─────────────────────
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("[WireWeaponRefs] Player not found"); return; }

        SetRef(player.GetComponent<ShotgunWeapon>(), "_data", shotgunData);
        SetRef(player.GetComponent<ShotgunWeapon>(), "_bulletPrefab", bulletPrefab);
        SetRef(player.GetComponent<OrbWeapon>(),     "_data", orbData);
        SetRef(player.GetComponent<OrbWeapon>(),     "_orbPrefab", orbPrefab);
        SetRef(player.GetComponent<MissileWeapon>(), "_data", missileData);
        SetRef(player.GetComponent<MissileWeapon>(), "_missilePrefab", missilePrefab);
        EditorUtility.SetDirty(player);

        // ── EnemySpawner 연결 ─────────────────────────────
        var spawnerGO = GameObject.Find("EnemySpawner");
        if (spawnerGO != null)
        {
            var spawner = spawnerGO.GetComponent<EnemySpawner>();
            if (spawner != null)
            {
                SetRef(spawner, "_slimePrefab",  slimePrefab);
                SetRef(spawner, "_batPrefab",    batPrefab);
                SetRef(spawner, "_golemPrefab",  golemPrefab);
                SetRef(spawner, "_slimeData",    slimeData);
                SetRef(spawner, "_batData",      batData);
                SetRef(spawner, "_golemData",    golemData);
                EditorUtility.SetDirty(spawnerGO);
            }
        }

        // ── BossSpawner 연결 ──────────────────────────────
        var bossSpawnerGO = GameObject.Find("BossSpawner");
        if (bossSpawnerGO != null)
        {
            var bossSpawner = bossSpawnerGO.GetComponent<BossSpawner>();
            if (bossSpawner != null)
            {
                SetRef(bossSpawner, "_bossPrefab", bossPrefab);
                SetRef(bossSpawner, "_bossData",   bossData);
                EditorUtility.SetDirty(bossSpawnerGO);
            }
        }

        // ── SurvivorGame 연결 ─────────────────────────────
        var gameCard1 = GameObject.Find("GameCard_1");
        if (gameCard1 != null)
        {
            var survivorGame = gameCard1.GetComponent<SurvivorGame>();
            if (survivorGame != null)
            {
                SetRef(survivorGame, "_settings",       settings);
                SetRef(survivorGame, "_shotgunData",    shotgunData);
                SetRef(survivorGame, "_orbData",        orbData);
                SetRef(survivorGame, "_missileData",    missileData);
                SetRef(survivorGame, "_expOrbPrefab",   expOrbPrefab);

                var survivorCam = GameObject.Find("SurvivorCamera");
                if (survivorCam != null)
                    SetRef(survivorGame, "_survivorCamera", survivorCam.GetComponent<Camera>());

                var playerCtrl = player.GetComponent<PlayerController>();
                if (playerCtrl != null)
                    SetRef(survivorGame, "_player", playerCtrl);

                if (spawnerGO != null) SetRef(survivorGame, "_enemySpawner", spawnerGO.GetComponent<EnemySpawner>());
                if (bossSpawnerGO != null) SetRef(survivorGame, "_bossSpawner", bossSpawnerGO.GetComponent<BossSpawner>());

                SetRef(survivorGame, "_shotgunWeapon", player.GetComponent<ShotgunWeapon>());
                SetRef(survivorGame, "_orbWeapon",     player.GetComponent<OrbWeapon>());
                SetRef(survivorGame, "_missileWeapon", player.GetComponent<MissileWeapon>());

                EditorUtility.SetDirty(gameCard1);
            }
        }

        Debug.Log("[WireWeaponRefs] 모든 레퍼런스 연결 완료");
    }

    static void SetRef(Object target, string propName, Object value)
    {
        if (target == null) { Debug.LogWarning($"[WireWeaponRefs] target null (prop={propName})"); return; }
        var so   = new SerializedObject(target);
        var prop = so.FindProperty(propName);
        if (prop == null) { Debug.LogWarning($"[WireWeaponRefs] property not found: {propName} on {target.GetType().Name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
