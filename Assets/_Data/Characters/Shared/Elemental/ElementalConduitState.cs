using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct ElementalConduitStoredElementView
{
    public ElementalConduitStoredElementView(ElementType element, float power, int stacks, float normalizedTime)
    {
        Element = element;
        Power = power;
        Stacks = Mathf.Max(1, stacks);
        NormalizedTime = normalizedTime;
    }

    public ElementType Element { get; }
    public float Power { get; }
    public int Stacks { get; }
    public float NormalizedTime { get; }
}

public readonly struct ElementalConduitReleasePayload
{
    public ElementalConduitReleasePayload(
        ElementType primaryElement,
        float primaryPower,
        int primaryStacks,
        ElementType primerElement,
        float primerPower,
        int primerStacks,
        ElementalReactionType reaction)
    {
        PrimaryElement = primaryElement;
        PrimaryPower = primaryPower;
        PrimaryStacks = Mathf.Max(1, primaryStacks);
        PrimerElement = primerElement;
        PrimerPower = primerPower;
        PrimerStacks = Mathf.Max(1, primerStacks);
        Reaction = reaction;
    }

    public ElementType PrimaryElement { get; }
    public float PrimaryPower { get; }
    public int PrimaryStacks { get; }
    public ElementType PrimerElement { get; }
    public float PrimerPower { get; }
    public int PrimerStacks { get; }
    public ElementalReactionType Reaction { get; }
    public bool HasPrimary => PrimaryElement != ElementType.None;
    public bool HasPrimer => PrimerElement != ElementType.None;
}

public sealed class ElementalConduitState : MonoBehaviour
{
    [Serializable]
    private struct StoredElement
    {
        public ElementType Element;
        public float Power;
        public int Stacks;
    }

    private readonly List<StoredElement> storedElements = new();
    private int capacity = 4;
    private int selectedPrimerIndex = -1;
    private int selectedPrimaryIndex = -1;
    private bool hasManualSelection;
    private bool hasPreparedRelease;
    private ElementalConduitReleasePayload preparedRelease;

    public ElementType LastAbsorbedElement { get; private set; } = ElementType.None;
    public ElementType LastPrimerElement { get; private set; } = ElementType.None;
    public ElementalReactionType LastReaction { get; private set; } = ElementalReactionType.None;
    public bool HasStoredElements => StoredCount > 0;
    public bool HasPreparedRelease => hasPreparedRelease;
    public int SelectedPrimerIndex => selectedPrimerIndex;
    public int SelectedPrimaryIndex => selectedPrimaryIndex;
    public int SelectedSlotCount =>
        (selectedPrimerIndex >= 0 ? 1 : 0) +
        (selectedPrimaryIndex >= 0 ? 1 : 0);
    public int Capacity => Mathf.Max(1, capacity);
    public int StoredCount
    {
        get
        {
            ClearExpired();
            return storedElements.Count;
        }
    }

    public event Action Changed;

    public void Store(
        ElementType element,
        float power,
        int capacity,
        int maxStacksPerElement = 3,
        float stackPowerGain = 0.5f)
    {
        if (element == ElementType.None)
            return;

        ClearExpired();
        this.capacity = Mathf.Max(1, capacity);
        int safeMaxStacks = Mathf.Max(1, maxStacksPerElement);
        float safePower = Mathf.Max(0f, power);

        for (int i = storedElements.Count - 1; i >= 0; i--)
        {
            if (storedElements[i].Element == element)
            {
                StoredElement existing = storedElements[i];

                int stacks = Mathf.Clamp(existing.Stacks + 1, 1, safeMaxStacks);
                float stackedPower = Mathf.Max(existing.Power, safePower) +
                                     safePower * Mathf.Max(0f, stackPowerGain) * Mathf.Max(0, stacks - 1);

                storedElements[i] = new StoredElement
                {
                    Element = element,
                    Power = stackedPower,
                    Stacks = stacks
                };

                LastAbsorbedElement = element;
                TrimToCapacity(capacity);
                EnsureDefaultSelection();
                Changed?.Invoke();
                return;
            }
        }

        storedElements.Add(new StoredElement
        {
            Element = element,
            Power = safePower,
            Stacks = 1
        });

        LastAbsorbedElement = element;
        TrimToCapacity(capacity);
        EnsureDefaultSelection();
        Changed?.Invoke();
    }

    public bool SelectReleaseSlot(int slotIndex)
    {
        ClearExpired();
        if (slotIndex < 0 || slotIndex >= storedElements.Count)
            return false;

        if (selectedPrimaryIndex == slotIndex)
        {
            hasManualSelection = true;
            selectedPrimaryIndex = selectedPrimerIndex;
            selectedPrimerIndex = -1;
            ValidateSelectedIndices();
            Changed?.Invoke();
            return true;
        }

        if (selectedPrimerIndex == slotIndex)
        {
            hasManualSelection = true;
            selectedPrimerIndex = -1;
            ValidateSelectedIndices();
            Changed?.Invoke();
            return true;
        }

        if (SelectedSlotCount >= 2)
            return false;

        hasManualSelection = true;
        selectedPrimerIndex = selectedPrimaryIndex;
        selectedPrimaryIndex = slotIndex;
        ValidateSelectedIndices();
        Changed?.Invoke();
        return true;
    }

    public bool IsReleaseSlotSelected(int slotIndex)
    {
        ValidateSelectedIndices();
        return slotIndex >= 0 &&
               (slotIndex == selectedPrimerIndex || slotIndex == selectedPrimaryIndex);
    }

    public void ClearSelection()
    {
        bool hadSelection = selectedPrimerIndex >= 0 || selectedPrimaryIndex >= 0;
        hasManualSelection = false;
        selectedPrimerIndex = -1;
        selectedPrimaryIndex = -1;

        if (hadSelection)
            Changed?.Invoke();
    }

    public bool TryGetReleasePreview(
        Func<ElementalReactionType, bool> reactionAllowed,
        out ElementalConduitReleasePayload payload)
    {
        ClearExpired();
        return TryBuildReleasePayload(reactionAllowed, out payload);
    }

    public void RecordRelease(ElementType primaryElement, ElementType primerElement, ElementalReactionType reaction)
    {
        LastAbsorbedElement = primaryElement;
        LastPrimerElement = primerElement;
        LastReaction = reaction;
        Changed?.Invoke();
    }

    public bool TryConsumeForRelease(
        bool requireReaction,
        Func<ElementalReactionType, bool> reactionAllowed,
        out ElementalConduitReleasePayload payload)
    {
        if (TryConsumePreparedRelease(out payload))
            return true;

        return TryPrepareRelease(requireReaction, reactionAllowed, out payload, consumePrepared: true);
    }

    public bool TryPrepareRelease(
        bool requireReaction,
        Func<ElementalReactionType, bool> reactionAllowed,
        out ElementalConduitReleasePayload payload)
    {
        if (hasPreparedRelease)
        {
            payload = preparedRelease;
            return true;
        }

        return TryPrepareRelease(requireReaction, reactionAllowed, out payload, consumePrepared: false);
    }

    private bool TryPrepareRelease(
        bool requireReaction,
        Func<ElementalReactionType, bool> reactionAllowed,
        out ElementalConduitReleasePayload payload,
        bool consumePrepared)
    {
        ClearExpired();
        payload = default;

        if (hasPreparedRelease)
        {
            payload = preparedRelease;
            if (consumePrepared)
            {
                hasPreparedRelease = false;
                preparedRelease = default;
            }

            return true;
        }

        if (storedElements.Count == 0)
            return false;

        if (!TryBuildReleasePayload(reactionAllowed, out payload))
            return false;

        if (requireReaction && payload.Reaction == ElementalReactionType.None)
            return false;

        ConsumeSelectedElements();
        ClearSelectedIndicesWithoutNotify();
        EnsureDefaultSelection();
        preparedRelease = payload;
        hasPreparedRelease = !consumePrepared;
        RecordRelease(payload.PrimaryElement, payload.PrimerElement, payload.Reaction);
        Changed?.Invoke();
        return true;
    }

    private bool TryConsumePreparedRelease(out ElementalConduitReleasePayload payload)
    {
        if (!hasPreparedRelease)
        {
            payload = default;
            return false;
        }

        payload = preparedRelease;
        hasPreparedRelease = false;
        preparedRelease = default;
        ClearSelectedIndicesWithoutNotify();
        return true;
    }

    private bool TryBuildReleasePayload(
        Func<ElementalReactionType, bool> reactionAllowed,
        out ElementalConduitReleasePayload payload)
    {
        ValidateSelectedIndices();
        payload = default;

        if (storedElements.Count < 2)
            return false;

        if (selectedPrimaryIndex < 0 || selectedPrimerIndex < 0)
            return false;

        int primaryIndex = selectedPrimaryIndex;
        StoredElement primary = storedElements[primaryIndex];

        int primerIndex = selectedPrimerIndex;

        if (primerIndex >= 0 && !IsValidPrimer(primaryIndex, primerIndex, reactionAllowed))
            primerIndex = -1;

        StoredElement primer = primerIndex >= 0 ? storedElements[primerIndex] : default;
        ElementalReactionType reaction = primerIndex >= 0
            ? CharacterElementalState.GetReaction(primer.Element, primary.Element)
            : ElementalReactionType.None;

        payload = new ElementalConduitReleasePayload(
            primary.Element,
            primary.Power,
            primary.Stacks,
            primerIndex >= 0 ? primer.Element : ElementType.None,
            primerIndex >= 0 ? primer.Power : 0f,
            primerIndex >= 0 ? primer.Stacks : 1,
            reaction);

        return true;
    }

    private bool IsValidPrimer(int primaryIndex, int primerIndex, Func<ElementalReactionType, bool> reactionAllowed)
    {
        if (primaryIndex < 0 || primaryIndex >= storedElements.Count ||
            primerIndex < 0 || primerIndex >= storedElements.Count)
        {
            return false;
        }

        StoredElement primary = storedElements[primaryIndex];
        StoredElement primer = storedElements[primerIndex];
        if (primary.Element == ElementType.None ||
            primer.Element == ElementType.None ||
            primary.Element == primer.Element)
        {
            return false;
        }

        ElementalReactionType reaction = CharacterElementalState.GetReaction(primer.Element, primary.Element);
        return reaction != ElementalReactionType.None &&
               (reactionAllowed == null || reactionAllowed(reaction));
    }

    private void ConsumeSelectedElements()
    {
        RemoveStoredElementAt(Mathf.Max(selectedPrimaryIndex, selectedPrimerIndex));
        RemoveStoredElementAt(Mathf.Min(selectedPrimaryIndex, selectedPrimerIndex));
    }

    private void RemoveStoredElementAt(int index)
    {
        if (index < 0 || index >= storedElements.Count)
            return;

        StoredElement stored = storedElements[index];
        int remainingStacks = stored.Stacks - 1;
        if (remainingStacks <= 0)
        {
            storedElements.RemoveAt(index);
            return;
        }

        stored.Stacks = remainingStacks;
        storedElements[index] = stored;
    }

    public IReadOnlyList<ElementalConduitStoredElementView> GetStoredElements()
    {
        ClearExpired();

        ElementalConduitStoredElementView[] result = new ElementalConduitStoredElementView[storedElements.Count];
        for (int i = 0; i < storedElements.Count; i++)
        {
            StoredElement stored = storedElements[i];
            result[i] = new ElementalConduitStoredElementView(
                stored.Element,
                stored.Power,
                Mathf.Max(1, stored.Stacks),
                1f);
        }

        return result;
    }

    private void ClearExpired()
    {
        int removed = storedElements.RemoveAll(element => element.Element == ElementType.None);
        if (removed > 0)
        {
            EnsureDefaultSelection();
            Changed?.Invoke();
        }
    }

    private void TrimToCapacity(int capacity)
    {
        int safeCapacity = Mathf.Max(1, capacity);
        while (storedElements.Count > safeCapacity)
        {
            storedElements.RemoveAt(0);
            if (selectedPrimaryIndex >= 0)
                selectedPrimaryIndex--;
            if (selectedPrimerIndex >= 0)
                selectedPrimerIndex--;
        }

        EnsureDefaultSelection();
    }

    private void ValidateSelectedIndices()
    {
        if (selectedPrimaryIndex < 0 || selectedPrimaryIndex >= storedElements.Count)
            selectedPrimaryIndex = -1;

        if (selectedPrimerIndex < 0 || selectedPrimerIndex >= storedElements.Count || selectedPrimerIndex == selectedPrimaryIndex)
            selectedPrimerIndex = -1;
    }

    private void EnsureDefaultSelection()
    {
        ValidateSelectedIndices();

        if (storedElements.Count < 2)
        {
            ClearSelectedIndicesWithoutNotify();
            hasManualSelection = false;
            return;
        }

        if (hasManualSelection)
            return;

        if (SelectedSlotCount >= 2)
            return;

        selectedPrimerIndex = 0;
        selectedPrimaryIndex = 1;
    }

    private void ClearSelectedIndicesWithoutNotify()
    {
        selectedPrimerIndex = -1;
        selectedPrimaryIndex = -1;
        hasManualSelection = false;
    }
}
