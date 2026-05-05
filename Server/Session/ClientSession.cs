using Google.Protobuf;
using Google.Protobuf.Protocol;

namespace ServerSkills;

public partial class ClientSession : PacketSession
{
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
        throw new NotImplementedException();
    }

    public override void OnDisconnected()
    {
        throw new NotImplementedException();
    }

    public override int OnRecv(ArraySegment<byte> segment)
    {
        throw new NotImplementedException();
    }

    public override void OnSend()
    {
        throw new NotImplementedException();
    }
}