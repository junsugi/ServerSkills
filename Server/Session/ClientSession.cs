namespace ServerSkills;

public class ClientSession : PacketSession
{
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