using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using ServerSkills.Packet;

namespace ServerSkills;

public partial class ClientSession : PacketSession
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
    }

    public override void OnDisconnected()
    {
        if (MyPlayer == null)
            return;
        
        if (MyPlayer.GameRoom != null)
            MyPlayer.GameRoom.LeaveGame(MyPlayer);

        _sessionState = SessionState.None;
        MyPlayer = null;
        AccountDto = null;
    }

    public override void OnSend()
    {
        throw new NotImplementedException();
    }
}