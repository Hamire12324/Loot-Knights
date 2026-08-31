using UnityEngine;
using UnityEngine.UI;

public partial class GameplayMobileSkillHud : BaseMonoBehaviour
{
    protected override void Start()
    {
        base.Start();

        LoadElementalIconSet();
        BindExistingButtons();
        LoadElementMeterReferences();
        BindElementMeterSlotButtons();
        LoadElementReleaseIconReference();
        RefreshElementMeter();
    }

    protected override void Update()
    {
        base.Update();

        if (Application.isPlaying)
        {
            RefreshElementMeter();
        }
    }

    private void BindExistingButtons()
    {
        BindAttackButton(FindRect("Btn_Attack_Basic", "Btn_Attack"));
        BindSkillButton(FindRect("Btn_Skill_GroundWave", "Btn_Skill"), 0);
        BindSkillButton(FindRect("Btn_Skill_IronGuard", "Btn_Skill_ShieldBash", "Btn_Skill (1)"), 1);
        BindSkillButton(FindRect("Btn_Skill_Whirlwind", "Btn_Skill (2)"), 2);
        BindSkillButton(FindRect("Btn_Skill_ChargeStrike", "Btn_Skill (3)"), 3);
        BindElementButton(FindRect("Btn_Skill_ElementConduit"), release: true);
        BindAddAllElementsButton(FindRect("Btn_AddAllElements", "Button_AddAllElements"));
    }

    private static void BindAttackButton(RectTransform rect)
    {
        if (rect == null) return;

        GetOrAddComponent<Button>(rect);
        GetOrAddComponent<ButtonAttack>(rect);
    }

    private static void BindSkillButton(RectTransform rect, int skillIndex)
    {
        if (rect == null) return;

        GetOrAddComponent<Button>(rect);
        ButtonHeroSkill skillButton = GetOrAddComponent<ButtonHeroSkill>(rect);
        skillButton.SetSkillIndex(skillIndex);
        ConfigureAimInput(skillButton);
    }

    private static void BindElementButton(RectTransform rect, bool release)
    {
        if (rect == null) return;

        GetOrAddComponent<Button>(rect);
        ButtonHeroSkill skillButton = GetOrAddComponent<ButtonHeroSkill>(rect);

        if (release)
        {
            skillButton.SetElementRelease();
        }
        else
        {
            skillButton.SetElementAbsorb();
        }

        ConfigureAimInput(skillButton);
    }

    private static void ConfigureAimInput(ButtonHeroSkill skillButton)
    {
        if (skillButton != null)
        {
            GetOrAddComponent<MobileSkillAimInput>(skillButton).SetSkillButton(skillButton);
        }
    }

    private static void BindAddAllElementsButton(RectTransform rect)
    {
        if (rect == null) return;

        GetOrAddComponent<Button>(rect);
        GetOrAddComponent<ButtonAddAllElements>(rect);
    }

    private static T GetOrAddComponent<T>(Component target) where T : Component
    {
        return target.TryGetComponent(out T component)
            ? component
            : target.gameObject.AddComponent<T>();
    }

    private RectTransform FindRect(params string[] objectNames)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child == null) continue;

            foreach (string objectName in objectNames)
            {
                if (child.name == objectName)
                {
                    return child as RectTransform;
                }
            }
        }

        return null;
    }
}

public sealed class ButtonAddAllElements : ButtonAbstract
{
    protected override void OnClick()
    {
        HeroCtrl hero = HeroCtrl.GetLocal();
        if (hero == null || hero.HeroSkillController == null) return;
        if (!hero.HeroSkillController.AddAllElementConduitForTesting()) return;

        GameplayMobileSkillHud hud = GetComponentInParent<GameplayMobileSkillHud>();
        if (hud == null)
        {
            hud = FindAnyObjectByType<GameplayMobileSkillHud>();
        }

        hud?.RefreshElementMeterNow();
    }
}
