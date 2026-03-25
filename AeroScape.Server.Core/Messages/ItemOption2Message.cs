namespace AeroScape.Server.Core.Messages;

/// <summary>
/// Second item option (e.g. Unequip from equipment screen).
/// Parsed from the legacy ItemOption2 packet.
/// </summary>
public record ItemOption2Message(int ItemSlot, int InterfaceId, int ItemId);
