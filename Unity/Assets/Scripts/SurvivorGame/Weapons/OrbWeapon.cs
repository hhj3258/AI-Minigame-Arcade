using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 오브 무기. 플레이어 주변을 회전하는 구체를 레벨 수만큼 생성합니다.
/// 레벨업 시 구체 수 +1. FireRate 타이머 미사용 (상시 활성).
/// </summary>
public class OrbWeapon : WeaponBase
{
    [SerializeField] private GameObject _orbPrefab;

    private readonly List<OrbProjectile> _orbs = new List<OrbProjectile>();

    protected override void OnInitialize()
    {
        if (_orbPrefab == null)
        {
            Debug.LogError("OrbWeapon: OrbPrefab이 할당되지 않았습니다.", this);
            return;
        }
        RefreshOrbs();
    }

    protected override void OnLevelUp()    => RefreshOrbs();

    protected override void OnActivate()   => SetOrbsActive(true);
    protected override void OnDeactivate() => SetOrbsActive(false);

    protected override void Fire() { /* 오브는 항상 활성 상태 — 발화 불필요 */ }

    private void RefreshOrbs()
    {
        if (PlayerTransform == null) return;

        var stat   = CurrentStat;
        int count  = stat != null ? stat.projectileCount : Level;
        float dmg  = stat != null ? stat.damage : 15f;

        // 부족한 오브 추가
        while (_orbs.Count < count)
        {
            var go  = Instantiate(_orbPrefab, PlayerTransform.position, Quaternion.identity, ProjectileRoot);
            var orb = go.GetComponent<OrbProjectile>();
            orb.Target        = PlayerTransform;
            var orbData = _data as OrbWeaponData;
            orb.OrbitalRadius = orbData != null ? orbData.OrbitalRadius  : 1.5f;
            orb.RotateSpeed   = orbData != null ? orbData.OrbRotateSpeed : 180f;
            _orbs.Add(orb);
        }

        // 넘치는 오브 제거
        while (_orbs.Count > count)
        {
            Destroy(_orbs[_orbs.Count - 1].gameObject);
            _orbs.RemoveAt(_orbs.Count - 1);
        }

        // 각도 균등 배분 + 데미지 + 크기 설정
        float step = count > 0 ? 360f / count : 0f;
        float size = stat != null ? stat.size : 1f;
        for (int i = 0; i < _orbs.Count; i++)
        {
            _orbs[i].Angle                        = step * i;
            _orbs[i].Damage                       = dmg;
            _orbs[i].transform.localScale         = Vector3.one * size;
            _orbs[i].gameObject.SetActive(true);
        }
    }

    private void SetOrbsActive(bool active)
    {
        foreach (var orb in _orbs)
        {
            if (orb != null)
                orb.gameObject.SetActive(active);
        }
    }

    private void OnDestroy()
    {
        foreach (var orb in _orbs)
        {
            if (orb != null)
                Destroy(orb.gameObject);
        }
        _orbs.Clear();
    }
}
