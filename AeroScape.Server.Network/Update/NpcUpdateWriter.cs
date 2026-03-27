using AeroScape.Server.Core.Entities;
using AeroScape.Server.Core.Frames;

namespace AeroScape.Server.Network.Update;

public sealed class NpcUpdateWriter
{
    private static readonly byte[] XlateDirectionToClient = [1, 2, 4, 7, 6, 5, 3, 0];

    public void UpdateNpc(Player p, NPC?[] npcs, FrameWriter str)
    {
        var update = new FrameWriter(5000);
        var newNpcIds = new byte[npcs.Length];
        str.CreateFrameVarSizeWord(222);
        str.InitBitAccess();
        str.WriteBits(8, p.NpcListSize);
        int size = p.NpcListSize;
        p.NpcListSize = 0;
        for (int i = 0; i < size; i++)
        {
            var listed = p.NpcList[i];
            if (listed == null || !WithinDistance(p, listed) || p.DidTeleport)
            {
                if (listed != null)
                    p.NpcsInList[listed.NpcId] = 0;
                str.WriteBits(1, 1);
                str.WriteBits(2, 3);
            }
            else
            {
                UpdateNpcMovement(listed, str);
                if (listed.UpdateReq)
                    AppendNpcUpdateMasks(listed, update);
                p.NpcList[p.NpcListSize++] = listed;
            }
        }

        for (int i = 1; i < npcs.Length; i++)
        {
            var n = npcs[i];
            if (n == null || !WithinDistance(p, n) || p.NpcsInList[n.NpcId] == 1)
                continue;
            newNpcIds[n.NpcId] = 1;
            AddNewNpc(p, n, str, update);
        }

        p.RebuildNPCList = false;
        if (update.Length >= 3)
            str.WriteBits(15, 32767);
        str.FinishBitAccess();
        if (update.Length > 0)
            str.WriteBytes(update.WrittenSpan.ToArray(), update.Length, 0);
        str.EndFrameVarSizeWord();
    }

    public void ClearUpdateMasks(NPC n)
    {
        n.UpdateReq = false;
        n.SpeakTextUpdateReq = false;
        n.AnimUpdateReq = false;
        n.GfxUpdateReq = false;
        n.FaceCoordsUpdateReq = false;
        n.Hit1UpdateReq = false;
        n.Hit2UpdateReq = false;
        n.FaceToUpdateReq = false;
        n.SpeakText = string.Empty;
        n.AnimRequest = 65535;
        n.AnimDelay = 0;
        n.GfxRequest = 65535;
        n.GfxHeight = 0;
        n.MoveX = n.MoveY = 0;
        n.Direction = -1;
        n.FaceCoordsX = -1;
        n.FaceCoordsY = -1;
        n.HitDiff1 = 0;
        n.PoisonHit1 = 0;
        n.HitDiff2 = 0;
        n.PoisonHit2 = 0;
    }

    public void RandomWalk(NPC n)
    {
        if (!DoesWalk(n) && n.FollowPlayer != 0)
        {
        }
        if (n.RandomWalk && DoesWalk(n) && !InRange(n, n.AbsX, n.AbsY))
        {
            n.MoveX = GetMove(n.AbsX, n.MakeX);
            n.MoveY = GetMove(n.AbsY, n.MakeY);
            GetNextNpcMovement(n);
        }
        else if (n.RandomWalk && Random.Shared.Next(10) == 0 && DoesWalk(n))
        {
            int moveX = Random.Shared.Next(2);
            int moveY = Random.Shared.Next(2);
            int rnd = Random.Shared.Next(5);
            if (rnd == 1)
            {
                moveX = -moveX;
                moveY = -moveY;
            }
            else if (rnd == 2)
            {
                moveX = -moveX;
            }
            else if (rnd == 3)
            {
                moveY = -moveY;
            }
            if (InRange(n, n.AbsX + moveX, n.AbsY + moveY))
            {
                n.MoveX = moveX;
                n.MoveY = moveY;
                GetNextNpcMovement(n);
                n.RequestFaceTo(-1);
            }
        }
    }

    private static void UpdateNpcMovement(NPC n, FrameWriter str)
    {
        if (n.Direction == -1)
        {
            if (n.UpdateReq)
            {
                str.WriteBits(1, 1);
                str.WriteBits(2, 0);
            }
            else
            {
                str.WriteBits(1, 0);
            }
        }
        else
        {
            str.WriteBits(1, 1);
            str.WriteBits(2, 1);
            str.WriteBits(3, XlateDirectionToClient[n.Direction]);
            str.WriteBits(1, n.UpdateReq ? 1 : 0);
        }
    }

    public void GetNextNpcMovement(NPC n)
    {
        if (n.MoveX == 0 && n.MoveY == 0)
            return;

        int dir = Direction(n.AbsX, n.AbsY, n.AbsX + n.MoveX, n.AbsY + n.MoveY);
        if (dir == -1)
            return;

        n.UpdateReq = true;
        dir >>= 1;
        n.Direction = dir;
        n.AbsX += n.MoveX;
        n.AbsY += n.MoveY;
    }

    private void AppendNpcUpdateMasks(NPC n, FrameWriter str)
    {
        if (!n.UpdateReq)
            return;

        bool b = false;
        int maskData = 0;
        if (n.FaceToUpdateReq) maskData |= 0x10;
        if (b) maskData |= 0x8;
        if (n.SpeakTextUpdateReq) maskData |= 0x40;
        if (n.AnimUpdateReq) maskData |= 0x1;
        if (n.GfxUpdateReq) maskData |= 0x2;
        if (n.Hit2UpdateReq) maskData |= 0x20;
        if (n.FaceCoordsUpdateReq) maskData |= 0x80;
        if (n.Hit1UpdateReq) maskData |= 0x4;
        str.WriteByte(maskData);
        if (n.FaceToUpdateReq) str.WriteWord(n.FaceToRequest);
        if (n.SpeakTextUpdateReq) str.WriteString(n.SpeakText);
        if (n.SpeakTextUpdateReq) str.WriteString(n.SpeakText);
        if (n.AnimUpdateReq)
        {
            str.WriteWordA(n.AnimRequest);
            str.WriteByte(n.AnimDelay);
        }
        if (n.GfxUpdateReq)
        {
            str.WriteWordA(n.GfxRequest);
            str.WriteDWordV2(n.GfxDelay);
        }
        if (n.Hit2UpdateReq) AppendHit2(n, str);
        if (n.FaceCoordsUpdateReq)
        {
            str.WriteWordA(n.FaceCoordsX);
            str.WriteWordBigEndianA(n.FaceCoordsY);
        }
        if (n.Hit1UpdateReq) AppendHit1(n, str);
    }

    private static void AppendHit1(NPC n, FrameWriter str)
    {
        str.WriteByte(n.HitDiff1);
        if (n.PoisonHit1 == 0) str.WriteByte(n.HitDiff1 > 0 ? 1 : 0);
        else str.WriteByte(2);
        int hpRatio = (int)Math.Round((double)n.CurrentHP / n.MaxHP * 100) * 255 / 100;
        str.WriteByteS(hpRatio);
    }

    private static void AppendHit2(NPC n, FrameWriter str)
    {
        str.WriteByte(n.HitDiff2);
        if (n.PoisonHit2 == 0) str.WriteByteS(n.HitDiff2 > 0 ? 1 : 0);
        else str.WriteByteS(2);
    }

    private void AddNewNpc(Player p, NPC npc, FrameWriter str, FrameWriter update)
    {
        p.NpcsInList[npc.NpcId] = 1;
        p.NpcList[p.NpcListSize++] = npc;
        str.WriteBits(15, npc.NpcId);
        str.WriteBits(14, npc.NpcType);
        str.WriteBits(1, npc.UpdateReq ? 1 : 0);
        int y = npc.AbsY - p.AbsY;
        if (y < 0) y += 32;
        int x = npc.AbsX - p.AbsX;
        if (x < 0) x += 32;
        str.WriteBits(5, y);
        str.WriteBits(5, x);
        str.WriteBits(3, 0);
        str.WriteBits(1, 1);
        AppendNpcUpdateMasks(npc, update);
    }

    private static bool WithinDistance(Player p, NPC npc)
    {
        if (p.HeightLevel != npc.HeightLevel || npc.HiddenNPC)
            return false;
        int deltaX = npc.AbsX - p.AbsX;
        int deltaY = npc.AbsY - p.AbsY;
        return deltaX <= 15 && deltaX >= -16 && deltaY <= 15 && deltaY >= -16;
    }

    private static bool DoesWalk(NPC n) => n.MoveRangeX1 > 0 && n.MoveRangeY1 > 0 && n.MoveRangeX2 > 0 && n.MoveRangeY2 > 0;
    private static bool InRange(NPC n, int moveX, int moveY) => moveX <= n.MoveRangeX1 && moveX >= n.MoveRangeX2 && moveY <= n.MoveRangeY1 && moveY >= n.MoveRangeY2;

    private static int Direction(int srcX, int srcY, int destX, int destY)
    {
        int dx = destX - srcX;
        int dy = destY - srcY;
        if (dx < 0)
        {
            if (dy < 0) return dx < dy ? 11 : dx > dy ? 9 : 10;
            if (dy > 0) return -dx < dy ? 15 : -dx > dy ? 13 : 14;
            return 12;
        }
        if (dx > 0)
        {
            if (dy < 0) return dx < -dy ? 7 : dx > -dy ? 5 : 6;
            if (dy > 0) return dx < dy ? 1 : dx > dy ? 3 : 2;
            return 4;
        }
        return dy < 0 ? 8 : dy > 0 ? 0 : -1;
    }

    private static int GetMove(int place1, int place2)
    {
        if ((place1 - place2) == 0) return 0;
        if ((place1 - place2) < 0) return 1;
        if ((place1 - place2) > 0) return -1;
        return 0;
    }
}
