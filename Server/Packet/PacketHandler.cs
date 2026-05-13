using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;

namespace ServerSkills.Packet;

public class PacketHandler
{
    public static void C_ConnectedHandler(PacketSession session, IMessage packet)
    {
        C_Connected connectedPacket =  (C_Connected)packet;
        ClientSession clientSession = (ClientSession)session;

        clientSession.HandleCConnected();
    }

    public static void C_LoginHandler(PacketSession session, IMessage packet)
    {
        C_Login loginPacket =  (C_Login)packet;
        ClientSession clientSession = (ClientSession)session;

        int requestId = loginPacket.RequestId;
        string id = loginPacket.Id;
        clientSession.HandleCLogin(requestId, id);
    }

    public static void C_EnterGameHandler(PacketSession session, IMessage packet)
    {
        C_EnterGame enterPacket =  (C_EnterGame)packet;
        ClientSession clientSession = (ClientSession)session;

        int requestId = enterPacket.RequestId;
        clientSession.HandleCEnterGame(requestId);
    }

    public static void C_ReadyForTestHandler(PacketSession session, IMessage packet)
    {
        C_ReadyForTest testPacket =  (C_ReadyForTest)packet;
        ClientSession clientSession = (ClientSession)session;
        
        clientSession.HandleCReadyForTest();
    }
    
    public static void C_PickItemHandler(PacketSession session, IMessage packet)
    {
        C_PickItem pickItemPacket =  (C_PickItem)packet;
        ClientSession clientSession = (ClientSession)session;
        
        int requestId = pickItemPacket.RequestId;
        int objectId = pickItemPacket.ObjectId;
        
        clientSession.HandleCPickItem(requestId, objectId);
    }

    public static void C_MoveHandler(PacketSession session, IMessage packet)
    {
        C_Move movePacket =  (C_Move)packet;
        ClientSession clientSession = (ClientSession)session;

        int requestId = movePacket.ReqeustId;
        float x = movePacket.Position.X;
        float y = movePacket.Position.Y;

        clientSession.HandleCMove(requestId, x, y);
    }
}