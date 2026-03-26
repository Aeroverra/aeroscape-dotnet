using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AeroScape.Server.Core.Combat;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

/// <summary>
/// Handles Magic-on-NPC packet (opcode 24).
/// Resolves the spell from the interface button, then either sets up autocasting
/// or triggers a one-shot spell cast via the PlayerVsNpcCombat service.
/// Ported from MagicOnNPC / PacketManager logic.
/// </summary>
public class MagicOnNPCMessageHandler : IMessageHandler<MagicOnNPCMessage>
{
    private readonly ILogger<MagicOnNPCMessageHandler> _logger;
    private readonly GameEngine _engine;

    public MagicOnNPCMessageHandler(ILogger<MagicOnNPCMessageHandler> logger, GameEngine engine)
    {
        _logger = logger;
        _engine = engine;
    }

    public Task HandleAsync(PlayerSession session, MagicOnNPCMessage message, CancellationToken cancellationToken)
    {
        var player = session.Entity;
        if (player == null)
            return Task.CompletedTask;

        int npcIndex = message.NpcIndex;
        int buttonId = message.ButtonId;

        if (npcIndex <= 0 || npcIndex >= GameEngine.MaxNpcs)
            return Task.CompletedTask;

        var npc = _engine.Npcs[npcIndex];
        if (npc == null || npc.IsDead)
            return Task.CompletedTask;

        // Resolve spell ID from interface button
        if (!MagicSpellData.ButtonToSpellId.TryGetValue(buttonId, out int spellId))
        {
            _logger.LogDebug("[MagicOnNPC] Unknown spell button {Button} from player {Player}",
                buttonId, player.Username);
            return Task.CompletedTask;
        }

        // Check magic level
        var spell = MagicSpellData.Spells[spellId];
        if (player.GetLevelForXP(CombatConstants.SkillMagic) < spell.LevelRequired)
        {
            _logger.LogDebug("[MagicOnNPC] Player {Player} lacks magic level for {Spell}",
                player.Username, spell.Name);
            return Task.CompletedTask;
        }

        // Check cooldown
        if (!player.MagicCanCast)
            return Task.CompletedTask;

        // Set up combat target with magic
        player.AttackNPC = npcIndex;
        player.AttackingNPC = true;
        player.AutoCastSpellId = spellId;
        player.AutoCasting = true;
        player.MagicCanCast = true;

        // Reset PvP
        player.AttackingPlayer = false;
        player.AttackPlayer = 0;

        _logger.LogDebug("[MagicOnNPC] Player {Player} casting {Spell} on NPC {Index}",
            player.Username, spell.Name, npcIndex);

        return Task.CompletedTask;
    }
}
