using DummyClient.Object;
using DummyClient.Session;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

namespace DummyClient;

public class PacketHandler
{
    public static void S_ConnectedHandler(PacketSession session, IMessage packet)
    {
        S_Connected connectedPacket = (S_Connected)packet;
        ServerSession serverSession =  (ServerSession)session;

        int requestId = connectedPacket.RequestId;
        ResultCode resultCode = connectedPacket.Result;
        serverSession.HandleSConnected(requestId, resultCode);
    }

    public static void S_LoginHandler(PacketSession session, IMessage packet)
    {
        S_Login loginPacket = (S_Login)packet;
        ServerSession serverSession = (ServerSession)session;
        
        int requestId = loginPacket.RequestId;
        ResultCode resultCode = loginPacket.Result;
        serverSession.HandleSLogin(requestId, resultCode);
    }

    public static void S_EnterGameHandler(PacketSession session, IMessage packet)
    {
        S_EnterGame enterGamePacket = (S_EnterGame)packet;
        ServerSession serverSession = (ServerSession)session;
        
        int requestId = enterGamePacket.RequestId;
        ResultCode resultCode = enterGamePacket.Result;
        PlayerInfo playerInfo = enterGamePacket.Player;
        serverSession.HandleSEnterGame(requestId, resultCode, playerInfo);
    }

    public static void S_SpawnHandler(PacketSession session, IMessage packet)
    {
        S_Spawn spawnPacket = (S_Spawn)packet;
        ServerSession serverSession = (ServerSession)session;
        
        SpawnObjectInfo spawnObject = spawnPacket.SpawnObject;
        switch (spawnObject.DetailCase)
        {
            case SpawnObjectInfo.DetailOneofCase.Player:
                Player player = PlayerMapper.ToDomain(spawnObject.Player);
                serverSession.HandleSSpawnPlayer(player);
                break;
            case SpawnObjectInfo.DetailOneofCase.Item:
                Item item = ItemMapper.ToDomain(spawnObject.Item);
                serverSession.HandleSSpawnItem(item);
                break;
            case SpawnObjectInfo.DetailOneofCase.None:
                Console.WriteLine("[S_Spawn] Invalid spawn object: detail is none");
                break;
        }
    }

    public static void S_PickItemHandler(PacketSession session, IMessage packet)
    {
        S_PickItem pickItemPacket = (S_PickItem)packet;
        ServerSession serverSession = (ServerSession)session;
        
        int requestId = pickItemPacket.RequestId;
        ResultCode resultCode = pickItemPacket.ResultCode;
        if (resultCode != ResultCode.Success)
        {
           Console.WriteLine($"Pick Item failed: {resultCode}");
           return;
        }
        
        Item item = ItemMapper.ToDomain(pickItemPacket.ItemInfo);
        serverSession.HandleSPickItem(requestId, resultCode, item);
    }

    public static void S_MoveHandler(PacketSession session, IMessage packet)
    {
        S_Move movePacket = (S_Move)packet;
        ServerSession serverSession = (ServerSession)session;

        int requestId = movePacket.RequestId;
        ResultCode resultCode = movePacket.ResultCode;
        Player player = PlayerMapper.ToDomain(movePacket.Player);
        
        serverSession.HandleSMoveHandler(player, requestId, resultCode);
    }
}