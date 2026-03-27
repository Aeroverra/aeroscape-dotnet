using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Engine;

public interface IGameUpdateService
{
    void PreparePlayerMovement(Player player);
    void SendPlayerAndNpcUpdates(Player player);
    void ClearPlayerUpdateReqs(Player player);
    void ClearNpcUpdateMasks(NPC npc);
    void ProcessNpcMovement(NPC npc);
    void RestoreTabs(Player player);
    void ProcessNpcRandomWalk(NPC npc);
}
