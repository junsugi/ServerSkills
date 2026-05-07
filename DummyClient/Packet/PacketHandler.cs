using DummyClient.Session;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerSkills;

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
}