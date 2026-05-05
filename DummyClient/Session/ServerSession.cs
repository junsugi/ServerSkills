using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerSkills;

namespace DummyClient.Session;

public partial class ServerSession : PacketSession
{
    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        PacketManager.Instance.OnRecvPacket(this, buffer);
    }
    
    public void Send(IMessage packet)
    {
        string msgName = packet.Descriptor.Name.Replace("_", String.Empty);
        MsgId msgId = (MsgId)Enum.Parse(typeof(MsgId), msgName);
        ushort size = (ushort)packet.CalculateSize();
        byte[] sendBuffer = new byte[size + 4];
        Array.Copy(BitConverter.GetBytes((ushort)(size + 4)), 0, sendBuffer, 0, sizeof(ushort));
        Array.Copy(BitConverter.GetBytes((ushort)msgId), 0, sendBuffer, 2, sizeof(ushort));
        Array.Copy(packet.ToByteArray(), 0, sendBuffer, 4, size);

        Send(sendBuffer);
    }
    
    public override void OnConnected()
    {
        C_Connected connectedPacket = new C_Connected();
        Send(connectedPacket);
    }

    public override void OnDisconnected()
    {
        throw new NotImplementedException();
    }

    public override void OnSend()
    {
        throw new NotImplementedException();
    }
}