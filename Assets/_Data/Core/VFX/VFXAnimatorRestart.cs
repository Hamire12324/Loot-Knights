using UnityEngine;

/// <summary>Restarts a sprite-sheet Animator whenever its pooled VFX object is reused.</summary>
[RequireComponent(typeof(Animator))]
public sealed class VFXAnimatorRestart : MonoBehaviour
{
    private Animator animatorComponent;

    private void Awake()
    {
        animatorComponent = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        if (animatorComponent == null)
            animatorComponent = GetComponent<Animator>();

        animatorComponent.Rebind();
        animatorComponent.Play(0, 0, 0f);
        animatorComponent.Update(0f);
    }
}
