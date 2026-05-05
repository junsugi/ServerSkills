using Google.Protobuf.Protocol;

namespace ServerSkills;

public partial class ClientSession : PacketSession
{
    public void HandleConnected()
    {
        S_Connected connectedPacket = new S_Connected();
        connectedPacket.Result = ResultCode.Success;
        Send(connectedPacket);
    }
}