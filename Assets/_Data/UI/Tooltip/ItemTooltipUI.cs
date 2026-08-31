using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltipUI : BaseMonoBehaviour
{
    private static ItemTooltipUI current;

    [Header("Tooltip UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text setText;

    private RectTransform panelRect;
    private Canvas rootCanvas;
    private bool isVisible;
    protected override void OnDestroy()
    {
        if (current == this)
            current = null;
    }
    public static void Show(ItemDefinition item, EquipmentInstanceData instance, Canvas sourceCanvas, Vector2 screenPosition)
    {
        if (item == null || sourceCanvas == null) return;

        ItemTooltipUI tooltip = FindAnyObjectByType<ItemTooltipUI>(FindObjectsInactive.Include);
        if (tooltip == null)
        {
            Debug.LogError("ItemTooltipUI was not found. Add it to your ItemTooltip panel.");
            return;
        }

        Canvas canvas = sourceCanvas.rootCanvas;
        tooltip.Prepare(canvas);
        tooltip.Populate(item, instance);
        tooltip.gameObject.SetActive(true);
        tooltip.isVisible = true;
        tooltip.SetPosition(screenPosition, sourceCanvas.worldCamera);
        current = tooltip;
    }

    public static void Move(Vector2 screenPosition, Canvas sourceCanvas)
    {
        if (current != null && current.isVisible && sourceCanvas != null)
            current.SetPosition(screenPosition, sourceCanvas.worldCamera);
    }

    public static void Hide()
    {
        if (current == null) return;
        current.isVisible = false;
        current.gameObject.SetActive(false);
    }

    private void Prepare(Canvas canvas)
    {
        panelRect = transform as RectTransform;
        rootCanvas = canvas;

        if (transform.parent != canvas.transform)
            transform.SetParent(canvas.transform, false);

        transform.SetAsLastSibling();
        panelRect.pivot = new Vector2(0f, 1f);

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void Populate(ItemDefinition item, EquipmentInstanceData instance)
    {
        Color rarityColor = GetRarityColor(item.Rarity);
        string upgrade = instance != null && instance.UpgradeLevel > 0 ? $" +{instance.UpgradeLevel}" : string.Empty;

        if (iconImage != null)
        {
            iconImage.sprite = item.Icon;
            iconImage.enabled = item.Icon != null;
            iconImage.preserveAspect = true;
        }

        if (titleText != null)
            titleText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(rarityColor)}>{item.DisplayName}{upgrade}</color>";

        if (typeText != null)
        {
            typeText.text = $"{item.Rarity}  •  {GetItemType(item)}";
            typeText.color = rarityColor;
        }

        SetOptionalText(statsText, BuildStats(item, instance), new Color(.55f, 1f, .68f));
        SetOptionalText(setText, BuildSetText(item), new Color(.54f, .88f, 1f));
    }

    private void SetPosition(Vector2 screenPosition, Camera sourceCamera)
    {
        if (panelRect == null || rootCanvas == null) return;

        Camera camera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : sourceCamera;
        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out Vector2 point)) return;

        Canvas.ForceUpdateCanvases();
        const float offset = 18f;
        Vector2 size = panelRect.rect.size;
        Rect bounds = canvasRect.rect;
        Vector2 position = point + new Vector2(offset, -offset);

        if (position.x + size.x > bounds.xMax)
            position.x = point.x - offset - size.x;
        if (position.y - size.y < bounds.yMin)
            position.y = point.y + offset + size.y;

        position.x = Mathf.Clamp(position.x, bounds.xMin, bounds.xMax - size.x);
        position.y = Mathf.Clamp(position.y, bounds.yMin + size.y, bounds.yMax);
        panelRect.anchoredPosition = position;
    }

    private static void SetOptionalText(TMP_Text text, string value, Color color)
    {
        if (text == null) return;
        text.text = value;
        text.color = color;
        text.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
    }

    private static string BuildStats(ItemDefinition item, EquipmentInstanceData instance)
    {
        if (item.Category != ItemCategory.Equipment) return string.Empty;

        List<StatModifier> modifiers = instance != null ? instance.BuildModifiers(item) : item.BuildEquipmentModifiers(0);
        StringBuilder result = new();
        foreach (StatModifier modifier in modifiers)
        {
            if (modifier.StatType == StatType.None || Mathf.Approximately(modifier.Amount, 0f)) continue;
            if (result.Length > 0) result.Append('\n');
            result.Append(FormatModifier(modifier));
        }

        return result.ToString();
    }

    private static string BuildSetText(ItemDefinition item)
    {
        EquipmentSetDefinition set = item.EquipmentSet;
        if (set == null) return string.Empty;

        int equippedPieces = 0;
        PlayerEquipmentManager manager = PlayerEquipmentManager.InstanceOrNull;
        if (manager != null)
            foreach (EquipmentSetProgress progress in manager.GetSetProgresses())
                if (progress.Set == set && progress.Rarity == item.Rarity)
                {
                    equippedPieces = progress.EquippedPieceCount;
                    break;
                }

        StringBuilder result = new($"{set.DisplayName} {item.Rarity} Set ({equippedPieces} pieces)");
        foreach (EquipmentSetBonus bonus in set.Bonuses)
        {
            if (bonus == null) continue;
            string detail = string.IsNullOrWhiteSpace(bonus.Description) ? BuildBonusModifiers(bonus) : bonus.Description;
            result.Append($"\n{(equippedPieces >= bonus.RequiredPieceCount ? "✓" : "○")} ({bonus.RequiredPieceCount}) {detail}");
        }

        return result.ToString();
    }

    private static string BuildBonusModifiers(EquipmentSetBonus bonus)
    {
        StringBuilder result = new();
        foreach (StatModifierData modifier in bonus.Modifiers)
        {
            if (modifier == null || modifier.StatType == StatType.None) continue;
            if (result.Length > 0) result.Append(", ");
            result.Append(FormatModifier(modifier.StatType, modifier.ModifierType, modifier.Amount));
        }

        return result.Length > 0 ? result.ToString() : "Set Bonus";
    }

    private static string GetItemType(ItemDefinition item) => item.Category == ItemCategory.Equipment ? item.EquipmentSlotType.ToString() : item.Category.ToString();
    private static string FormatModifier(StatModifier modifier) => FormatModifier(modifier.StatType, modifier.ModifierType, modifier.Amount);
    private static string FormatModifier(StatType type, ModifierType modifierType, float amount)
    {
        string suffix = modifierType == ModifierType.Flat ? string.Empty : "%";
        return $"{(amount > 0 ? "+" : string.Empty)}{amount:0.##}{suffix} {FormatStatName(type)}";
    }

    private static string FormatStatName(StatType type) => type switch
    {
        StatType.Attack => "Attack", StatType.Armor => "Armor", StatType.MaxHealth => "Max Health", StatType.MaxMana => "Max Mana",
        StatType.MoveSpeed => "Move Speed", StatType.AttackSpeed => "Attack Speed", StatType.CritChance => "Critical Chance",
        StatType.CritDamage => "Critical Damage", StatType.HealthRegen => "Health Regeneration", StatType.ManaRegen => "Mana Regeneration", _ => type.ToString()
    };

    private static Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => new Color(.75f, .78f, .82f), ItemRarity.Uncommon => new Color(.35f, .88f, .48f),
        ItemRarity.Rare => new Color(.36f, .62f, 1f), ItemRarity.Epic => new Color(.75f, .43f, 1f),
        ItemRarity.Legendary => new Color(1f, .72f, .25f), _ => Color.white
    };
}
