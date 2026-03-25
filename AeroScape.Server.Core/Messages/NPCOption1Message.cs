namespace AeroScape.Server.Core.Messages;

/// <summary>
/// First NPC option (e.g. Talk-to, Trade, Pickpocket).
/// Parsed from the legacy NPCOption1 packet.
/// </summary>
public record NPCOption1Message(int NpcIndex);
