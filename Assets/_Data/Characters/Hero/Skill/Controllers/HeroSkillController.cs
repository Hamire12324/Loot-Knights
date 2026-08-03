using UnityEngine;

public class HeroSkillController : CharacterSkillController
{
    public HeroCtrl Hero => characterCtrl as HeroCtrl;

    protected override void LoadCharacterCtrl()
    {
        if (characterCtrl != null) return;

        characterCtrl = GetComponentInParent<HeroCtrl>(true);

        if (characterCtrl == null)
            Debug.LogError($"There is no HeroCtrl in {gameObject.name}", gameObject);
    }

    protected override void SetMovementLocked(bool locked)
    {
        base.SetMovementLocked(locked);

        HeroMovement movement = characterCtrl != null ? characterCtrl.CharacterMovement as HeroMovement : null;
        movement?.SetInputEnabled(!locked);
    }

    protected override bool CanCastSpecialSkillRuntime(CharacterSkillRuntime runtime)
    {
        if (GetElementalConduitEffect(runtime) == null)
            return base.CanCastSpecialSkillRuntime(runtime);

        return CanReleaseElementConduit();
    }

    public void OnSkill1() => TryCast(0);
    public void OnSkill2() => TryCast(1);
    public void OnSkill3() => TryCast(2);
    public void OnSkill4() => TryCast(3);
    public void OnElementSkill() => TryReleaseElementConduit();
    public void OnElementAbsorb() => TryAbsorbElementConduit();

    public bool TryAbsorbElementConduit()
    {
        ElementalSkillConduitEffect effect = GetElementalConduitEffect();
        if (effect == null || characterCtrl == null)
            return false;

        Transform target = CharacterSkillTargeting.FindTarget(characterCtrl);
        Vector2 direction = CharacterSkillTargeting.GetAimDirection(characterCtrl, target);
        return effect.AbsorbShards(characterCtrl, direction);
    }

    public bool TryCollectElementShard(ElementalShardPickup shard)
    {
        ElementalSkillConduitEffect effect = GetElementalConduitEffect();
        if (effect == null || characterCtrl == null || shard == null)
            return false;

        return effect.CollectShard(characterCtrl, shard);
    }

    public bool AddAllElementConduitForTesting()
    {
        ElementalSkillConduitEffect effect = GetElementalConduitEffect();
        return effect != null && effect.AddAllElementsForTesting(characterCtrl);
    }

    public bool TryReleaseElementConduit()
    {
        if (!CanReleaseElementConduit(logReason: true))
            return false;

        ElementalSkillConduitEffect effect = GetElementalConduitEffect();
        if (!TryCastSpecialSkill())
            return false;

        effect?.PrepareRelease(characterCtrl);
        return true;
    }

    public bool CanReleaseElementConduit()
    {
        return CanReleaseElementConduit(logReason: false);
    }

    public bool TryGetElementalConduitReleasePreview(out ElementalConduitReleasePayload preview)
    {
        preview = default;

        ElementalSkillConduitEffect effect = GetElementalConduitEffect();
        return effect != null && effect.TryGetReleasePreview(characterCtrl, out preview);
    }

    private bool CanReleaseElementConduit(bool logReason)
    {
        ElementalSkillConduitEffect effect = GetElementalConduitEffect();
        if (effect == null)
        {
            if (logReason)
                Debug.LogWarning($"{nameof(HeroSkillController)}: Elemental Conduit special skill is not equipped.", gameObject);

            return false;
        }

        bool canRelease = effect.CanRelease(characterCtrl, out string reason);
        if (!canRelease && logReason)
        {
            Debug.LogWarning(
                $"{nameof(HeroSkillController)}: Cannot release Elemental Conduit. {reason}",
                gameObject);
        }

        return canRelease;
    }

    private ElementalSkillConduitEffect GetElementalConduitEffect()
    {
        return GetElementalConduitEffect(GetSpecialSkill());
    }

    private ElementalSkillConduitEffect GetElementalConduitEffect(CharacterSkillRuntime runtime)
    {
        CharacterSkillDefinition definition = runtime != null ? runtime.Definition : null;
        if (definition == null || definition.Effects == null)
            return null;

        foreach (CharacterSkillEffectDefinition effect in definition.Effects)
        {
            if (effect is ElementalSkillConduitEffect conduitEffect)
                return conduitEffect;
        }

        return null;
    }
}
