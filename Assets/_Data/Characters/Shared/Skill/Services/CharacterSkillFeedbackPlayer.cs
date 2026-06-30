using UnityEngine;

public static class CharacterSkillFeedbackPlayer
{
    public static void PlayCastFeedback(
        CharacterCtrl characterCtrl,
        CharacterSkillDefinition definition,
        Vector2 direction)
    {
        if (characterCtrl == null || definition == null) return;

        Vector3 position = characterCtrl.transform.position;

        if (definition.CastVfx != null && VFXManager.HasInstance)
            VFXManager.InstanceOrNull.Play(definition.CastVfx, position, direction, characterCtrl.transform);

        if (definition.CastSfx != null)
            SFXManager.Play(definition.CastSfx, position);
    }
}
