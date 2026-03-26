using AeroScape.Server.Core.Entities;

namespace AeroScape.Server.Core.Services;

public sealed class ObjectInteractionService
{
    private readonly ShopService _shops;
    private readonly PrayerService _prayer;

    public ObjectInteractionService(ShopService shops, PrayerService prayer)
    {
        _shops = shops;
        _prayer = prayer;
    }

    public bool HandleOption1(Player player, int objectId, int x, int y)
    {
        player.ClickId = objectId;
        player.ClickX = x;
        player.ClickY = y;

        switch (objectId)
        {
            case 2213:
            case 2672:
            case 280:
            case 4483:
            case 25808:
            case 26972:
                player.InterfaceId = 762;
                return true;
            case 409:
            case 34616:
            case 19145:
            case 26286:
            case 26288:
            case 26289:
                player.SkillLvl[5] = player.GetLevelForXP(5);
                _prayer.Reset(player);
                return true;
            case 6552:
                player.IsAncients = player.IsAncients == 1 ? 0 : 1;
                player.IsLunar = 0;
                return true;
            case 17010:
                player.IsLunar = player.IsLunar == 1 ? 0 : 1;
                player.IsAncients = 0;
                return true;
            case 1738:
            case 1740:
                player.SetCoords(player.AbsX, player.AbsY, player.HeightLevel == 0 ? 1 : 0);
                return true;
            case 15482:
                player.InterfaceId = 399;
                return true;
            case 23271:
                player.JumpDelay = 3;
                return true;
            default:
                return false;
        }
    }

    public bool HandleOption2(Player player, int objectId, int x, int y)
    {
        if (objectId == 28089)
        {
            player.InterfaceId = 762;
            return true;
        }

        return false;
    }
}
