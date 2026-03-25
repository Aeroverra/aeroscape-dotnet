namespace AeroScape.Server.Core.Messages;

public record ButtonMessage(int InterfaceId, int ButtonId, int ItemId, int SlotId);
