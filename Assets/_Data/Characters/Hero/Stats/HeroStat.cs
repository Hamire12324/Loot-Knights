using UnityEngine;

public class HeroStat : CharacterStat
{
    // The character-menu fallback and the gameplay prefab must start from the
    // same baseline. Otherwise a player who sees 100 HP in the menu enters a
    // stage with a different value before any progression is applied.
    public override void InitBaseStats()
    {
        HeroCtrl hero = characterCtrl as HeroCtrl;
        CharacterClass characterClass = hero != null && hero.Profile != null
            ? hero.Profile.CharacterClass
            : CharacterClass.Knight;

        MaxHealth.BaseValue = characterClass == CharacterClass.Ranger ? 90f : 100f;
    }
}
