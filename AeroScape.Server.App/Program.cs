using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using AeroScape.Server.App.Services;
using AeroScape.Server.Core.Engine;
using AeroScape.Server.Core.Handlers;
using AeroScape.Server.Core.Items;
using AeroScape.Server.Core.Messages;
using AeroScape.Server.Core.Movement;
using AeroScape.Server.Core.Session;
using AeroScape.Server.Core.Services;
using AeroScape.Server.Data;
using AeroScape.Server.Core.World;
using AeroScape.Server.Network.Listeners;
using AeroScape.Server.Core.Frames;
using AeroScape.Server.Network.Login;
using AeroScape.Server.Network.Protocol;
using AeroScape.Server.Network.Update;

var builder = Host.CreateApplicationBuilder(args);

// Logging — Serilog replaces the default logger; ILogger<T> injection continues to work.
builder.Logging.ClearProviders();
builder.Services.AddSerilog(config => config
    .MinimumLevel.Information()
    .WriteTo.Console());

// ── Data / EF Core ──────────────────────────────────────────────────────────
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";

builder.Services.AddDbContext<AeroScapeDbContext>(options =>
{
    if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        var sqliteConn = builder.Configuration["ConnectionStrings:SqliteConnection"]
                         ?? "Data Source=AeroScape.db";
        options.UseSqlite(sqliteConn);
    }
    else
    {
        var sqlServerConn = builder.Configuration["ConnectionStrings:DefaultConnection"]
                            ?? throw new InvalidOperationException(
                                "Missing ConnectionStrings:DefaultConnection in appsettings.json");
        options.UseSqlServer(sqlServerConn);
    }
});

// ── Core services ───────────────────────────────────────────────────────────
builder.Services.AddSingleton<IPlayerSessionManager, PlayerSessionManager>();
builder.Services.AddSingleton<ItemDefinitionLoader>();
builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<PlayerItemsService>();
builder.Services.AddSingleton<PlayerEquipmentService>();
builder.Services.AddSingleton<PlayerBankService>();
builder.Services.AddSingleton<TradingService>();
builder.Services.AddSingleton<GroundItemManager>();
builder.Services.AddSingleton<WalkQueue>();
builder.Services.AddSingleton<ShopService>();
builder.Services.AddSingleton<PrayerService>();
builder.Services.AddSingleton<DeathService>();
builder.Services.AddSingleton<LegacyFileManager>();
builder.Services.AddSingleton<MagicService>();
builder.Services.AddSingleton<IClientUiService, ClientUiService>();
builder.Services.AddSingleton<ClanChatService>();
builder.Services.AddSingleton<IClanChatPersistenceService, ClanChatPersistenceService>();
builder.Services.AddHostedService<ClanChatSaveService>();
builder.Services.AddSingleton<BountyHunterService>();
builder.Services.AddSingleton<ConstructionService>();
builder.Services.AddSingleton<CastleWarsService>();
builder.Services.AddSingleton<DialogueService>();
builder.Services.AddSingleton<ObjectInteractionService>();
builder.Services.AddSingleton<ObjectLoaderService>();
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<NPCInteractionService>();
builder.Services.AddSingleton<NpcSpawnLoader>();
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<GameEngine>());

// ── Map data service ─────────────────────────────────────────────────────────
builder.Services.AddSingleton<MapDataService>();
builder.Services.AddSingleton<GameFrames>();
builder.Services.AddSingleton<PlayerUpdateWriter>();
builder.Services.AddSingleton<NpcUpdateWriter>();
builder.Services.AddSingleton<IGameUpdateService, GameUpdateService>();

// ── Login service ────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IPlayerLoginService, PlayerLoginService>();
builder.Services.AddSingleton<IPlayerPersistenceService, PlayerPersistenceService>();
builder.Services.AddHostedService(sp => (PlayerPersistenceService)sp.GetRequiredService<IPlayerPersistenceService>());

// ── Network / protocol ──────────────────────────────────────────────────────
builder.Services.AddSingleton<PacketRouter>();
builder.Services.AddHostedService<TcpBackgroundService>();

// ── Scoped packet handlers ──────────────────────────────────────────────────
// Each handler is registered as scoped so that IServiceScopeFactory.CreateScope()
// in PacketRouter resolves a fresh handler per packet.
builder.Services.AddScoped<IMessageHandler<WalkMessage>, WalkMessageHandler>();
builder.Services.AddScoped<IMessageHandler<PublicChatMessage>, PublicChatMessageHandler>();
builder.Services.AddScoped<IMessageHandler<CommandMessage>, CommandMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ActionButtonsMessage>, ActionButtonsMessageHandler>();
builder.Services.AddScoped<IMessageHandler<EquipItemMessage>, EquipItemMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemOperateMessage>, ItemOperateMessageHandler>();
builder.Services.AddScoped<IMessageHandler<DropItemMessage>, DropItemMessageHandler>();
builder.Services.AddScoped<IMessageHandler<PickupItemMessage>, PickupItemMessageHandler>();
builder.Services.AddScoped<IMessageHandler<PlayerOption1Message>, PlayerOption1MessageHandler>();
builder.Services.AddScoped<IMessageHandler<PlayerOption2Message>, PlayerOption2MessageHandler>();
builder.Services.AddScoped<IMessageHandler<PlayerOption3Message>, PlayerOption3MessageHandler>();
builder.Services.AddScoped<IMessageHandler<NPCAttackMessage>, NPCAttackMessageHandler>();
builder.Services.AddScoped<IMessageHandler<NPCOption1Message>, NPCOption1MessageHandler>();
builder.Services.AddScoped<IMessageHandler<NPCOption2Message>, NPCOption2MessageHandler>();
builder.Services.AddScoped<IMessageHandler<NPCOption3Message>, NPCOption3MessageHandler>();
builder.Services.AddScoped<IMessageHandler<ObjectOption1Message>, ObjectOption1MessageHandler>();
builder.Services.AddScoped<IMessageHandler<ObjectOption2Message>, ObjectOption2MessageHandler>();
builder.Services.AddScoped<IMessageHandler<SwitchItemsMessage>, SwitchItemsMessageHandler>();
builder.Services.AddScoped<IMessageHandler<SwitchItems2Message>, SwitchItems2MessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemOnItemMessage>, ItemOnItemMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemSelectMessage>, ItemSelectMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemOption1Message>, ItemOption1MessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemGiveMessage>, ItemGiveMessageHandler>();
builder.Services.AddScoped<IMessageHandler<MagicOnNPCMessage>, MagicOnNPCMessageHandler>();
builder.Services.AddScoped<IMessageHandler<MagicOnPlayerMessage>, MagicOnPlayerMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemOnObjectMessage>, ItemOnObjectMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemOnNPCMessage>, ItemOnNPCMessageHandler>();
builder.Services.AddScoped<IMessageHandler<AssaultMessage>, AssaultMessageHandler>();
builder.Services.AddScoped<IMessageHandler<BountyHunterMessage>, BountyHunterMessageHandler>();
builder.Services.AddScoped<IMessageHandler<PrayerMessage>, PrayerMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemOption2Message>, ItemOption2MessageHandler>();
builder.Services.AddScoped<IMessageHandler<ClanJoinMessage>, ClanJoinMessageHandler>();
builder.Services.AddScoped<IMessageHandler<StringInputMessage>, StringInputMessageHandler>();
builder.Services.AddScoped<IMessageHandler<LongInputMessage>, LongInputMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ClanKickMessage>, ClanKickMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ConstructionMessage>, ConstructionMessageHandler>();

// Inline-handled packet handlers (formerly processed in PacketManager.parsePacket)
builder.Services.AddScoped<IMessageHandler<AddFriendMessage>, AddFriendMessageHandler>();
builder.Services.AddScoped<IMessageHandler<RemoveFriendMessage>, RemoveFriendMessageHandler>();
builder.Services.AddScoped<IMessageHandler<AddIgnoreMessage>, AddIgnoreMessageHandler>();
builder.Services.AddScoped<IMessageHandler<RemoveIgnoreMessage>, RemoveIgnoreMessageHandler>();
builder.Services.AddScoped<IMessageHandler<PrivateMessageMessage>, PrivateMessageMessageHandler>();
builder.Services.AddScoped<IMessageHandler<IdleMessage>, IdleMessageHandler>();
builder.Services.AddScoped<IMessageHandler<DialogueContinueMessage>, DialogueContinueMessageHandler>();
builder.Services.AddScoped<IMessageHandler<CloseInterfaceMessage>, CloseInterfaceMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ItemExamineMessage>, ItemExamineMessageHandler>();
builder.Services.AddScoped<IMessageHandler<NpcExamineMessage>, NpcExamineMessageHandler>();
builder.Services.AddScoped<IMessageHandler<ObjectExamineMessage>, ObjectExamineMessageHandler>();
builder.Services.AddScoped<IMessageHandler<TradeAcceptMessage>, TradeAcceptMessageHandler>();

var host = builder.Build();

host.Services.GetRequiredService<GameEngine>().GameUpdateService =
    host.Services.GetRequiredService<IGameUpdateService>();

// Ensure database is created (dev convenience — use migrations in production)
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AeroScapeDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Load map region XTEA keys from data file
var mapDataService = host.Services.GetRequiredService<MapDataService>();
var mapDataPath = Path.Combine(AppContext.BaseDirectory, "Data", "mapdata", "1.dat");
mapDataService.LoadRegions(mapDataPath);

// Load clan channels from database
var clanPersistence = host.Services.GetRequiredService<IClanChatPersistenceService>();
await clanPersistence.LoadAllChannelsAsync();

host.Run();
