using DummyClient.Session;
using Google.Protobuf;
using Google.Protobuf.Protocol;
using ServerCore;
using ServerSkills;
using ServerSkills.Packet;

namespace DummyClient;

public class PacketManager
{
    public static PacketManager Instance => new PacketManager();

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
        _onRecv.Add((ushort)MsgId.SConnected, MakePacket<S_Connected>);
        _handler.Add((ushort)MsgId.SConnected, PacketHandler.S_ConnectedHandler);   
        _onRecv.Add((ushort)MsgId.SLogin, MakePacket<S_Login>);
        _handler.Add((ushort)MsgId.SLogin, PacketHandler.S_LoginHandler);       
        _onRecv.Add((ushort)MsgId.SEnterGame, MakePacket<S_EnterGame>);
        _handler.Add((ushort)MsgId.SEnterGame, PacketHandler.S_EnterGameHandler);
        _onRecv.Add((ushort)MsgId.SSpawn, MakePacket<S_Spawn>);
        _handler.Add((ushort)MsgId.SSpawn, PacketHandler.S_SpawnHandler);
        _onRecv.Add((ushort)MsgId.SPickItem, MakePacket<S_PickItem>);
        _handler.Add((ushort)MsgId.SPickItem, PacketHandler.S_PickItemHandler);
        _onRecv.Add((ushort)MsgId.SMove, MakePacket<S_Move>);
        _handler.Add((ushort)MsgId.SMove, PacketHandler.S_MoveHandler);
    }    

    public void OnRecvPacket(ServerSession session, ArraySegment<byte> buffer)
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