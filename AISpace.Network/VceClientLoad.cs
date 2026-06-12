namespace AISpace.Network;

public readonly record struct VceClientLoad(int ActiveHandlers, int AvailableSlots, int MaxHandlers);
