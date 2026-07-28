using System.Collections;
using UnityEngine;

public class CharacterDamageFlash : CharacterAbstract
{
    [Header("Damage Flash")]
    [SerializeField] private bool flashEnabled = true;
    [SerializeField] private Color flashColor = new(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private float flashDuration = 0.08f;

    private SpriteRenderer[] spriteRenderers;
    private Color[] originalSpriteColors;
    private Coroutine flashCoroutine;

    protected override void Awake()
    {
        base.Awake();
        LoadSpriteRenderers();
    }

    protected override void OnDisable()
    {
        RestoreSpriteColors();
        StopFlashCoroutine();
        base.OnDisable();
    }

    public void Play()
    {
        if (!flashEnabled) return;

        if (spriteRenderers == null || spriteRenderers.Length == 0)
            LoadSpriteRenderers();

        if (spriteRenderers == null || spriteRenderers.Length == 0) return;

        StopFlashCoroutine();
        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    private IEnumerator FlashCoroutine()
    {
        SetSpriteColors(flashColor);
        yield return new WaitForSeconds(flashDuration);
        RestoreSpriteColors();
        flashCoroutine = null;
    }

    private void LoadSpriteRenderers()
    {
        spriteRenderers = characterCtrl != null
            ? characterCtrl.GetComponentsInChildren<SpriteRenderer>(true)
            : GetComponentsInChildren<SpriteRenderer>(true);

        originalSpriteColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            originalSpriteColors[i] = spriteRenderers[i].color;
    }

    private void SetSpriteColors(Color color)
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].color = color;
        }
    }

    private void RestoreSpriteColors()
    {
        if (spriteRenderers == null || originalSpriteColors == null) return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            if (i >= originalSpriteColors.Length) continue;

            spriteRenderers[i].color = originalSpriteColors[i];
        }
    }

    private void StopFlashCoroutine()
    {
        if (flashCoroutine == null) return;

        StopCoroutine(flashCoroutine);
        flashCoroutine = null;
    }
}
