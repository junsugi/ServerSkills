using Google.Protobuf.Protocol;
using ServerSkills;

namespace DummyClient.Session;

public partial class ServerSession : PacketSession
{
    private DummyClient _dummyClient;
    
    public void SetClient(DummyClient dummyClient)
    {
        _dummyClient = dummyClient;
    }
    
    public void HandleSConnected(ResultCode resultCode)
    {
       _dummyClient.OnConnected(resultCode);
    }
}