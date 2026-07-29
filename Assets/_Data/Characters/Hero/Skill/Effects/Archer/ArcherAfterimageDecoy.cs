using System.Collections.Generic;
using UnityEngine;

public sealed class ArcherAfterimageDecoy : MonoBehaviour
{
    private static readonly List<ArcherAfterimageDecoy> Active = new();
    private static readonly Color DecoyColor = new(0.45f, 0.85f, 1f, 0.45f);

    public CharacterCtrl Owner { get; private set; }
    public bool IsExpired => Time.time >= expireTime;

    private float expireTime;
    private float spawnTime;
    private SpriteRenderer[] renderers;

    public static ArcherAfterimageDecoy Create(CharacterCtrl owner, Vector3 position, float duration)
    {
        GameObject instance = new("ArcherAfterimageDecoy");
        instance.transform.position = position;

        ArcherAfterimageDecoy decoy = instance.AddComponent<ArcherAfterimageDecoy>();
        decoy.Owner = owner;
        decoy.spawnTime = Time.time;
        decoy.expireTime = Time.time + Mathf.Max(0.1f, duration);
        decoy.CreateVisual(owner);
        Active.Add(decoy);
        return decoy;
    }

    public static Transform FindClosest(Vector3 position, float radius)
    {
        Active.RemoveAll(decoy => decoy == null || decoy.IsExpired);

        Transform closest = null;
        float bestDistance = Mathf.Max(0f, radius);
        for (int i = 0; i < Active.Count; i++)
        {
            ArcherAfterimageDecoy decoy = Active[i];
            if (decoy == null)
                continue;

            float distance = Vector2.Distance(position, decoy.transform.position);
            if (distance > bestDistance)
                continue;

            bestDistance = distance;
            closest = decoy.transform;
        }

        return closest;
    }

    public static bool IsValidTarget(Transform target)
    {
        ArcherAfterimageDecoy decoy = target != null ? target.GetComponent<ArcherAfterimageDecoy>() : null;
        return decoy != null && !decoy.IsExpired;
    }

    private void Update()
    {
        if (IsExpired)
        {
            Destroy(gameObject);
            return;
        }

        RefreshVisualAlpha();
    }

    private void OnDestroy()
    {
        Active.Remove(this);
    }

    private void CreateVisual(CharacterCtrl owner)
    {
        SpriteRenderer source = owner != null ? owner.GetComponentInChildren<SpriteRenderer>() : null;
        if (source == null || source.sprite == null)
            return;

        GameObject visual = new("Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = source.transform.localPosition;
        visual.transform.localRotation = source.transform.localRotation;
        visual.transform.localScale = source.transform.localScale;

        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = source.sprite;
        renderer.sharedMaterial = source.sharedMaterial;
        renderer.flipX = source.flipX;
        renderer.flipY = source.flipY;
        renderer.sortingLayerID = source.sortingLayerID;
        renderer.sortingOrder = source.sortingOrder - 1;
        renderer.color = DecoyColor;
        renderers = new[] { renderer };
    }

    private void RefreshVisualAlpha()
    {
        if (renderers == null || renderers.Length == 0)
            return;

        float lifetime = Mathf.Max(0.1f, expireTime - spawnTime);
        float elapsed = Mathf.Clamp01((Time.time - spawnTime) / lifetime);
        float alpha = DecoyColor.a * (1f - elapsed);

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            Color color = DecoyColor;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
