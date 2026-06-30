using UnityEngine;

public class HeroLevel : CharacterLevel
{
    public HeroCtrl Hero => characterCtrl as HeroCtrl;

    protected override void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<HeroCtrl>(true);

        if (characterCtrl == null)
            Debug.LogError($"There is no HeroCtrl in {gameObject.name}", gameObject);
    }
}
