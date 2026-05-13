using DummyClient.Object;
using Google.Protobuf.Protocol;
using Position = DummyClient.Object.Position;

namespace DummyClient;

public partial class DummyClient
{
    private Dictionary<int, Item> _items = new();
    private Dictionary<int, Player> _players = new();

    public bool OnItemSpawned(Item item) => _items.TryAdd(item.ObjectId, item);

    public void TryPickItem(int itemObjectId)
    {
        if (!IsItemExist(itemObjectId))
        {
            Console.WriteLine($"Item {itemObjectId} doesn't exist");
            return;
        }

        C_PickItem pickItemPacket = new C_PickItem()
        {
            RequestId = pendingRequestManager.Register(nameof(C_PickItem), () => { }),
            ObjectId = itemObjectId,
        };

        _serverSession.Send(pickItemPacket);
    }

    public void OnPickItem(int requestId, ResultCode resultCode, Item? item)
    {
        pendingRequestManager.Completed(requestId);

        if (resultCode != ResultCode.Success || item == null)
        {
            Console.WriteLine($"Item {requestId} failed");
            return;
        }

        Console.WriteLine($"Player: [{MyPlayer.ObjectId}],  Item {item.ObjectId} picked");
        _items.Remove(item.ObjectId);
    }

    public bool OnPlayerSpawn(Player player) => _players.TryAdd(player.ObjectId, player);

    public void TryMove()
    {
        if (MyPlayer == null || MyPlayer.Position == null)
            return;

        C_Move movePacket = new C_Move();
        movePacket.ReqeustId = pendingRequestManager.Register(nameof(C_Move),
            () => { Task.Delay(500).ContinueWith(_ => TryMove()); });
        _serverSession.Send(movePacket);
    }

    public void OnMove(int requestId, ResultCode resultCode, Player player)
    {
        if (resultCode != ResultCode.Success || player == null)
        {
            Console.WriteLine($"Player {requestId} failed");
            return;
        }

        if (player.ObjectId == MyPlayer.ObjectId)
        {
            MyPlayer.Position = player.Position;
            pendingRequestManager.Completed(requestId);
            return;
        }

        if (_players.TryGetValue(player.ObjectId, out Player? inGamePlayer))
        {
            inGamePlayer.Position = player.Position;
            Console.WriteLine($"PlayerId: {inGamePlayer.ObjectId} | ({inGamePlayer.Position.X}, {inGamePlayer.Position.Y})");
        }
        else
        {
            // S_Spawn보다 S_Move가 먼저 오거나, spawn을 놓친 경우
            _players[player.ObjectId] = player;
        }
    }

    private bool IsItemExist(int itemObjectId)
    {
        return _items.ContainsKey(itemObjectId);
    }
}