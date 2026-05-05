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
}