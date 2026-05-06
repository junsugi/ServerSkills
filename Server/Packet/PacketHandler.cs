using Google.Protobuf;
using Google.Protobuf.Protocol;

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

        string id = loginPacket.Id;
        clientSession.HandleCLogin(id);
    }

    public static void C_EnterGameHandler(PacketSession session, IMessage packet)
    {
        C_EnterGame enterPacket =  (C_EnterGame)packet;
        ClientSession clientSession = (ClientSession)session;

        clientSession.HandleCEnterGame();
    }
}