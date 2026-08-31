using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public sealed class PlayerNameHud : MonoBehaviour
{
    private TMP_Text label;
    private HeroCtrl hero;
    private string displayedName;

    private void Awake() => label = GetComponent<TMP_Text>();

    private void OnEnable()
    {
        hero = null;
        displayedName = null;
        Refresh();
    }

    private void OnDisable()
    {
        hero = null;
        displayedName = null;
    }

    private void Update()
    {
        HeroCtrl localHero = HeroCtrl.GetLocal();
        if (localHero != hero)
        {
            hero = localHero;
            displayedName = null;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (label == null || hero == null) return;

        string nextName = !string.IsNullOrWhiteSpace(hero.Profile?.CharacterName)
            ? hero.Profile.CharacterName
            : hero.name;

        if (nextName == displayedName) return;

        displayedName = nextName;
        label.text = displayedName;
    }
}
