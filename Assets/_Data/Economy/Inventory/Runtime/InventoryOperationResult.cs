using System.Collections.Generic;

public sealed class InventoryOperationResult
{
    private readonly List<int> changedSlots;

    public bool Success { get; }
    public InventoryOperationStatus Status { get; }
    public InventoryChangeType ChangeType { get; }
    public IReadOnlyList<int> ChangedSlots => changedSlots;
    public int RequestedAmount { get; }
    public int AcceptedAmount { get; }
    public int RemainingAmount => RequestedAmount > AcceptedAmount ? RequestedAmount - AcceptedAmount : 0;
    public string Message { get; }

    private InventoryOperationResult(
        bool success,
        InventoryOperationStatus status,
        InventoryChangeType changeType,
        IEnumerable<int> changedSlots,
        int requestedAmount,
        int acceptedAmount,
        string message)
    {
        Success = success;
        Status = status;
        ChangeType = changeType;
        this.changedSlots = BuildSlotList(changedSlots);
        RequestedAmount = requestedAmount;
        AcceptedAmount = acceptedAmount;
        Message = message;
    }

    public static InventoryOperationResult Succeeded(
        InventoryChangeType changeType,
        IEnumerable<int> changedSlots = null,
        int requestedAmount = 0,
        int acceptedAmount = 0,
        string message = null)
    {
        return new InventoryOperationResult(
            true,
            InventoryOperationStatus.Success,
            changeType,
            changedSlots,
            requestedAmount,
            acceptedAmount,
            message);
    }

    public static InventoryOperationResult Failed(
        InventoryOperationStatus status,
        InventoryChangeType changeType = InventoryChangeType.None,
        int requestedAmount = 0,
        string message = null)
    {
        return new InventoryOperationResult(
            false,
            status,
            changeType,
            null,
            requestedAmount,
            0,
            message);
    }

    public static InventoryOperationResult NoChange(InventoryChangeType changeType = InventoryChangeType.None)
    {
        return new InventoryOperationResult(
            true,
            InventoryOperationStatus.NoChange,
            changeType,
            null,
            0,
            0,
            null);
    }

    private static List<int> BuildSlotList(IEnumerable<int> source)
    {
        List<int> slots = new();
        if (source == null) return slots;

        foreach (int slotIndex in source)
        {
            if (slotIndex < 0 || slots.Contains(slotIndex)) continue;
            slots.Add(slotIndex);
        }

        slots.Sort();
        return slots;
    }
}
