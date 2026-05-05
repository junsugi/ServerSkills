using Google.Protobuf;
using Google.Protobuf.Protocol;

namespace ServerSkills.Packet;

public class PacketManager
{
    public static PacketManager Instacne => new PacketManager();

    private Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>> _onRecv =
        new Dictionary<ushort, Action<PacketSession, ArraySegment<byte>, ushort>>();

    private Dictionary<ushort, Action<PacketSession, IMessage>> _handler =
        new Dictionary<ushort, Action<PacketSession, IMessage>>();

    private PacketManager()
    {
        Register();
    }

    private void Register()
    {
        _onRecv.Add((ushort)MsgId.CConnected, MakePacket<C_Connected>);
        _handler.Add((ushort)MsgId.CConnected, PacketHandler.C_ConnectedHandler);       
    }

    public void OnRecvPacket(ClientSession session, ArraySegment<byte> buffer)
    {
        ushort count = 0;

        ushort dataSize = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
        count += 2;
        ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + count);
        count += 2;

        Action<PacketSession, ArraySegment<byte>, ushort> action = null;
        if (_onRecv.TryGetValue(id, out action))
            action.Invoke(session, buffer, id);
    }

    private void MakePacket<T>(PacketSession session, ArraySegment<byte> buffer, ushort id) where T : IMessage, new()
    {
        T packet = new T();
        packet.MergeFrom(buffer.Array, buffer.Offset + 4, buffer.Count - 4);

        Action<PacketSession, IMessage> action = null;
        if (_handler.TryGetValue(id, out action))
            action.Invoke(session, packet);
    }
}