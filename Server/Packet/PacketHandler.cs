using Google.Protobuf;
using Google.Protobuf.Protocol;

namespace ServerSkills.Packet;

public class PacketHandler
{
    public static void C_ConnectedHandler(PacketSession session, IMessage message)
    {
        C_Connected connectedPacket =  (C_Connected)message;
        ClientSession clientSession = (ClientSession)session;

        clientSession.HandleConnected();
    }
}