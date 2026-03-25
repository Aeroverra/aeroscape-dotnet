namespace AeroScape.Server.Core.Messages;

/// <summary>
/// Second option on a game object (e.g. Bank, Prospect, Search).
/// Parsed from the legacy ObjectOption2 packet.
/// </summary>
public record ObjectOption2Message(int ObjectId, int ObjectX, int ObjectY);
