using UnityEngine;

public sealed class CharacterSkillFacing
{
    private readonly CharacterCtrl characterCtrl;
    private Vector3 originalScale;
    private bool castFacingOverrideActive;

    public CharacterSkillFacing(CharacterCtrl characterCtrl)
    {
        this.characterCtrl = characterCtrl;

        if (characterCtrl != null)
            originalScale = characterCtrl.transform.localScale;
    }

    public void FaceCastDirection(Vector2 direction)
    {
        if (characterCtrl == null || Mathf.Abs(direction.x) <= 0.01f)
            return;

        if (originalScale == Vector3.zero)
            originalScale = characterCtrl.transform.localScale;

        Vector3 scale = originalScale;
        scale.x = direction.x >= 0f ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        characterCtrl.transform.localScale = scale;
        castFacingOverrideActive = true;
    }

    public void RestoreWhenAttackVisualEnds(bool attackVisualActive)
    {
        if (!castFacingOverrideActive) return;
        if (attackVisualActive) return;

        RestoreOriginalScale();
    }

    public void RestoreOriginalScale()
    {
        if (!castFacingOverrideActive || characterCtrl == null)
            return;

        characterCtrl.transform.localScale = originalScale;
        castFacingOverrideActive = false;
    }
}
