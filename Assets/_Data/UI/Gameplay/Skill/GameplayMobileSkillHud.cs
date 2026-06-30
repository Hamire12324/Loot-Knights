using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameplayMobileSkillHud : BaseMonoBehaviour
{
    private const string HudName = "MobileSkillHud";

    [SerializeField] private bool rebuildOnStart = true;
    [SerializeField] private Vector2 anchoredPosition = new(-112f, 102f);
    [SerializeField] private float attackSize = 92f;
    [SerializeField] private float skillSize = 62f;
    [SerializeField] private float skillRadius = 102f;
    [SerializeField] private float skillArcStartAngle = 180f;
    [SerializeField] private float skillArcEndAngle = 90f;

    protected override void Start()
    {
        base.Start();

        if (!rebuildOnStart) return;

        Build();
    }

    public void Build()
    {
        Clear();
        ArrangeExistingButtons();
    }

    [ContextMenu("Arrange Existing Skill Buttons")]
    private void BuildFromContextMenu()
    {
        ArrangeExistingButtons();
    }

    [ContextMenu("Clear Generated Mobile Skill HUD")]
    private void ClearFromContextMenu()
    {
        Clear();
    }

    public void ArrangeExistingButtons()
    {
        Clear();

        RectTransform attack = FindRect("Btn_Attack", "Btn_Attack_Basic");
        RectTransform skill0 = FindRect("Btn_Skill", "Btn_Skill_GroundWave");
        RectTransform skill1 = FindRect("Btn_Skill (1)", "Btn_Skill_IronGuard", "Btn_Skill_ShieldBash");
        RectTransform skill2 = FindRect("Btn_Skill (2)", "Btn_Skill_Whirlwind");
        RectTransform skill3 = FindRect("Btn_Skill (3)", "Btn_Skill_ChargeStrike");

        SetupAttackButton(attack, "Btn_Attack_Basic", Vector2.zero, attackSize);
        SetupSkillButton(skill0, "Btn_Skill_GroundWave", 0, SkillArcPosition(0, 4));
        SetupSkillButton(skill1, "Btn_Skill_IronGuard", 1, SkillArcPosition(1, 4));
        SetupSkillButton(skill2, "Btn_Skill_Whirlwind", 2, SkillArcPosition(2, 4));
        SetupSkillButton(skill3, "Btn_Skill_ChargeStrike", 3, SkillArcPosition(3, 4));
    }

    public void Clear()
    {
        Transform oldHud = transform.Find(HudName);
        if (oldHud == null) return;

        if (Application.isPlaying)
            Destroy(oldHud.gameObject);
        else
            DestroyImmediate(oldHud.gameObject);
    }

    private void SetupAttackButton(RectTransform rect, string objectName, Vector2 localOffset, float size)
    {
        if (rect == null) return;

        rect.gameObject.name = objectName;
        PlaceButton(rect, localOffset, size);

        if (rect.GetComponent<Button>() == null)
            rect.gameObject.AddComponent<Button>();

        if (rect.GetComponent<ButtonAttack>() == null)
            rect.gameObject.AddComponent<ButtonAttack>();
    }

    private void SetupSkillButton(RectTransform rect, string objectName, int skillIndex, Vector2 localOffset)
    {
        if (rect == null) return;

        rect.gameObject.name = objectName;
        PlaceButton(rect, localOffset, skillSize);

        if (rect.GetComponent<Button>() == null)
            rect.gameObject.AddComponent<Button>();

        ButtonHeroSkill buttonHeroSkill = rect.GetComponent<ButtonHeroSkill>();
        if (buttonHeroSkill == null)
            buttonHeroSkill = rect.gameObject.AddComponent<ButtonHeroSkill>();

        buttonHeroSkill.SetSkillIndex(skillIndex);
    }

    private void PlaceButton(RectTransform rect, Vector2 localOffset, float size)
    {
        rect.gameObject.SetActive(true);
        rect.SetParent(transform, false);
        rect.anchorMin = Vector2.one;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition + localOffset;
        rect.sizeDelta = new Vector2(size, size);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private RectTransform FindRect(params string[] objectNames)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null) continue;
            if (!NameMatches(child.name, objectNames)) continue;

            return child as RectTransform;
        }

        Debug.LogWarning($"{nameof(GameplayMobileSkillHud)}: Cannot find {string.Join(" or ", objectNames)}.", gameObject);
        return null;
    }

    private static bool NameMatches(string candidate, string[] names)
    {
        foreach (string name in names)
        {
            if (candidate == name)
                return true;
        }

        return false;
    }

    private void CreateButton(
        RectTransform parent,
        string name,
        bool basicAttack,
        int skillIndex,
        Vector2 position,
        float size,
        string fallbackLabel,
        Color backgroundColor)
    {
        RectTransform buttonRect = CreateGraphicRect(name, parent, new Vector2(1f, 0f), new Vector2(1f, 0f), position, new Vector2(size, size));
        buttonRect.pivot = new Vector2(0.5f, 0.5f);

        UICircleGraphic background = buttonRect.gameObject.AddComponent<UICircleGraphic>();
        background.color = backgroundColor;
        background.raycastTarget = true;

        UICircleGraphic border = CreateCircle("Frame", buttonRect, Vector2.zero, new Vector2(size, size), new Color(0.92f, 0.84f, 0.62f, 0.95f));
        border.InnerRadius = 0.82f;
        border.raycastTarget = false;

        RectTransform iconRect = CreateGraphicRect("Icon", buttonRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.one * size * 0.58f);
        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        TMP_Text label = CreateText("Label", buttonRect, fallbackLabel, size <= 70f ? 18 : 22, new Color(0.96f, 0.91f, 0.78f, 1f));
        label.raycastTarget = false;

        UICircleGraphic cooldown = CreateCircle("CooldownOverlay", buttonRect, Vector2.zero, new Vector2(size, size), new Color(0f, 0f, 0f, 0.58f));
        cooldown.FillAmount = 0f;
        cooldown.raycastTarget = false;

        TMP_Text cooldownText = CreateText("CooldownText", buttonRect, "", size <= 70f ? 20 : 26, Color.white);
        cooldownText.raycastTarget = false;

        MobileSkillButton skillButton = buttonRect.gameObject.AddComponent<MobileSkillButton>();
        skillButton.Setup(basicAttack, skillIndex, icon, cooldown, cooldownText, label);
    }

    private static UICircleGraphic CreateCircle(string name, RectTransform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = CreateGraphicRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        UICircleGraphic circle = rect.gameObject.AddComponent<UICircleGraphic>();
        circle.color = color;
        return circle;
    }

    private static TMP_Text CreateText(string name, RectTransform parent, string text, int fontSize, Color color)
    {
        RectTransform rect = CreateGraphicRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, parent.sizeDelta);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.enableAutoSizing = true;
        label.fontSizeMin = 10f;
        label.fontSizeMax = fontSize;
        return label;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, Vector2 size)
    {
        GameObject obj = new(name, typeof(RectTransform));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchored;
        rect.sizeDelta = size;
        return rect;
    }

    private static RectTransform CreateGraphicRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchored, Vector2 size)
    {
        GameObject obj = new(name, typeof(RectTransform), typeof(CanvasRenderer));
        obj.layer = parent.gameObject.layer;
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchored;
        rect.sizeDelta = size;
        return rect;
    }

    private Vector2 Polar(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * skillRadius;
    }

    private Vector2 SkillArcPosition(int index, int count)
    {
        if (count <= 1)
            return Polar(skillArcStartAngle);

        float t = Mathf.Clamp01((float)index / (count - 1));
        float angle = Mathf.Lerp(skillArcStartAngle, skillArcEndAngle, t);
        return Polar(angle);
    }

    private void HideLegacyButtons()
    {
        HideIfFound("Btn_Skill");
        HideIfFound("Btn_Skill (1)");
        HideIfFound("Btn_Skill (2)");
        HideIfFound("Btn_Attack");
    }

    private void HideIfFound(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child == null || child == transform) continue;
            if (child.name != objectName) continue;

            child.gameObject.SetActive(false);
        }
    }
}
