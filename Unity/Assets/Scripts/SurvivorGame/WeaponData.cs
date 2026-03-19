using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "AIMiniArcade/WeaponData")]
public class WeaponData : ScriptableObject
{
    [System.Serializable]
    public class LevelStat
    {
        public float damage;
        public float fireRate;        // 초당 발사 횟수
        public int   projectileCount; // 샷건: 발사 수, 오브: 구체 수
        public float size = 1f;       // 발사체 크기
    }

    public string   WeaponName;
    public string   Description;
    public string   Icon;          // 이모지 (🔫 / 🔵 / 🚀)
    public LevelStat[] Levels;     // index 0 = Lv.1
}
