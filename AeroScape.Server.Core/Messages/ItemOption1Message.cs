namespace AeroScape.Server.Core.Messages;

/// <summary>
/// First item option (e.g. Use, Eat, Drink, Unequip, Summon familiar).
/// Parsed from the legacy ItemOption1 packet.
/// </summary>
public record ItemOption1Message(int ItemSlot, int InterfaceId, int ItemId);
