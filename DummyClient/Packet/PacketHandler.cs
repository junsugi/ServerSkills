using DummyClient.Session;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerSkills;

namespace DummyClient;

public class PacketHandler
{
    public static void S_ConnectedHandler(PacketSession session, IMessage packet)
    {
        S_Connected connectedPacket = new S_Connected();
        ServerSession serverSession =  (ServerSession)session;

        ResultCode resultCode = connectedPacket.Result;
        serverSession.HandleSConnected(resultCode);
    }

    public static void S_LoginHandler(PacketSession session, IMessage packet)
    {
        S_Login loginPacket = new S_Login();
        ServerSession serverSession = (ServerSession)session;
        
        ResultCode resultCode = loginPacket.Result;
        serverSession.HandleSLogin(resultCode);
    }

    public static void S_EnterGameHandler(PacketSession session, IMessage packet)
    {
        S_EnterGame enterGamePacket = (S_EnterGame)packet;
        ServerSession serverSession = (ServerSession)session;
        
        ResultCode resultCode = enterGamePacket.Result;
        PlayerInfo playerInfo = enterGamePacket.Player;
        serverSession.HandleSEnterGame(resultCode, playerInfo);
    }
}