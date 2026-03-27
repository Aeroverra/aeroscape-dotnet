using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Frames;

namespace AeroScape.Server.Network.Update;

public sealed class GameUpdateService : IGameUpdateService
{
    private readonly GameEngine _engine;
    private readonly PlayerUpdateWriter _playerUpdateWriter;
    private readonly NpcUpdateWriter _npcUpdateWriter;

    public GameUpdateService(GameEngine engine, PlayerUpdateWriter playerUpdateWriter, NpcUpdateWriter npcUpdateWriter)
    {
        _engine = engine;
        _playerUpdateWriter = playerUpdateWriter;
        _npcUpdateWriter = npcUpdateWriter;
    }

    public void PreparePlayerMovement(Player player)
    {
    }

    public void SendPlayerAndNpcUpdates(Player player)
    {
        if (player.Session == null)
            return;

        var frame = new FrameWriter(8192);
        _playerUpdateWriter.Update(player, _engine.Players, frame);
        _npcUpdateWriter.UpdateNpc(player, _engine.Npcs, frame);

        var stream = player.Session.GetStream();
        if (frame.Length > 0)
        {
            stream.Write(frame.WrittenSpan);
            stream.Flush();
        }
    }

    public void ClearPlayerUpdateReqs(Player player) => _playerUpdateWriter.ClearUpdateReqs(player);
    public void ClearNpcUpdateMasks(NPC npc) => _npcUpdateWriter.ClearUpdateMasks(npc);

    public void ProcessNpcMovement(NPC npc)
    {
        if (npc.RandomWalk && !npc.AttackingPlayer)
            _npcUpdateWriter.RandomWalk(npc);
    }
}
