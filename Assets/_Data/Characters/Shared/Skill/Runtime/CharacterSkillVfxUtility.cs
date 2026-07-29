using UnityEngine;

public static class CharacterSkillVfxUtility
{
    public static PoolObj Play(VFXDefinition definition, Vector3 position, Vector2 direction, Transform parent = null)
    {
        if (definition == null || !VFXManager.HasInstance)
            return null;

        return VFXManager.InstanceOrNull.Play(definition, position, direction, parent);
    }

    public static PoolObj PlayProjectile(
        VFXDefinition definition,
        Vector3 position,
        Vector2 direction,
        float distance,
        float speed,
        float rotationOffsetDegrees = 0f)
    {
        PoolObj projectile = Play(definition, position, direction);
        if (projectile == null)
            return null;

        VFXProjectileMover mover = projectile.GetComponent<VFXProjectileMover>();
        if (mover == null)
            mover = projectile.gameObject.AddComponent<VFXProjectileMover>();

        mover.Play(direction, distance, speed, rotationOffsetDegrees);
        return projectile;
    }
}
