using System;
using UnityEngine;

/// <summary>
/// Compatibility component for scenes that already used the old script name.
/// Use ForgeMarketUIController for all new UI work.
/// </summary>
[Obsolete("Use ForgeMarketUIController instead.")]
public class EconomyForgePanel : ForgeMarketUIController
{
    // The previous implementation generated Header/Scroll Area/Message and
    // nested upgrade cards directly in the editor. Removing that one marker
    // tree clears its now-invalid private component references without touching
    // a designer-authored ForgeMarketUIController hierarchy.
    private void OnValidate()
    {
        RemoveLegacyGeneratedUi();
    }

    protected override void OnEnable()
    {
        RemoveLegacyGeneratedUi();
    }

    private void RemoveLegacyGeneratedUi()
    {
        Transform legacyScrollArea = transform.Find("Scroll Area");
        if (legacyScrollArea == null)
            return;

        Transform legacyHeader = transform.Find("Header");
        Transform legacyMessage = transform.Find("Message");
        RemoveObject(legacyHeader);
        RemoveObject(legacyScrollArea);
        RemoveObject(legacyMessage);
    }

    private static void RemoveObject(Transform target)
    {
        if (target == null) return;

        if (Application.isPlaying)
            Destroy(target.gameObject);
        else
            DestroyImmediate(target.gameObject);
    }
}
