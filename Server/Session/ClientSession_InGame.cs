using Google.Protobuf.Protocol;
using ServerCore;

namespace ServerSkills;

public partial class ClientSession : PacketSession
{
    public void HandleCPickItem(int requestId, int objectId)
    {
        GameObjectType type = ObjectManager.Instance.GetObjectTypeById(objectId);
        if (type != GameObjectType.ITEM || _sessionState != SessionState.EnterGame)
        {
            S_PickItem pickItemPacket = new S_PickItem();
            pickItemPacket.ResultCode = ResultCode.InvalidRequest;
            Send(pickItemPacket);
            return;
        }

        MyPlayer.GameRoom.PickItem(MyPlayer, requestId, objectId);
    }

    public void SendPickItem(int requestId, ResultCode resultCode, Item? item = null)
    {
        S_PickItem pickItemPacket = new S_PickItem();
        pickItemPacket.RequestId = requestId;
        pickItemPacket.ResultCode = resultCode;
        pickItemPacket.ItemInfo = item == null ? null : ItemMapper.ToDto(item);
        Send(pickItemPacket);
    }
    
    
    public void HandleCMove(int requestId)
    {
        if (_sessionState != SessionState.EnterGame)
        {
            S_Move movePacket = new S_Move();
            movePacket.RequestId = requestId;
            movePacket.ResultCode = ResultCode.InvalidRequest;
            Send(movePacket);
            return;
        }

        if (MyPlayer == null || MyPlayer.GameRoom == null)
        {
            S_Move movePacket = new S_Move();
            movePacket.RequestId = requestId;
            movePacket.ResultCode = ResultCode.InternalError;
            Send(movePacket);
            return;
        }

        MyPlayer.GameRoom.Move(MyPlayer, requestId);
    }
}