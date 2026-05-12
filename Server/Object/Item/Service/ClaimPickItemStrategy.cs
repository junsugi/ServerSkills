using Google.Protobuf.Protocol;
using ServerSkills.Game.Room;
using ServerSkills.Monitoring;

namespace ServerSkills;

public class ClaimPickItemStrategy(PickItemMetrics metrics) : IPickItemStrategy
{
    private int _forcedCommitFailCount;
    
    public void Pick(GameRoom gameRoom, Player player, int requestId, int objectId)
    {
        metrics.OnRequest(player.ObjectId, objectId);
        
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}][PICKUP_REQUEST] roomId={gameRoom.RoomId}, playerId={player.ObjectId}, itemId={objectId}, requestId={requestId}");
        
        if (!gameRoom.HasRoomItem(objectId))
        {
            Interlocked.Increment(ref metrics.ClaimFail);
            Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}][CLAIM_FAIL] roomId={gameRoom.RoomId}, playerId={player.ObjectId}, itemId={objectId}, claimedByPlayerId=0, reason=ALREADY_PICKUP");
            player.Session.SendPickItem(requestId, ResultCode.InvalidRequest);
            return;
        }
        
        RoomItem roomItem = gameRoom.GetRoomItem(objectId)!;
        if (!roomItem.TryClaim(player.ObjectId))
        {
            Interlocked.Increment(ref metrics.ClaimFail);
            Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}][CLAIM_FAIL] roomId={gameRoom.RoomId}, playerId={player.ObjectId}, itemId={objectId}, claimedByPlayerId={roomItem.GetClaimedByPlayerId()}");
            player.Session.SendPickItem(requestId, ResultCode.InvalidRequest);
            return;
        }
        
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}][CLAIM_SUCCESS] roomId={gameRoom.RoomId}, playerId={player.ObjectId}, itemId={objectId}, claimedByPlayerId={roomItem.GetClaimedByPlayerId()}");

        bool forceCommitFail =
            Environment.GetEnvironmentVariable("TEST_PICK_ITEM_FORCE_COMMIT_FAIL_ONCE") == "true"
            && Interlocked.CompareExchange(ref _forcedCommitFailCount, 1, 0) == 0;
        
        if (forceCommitFail || !player.Inventory.TryAdd(roomItem.Item))
        {
            Interlocked.Increment(ref metrics.CommitFail);
            Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}][COMMIT_FAIL] roomId={gameRoom.RoomId}, playerId={player.ObjectId}, itemId={objectId}, reason=INVENTORY_REJECTED");

            if (roomItem.RollbackClaim(player.ObjectId))
            {
                Interlocked.Increment(ref metrics.RollbackCount);
                Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}][ROLLBACK_SUCCESS] roomId={gameRoom.RoomId}, playerId={player.ObjectId}, itemId={objectId}");
            }
            
            player.Session.SendPickItem(requestId, ResultCode.InternalError);
            return;
        }
        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss.fff}][COMMIT_SUCCESS] roomId={gameRoom.RoomId}, playerId={player.ObjectId}, itemId={objectId}");

        gameRoom.RemoveRoomItem(objectId);
        metrics.OnSuccess(player.ObjectId, objectId);
        
        player.Session.SendPickItem(requestId, ResultCode.Success, roomItem.Item);
    }
}