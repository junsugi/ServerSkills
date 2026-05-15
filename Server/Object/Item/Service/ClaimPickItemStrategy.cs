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

        LogPickItem("PICKUP_REQUEST", gameRoom, player, requestId, objectId);

        if (!gameRoom.HasRoomItem(objectId))
        {
            Interlocked.Increment(ref metrics.ClaimFail);
            LogPickItem("PICKUP_REQUEST", gameRoom, player, requestId, objectId, "reason=ALREADY_PICKUP");
            gameRoom.CompletePickItemRequest(player, requestId, objectId, ResultCode.InvalidRequest);
            return;
        }

        RoomItem roomItem = gameRoom.GetRoomItem(objectId)!;
        if (!roomItem.TryClaim(player.ObjectId))
        {
            Interlocked.Increment(ref metrics.ClaimFail);
            LogPickItem("PICKUP_REQUEST", gameRoom, player, requestId, objectId, $"claimedByPlayerId={roomItem.GetClaimedByPlayerId()}");
            gameRoom.CompletePickItemRequest(player, requestId, objectId, ResultCode.InvalidRequest);
            return;
        }

        LogPickItem("CLAIM_SUCCESS", gameRoom, player, requestId, objectId, $"claimedByPlayerId={roomItem.GetClaimedByPlayerId()}");

        bool forceCommitFail =
            EnablePickItemTrace && Interlocked.CompareExchange(ref _forcedCommitFailCount, 1, 0) == 0;

        if (forceCommitFail || !player.Inventory.TryAdd(roomItem.Item))
        {
            Interlocked.Increment(ref metrics.CommitFail);
            LogPickItem("COMMIT_FAIL", gameRoom, player, requestId, objectId, "reason=INVENTORY_REJECTED");
            
            if (roomItem.RollbackClaim(player.ObjectId))
            {
                Interlocked.Increment(ref metrics.RollbackCount);
                LogPickItem("ROLLBACK_SUCCESS", gameRoom, player, requestId, objectId);
            }

            gameRoom.CompletePickItemRequest(player, requestId, objectId, ResultCode.InternalError);
            return;
        }
        LogPickItem("COMMIT_SUCCESS", gameRoom, player, requestId, objectId);

        gameRoom.RemoveRoomItem(objectId);
        metrics.OnSuccess(player.ObjectId, objectId);

        gameRoom.CompletePickItemRequest(player, requestId, objectId, ResultCode.Success, roomItem.Item);
    }

    private static readonly bool EnablePickItemTrace =
        Environment.GetEnvironmentVariable("TRACE_PICK_ITEM") == "true";

    private static void LogPickItem(
        string eventName,
        GameRoom gameRoom,
        Player player,
        int requestId,
        int itemId,
        string? detail = null)
    {
        if (!EnablePickItemTrace)
            return;

        Console.WriteLine(
            $"[{DateTimeOffset.Now:HH:mm:ss.fff}][{eventName}] " +
            $"roomId={gameRoom.RoomId}, playerId={player.ObjectId}, " +
            $"itemId={itemId}, requestId={requestId}" +
            (detail is null ? "" : $", {detail}")
        );
    }
}