using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 레벨업 시 무기 선택 패널 (UIToolkit).
/// SurvivorRunData와 무기별 최대 레벨(WeaponData.Levels.Length)을 참고해 카드를 제시합니다.
/// </summary>
public class UpgradePanel
{
    private readonly VisualElement _panel;
    private readonly Label         _titleLabel;
    private readonly Label         _subLabel;
    private readonly VisualElement _cardsContainer;

    public event Action<string> OnWeaponSelected; // "shotgun" / "orb" / "missile"

    private static readonly (string id, string name, string icon, string iconClass, string desc)[] WeaponDefs =
    {
        ("shotgun", "샷건",  "🔫", "weapon-icon-shotgun", "3방향 산탄 — 레벨업 시 발사 수 +1"),
        ("orb",     "오브",  "🔵", "weapon-icon-orb",     "회전 구체 — 레벨업 시 구체 수 +1"),
        ("missile", "미사일","🚀", "weapon-icon-missile",  "유도 추적 — 레벨업 시 데미지 +50%"),
    };

    public UpgradePanel(VisualElement root)
    {
        _panel          = root.Q("upgrade-panel");
        _titleLabel     = root.Q<Label>("upgrade-title");
        _subLabel       = root.Q<Label>("upgrade-sub");
        _cardsContainer = root.Q("upgrade-cards");
    }

    /// <summary>
    /// 무기 카드를 제시합니다. 강화 가능한 무기가 없으면 패널 없이 바로 재개합니다.
    /// maxLevels: 무기 id → 최대 레벨 (WeaponData.Levels.Length 기반)
    /// </summary>
    public void Show(SurvivorRunData runData, int newLevel, Dictionary<string, int> maxLevels)
    {
        if (_panel == null) return;

        bool hasCards = BuildCards(runData, maxLevels);
        if (!hasCards)
        {
            // 모든 무기 최대 레벨 → 패널 표시 없이 즉시 재개
            OnWeaponSelected?.Invoke("");
            return;
        }

        if (_titleLabel != null) _titleLabel.text = "무기를 선택하세요";
        if (_subLabel   != null) _subLabel.text   = $"레벨업! Lv.{newLevel - 1} → Lv.{newLevel}";
        _panel.RemoveFromClassList("hidden");
    }

    public void Hide()
    {
        _panel?.AddToClassList("hidden");
    }

    // 카드가 하나라도 있으면 true 반환
    private bool BuildCards(SurvivorRunData runData, Dictionary<string, int> maxLevels)
    {
        if (_cardsContainer == null) return false;
        _cardsContainer.Clear();

        // 최대 레벨에 도달한 무기 제외
        var available = new List<(string id, string name, string icon, string iconClass, string desc)>();
        foreach (var w in WeaponDefs)
        {
            int currentLv = GetWeaponLevel(w.id, runData);
            int maxLv     = maxLevels != null && maxLevels.TryGetValue(w.id, out int m) ? m : 3;
            if (currentLv < maxLv) available.Add(w);
        }

        if (available.Count == 0) return false;

        // 최대 3장
        int count = Mathf.Min(3, available.Count);
        for (int i = 0; i < count; i++)
        {
            var w         = available[i];
            int currentLv = GetWeaponLevel(w.id, runData);
            int maxLv     = maxLevels != null && maxLevels.TryGetValue(w.id, out int m2) ? m2 : 3;
            bool isNew    = currentLv == 0;

            var card = new VisualElement();
            card.AddToClassList("upgrade-card");
            card.AddToClassList(isNew ? "new" : "evolve");

            // 아이콘
            var iconWrap = new VisualElement();
            iconWrap.AddToClassList("weapon-icon");
            iconWrap.AddToClassList(w.iconClass);
            var iconLabel = new Label(w.icon);
            iconLabel.AddToClassList("weapon-icon-label");
            iconWrap.Add(iconLabel);

            // 정보
            var info = new VisualElement();
            info.AddToClassList("weapon-info");

            var nameLabel = new Label(w.name);
            nameLabel.AddToClassList("weapon-name");

            var levelLabel = new Label(isNew ? "NEW" : $"Lv.{currentLv} → Lv.{currentLv + 1}");
            levelLabel.AddToClassList("weapon-level");
            levelLabel.AddToClassList(isNew ? "new" : "evolve");

            var descLabel = new Label(w.desc);
            descLabel.AddToClassList("weapon-desc");

            info.Add(nameLabel);
            info.Add(levelLabel);
            info.Add(descLabel);

            card.Add(iconWrap);
            card.Add(info);

            // 클릭 이벤트
            string weaponId = w.id;
            card.RegisterCallback<ClickEvent>(_ => OnCardClicked(weaponId));

            _cardsContainer.Add(card);
        }

        return true;
    }

    private void OnCardClicked(string weaponId)
    {
        Hide();
        OnWeaponSelected?.Invoke(weaponId);
    }

    private static int GetWeaponLevel(string id, SurvivorRunData data)
    {
        return id switch
        {
            "shotgun" => data.ShotgunLevel,
            "orb"     => data.OrbLevel,
            "missile" => data.MissileLevel,
            _         => 0,
        };
    }
}
