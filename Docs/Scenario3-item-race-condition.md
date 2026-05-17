# Scenario 3 — Atomic Item Claim
## Intro

이번 시나리오에서는 동시 Pickup 요청 환경에서 발생하는 Race Condition을 재현하고,
Unsafe 구조 → Lock 기반 처리 → Interlocked 기반 Atomic Claim 구조를 순차적으로 비교했다.

특히 동일 아이템 중복 획득 여부, Claim / Commit / Rollback 흐름,
그리고 중복 요청 처리 가능 여부를 중심으로 정합성 보장 방식을 검증했다.

---

## Goal

다음 문제를 재현하고 개선하는 것을 목표로 했다.

- 동일 아이템 중복 획득
- Check-Then-Act Race
- Lock Contention
- Critical Section 증가

---

## Test Environment

| Category           | Value             |
| ------------------ | ----------------- |
| Runtime            | .NET 10           |
| Protocol           | TCP               |
| Serialization      | Protobuf          |
| Client             | DummyClient       |
| Concurrent Clients | 500               |

---

## Problem

멀티플레이 환경에서는 동일 아이템에 대해 여러 Pickup 요청이 동시에 도착할 수 있다.

하지만 상태 확인(Check)과 상태 변경(Act)이 분리된 구조에서는,  
Check와 Act 사이의 짧은 시간 동안 다른 요청 또한 동일 아이템에 접근할 수 있다.

특히 Thread.Sleep(100)을 통해 **경합 상황을 의도적으로 증가**시킨 결과,  
여러 플레이어가 동시에 동일 아이템을 획득하는 Race Condition이 발생했다.

---

# Step 1 — Unsafe Pickup

## Structure

[아무런 동기화 없이 Pickup 요청](https://github.com/junsugi/ServerSkills/blob/0cfc15943585ebdd110e6ca091952e6c73dfca44/Server/Object/Item/Service/UnsafePickItemStrategy.cs#L8-L26)을 처리했다.

각 요청은:

1. Item 존재 여부 확인
2. Inventory 추가
3. Room Item 제거

를 순차적으로 수행했다.

```csharp
if (!gameRoom.HasRoomItem(objectId))
    return;

RoomItem roomItem = gameRoom.GetRoomItem(objectId)!;
Item item = roomItem.Item;

Thread.Sleep(100); // 경합 키우기

player.Inventory.TryAdd(item);
gameRoom.RemoveRoomItem(objectId);
```

---

## Error Log

```text
Unhandled exception: KeyNotFoundException
The given key '33554533' was not present in the dictionary.
```

## Observation

동시 Pickup 요청 환경에서 KeyNotFoundException이 반복적으로 발생하였다.

여러 요청이 동시에 HasRoomItem() 검사를 통과한 이후,
다른 요청이 먼저 RemoveRoomItem()을 수행하면서
동일 key가 Dictionary에서 제거되었기 때문이다.

## Interpretation

Unsafe 구조에서는 Item 존재 여부 확인(Check)과
RoomItem 제거(Act)가 원자적으로 처리되지 않았다.

그 결과 Check와 Act 사이에 Race Window가 발생했으며,
동시에 들어온 여러 Pickup 요청이 동일 아이템에 접근할 수 있었다.

## Result

Unsafe Pickup 구조에서는 Race Condition으로 인해
동일 아이템 중복 처리 가능성이 존재했으며,
실제로 KeyNotFoundException이 발생하여
안정적인 성능 측정 또한 보장할 수 없었다.


---

# Step 2 — Lock Pickup

## Structure

[Unsafe 구조 전체를 Lock으로 감싸 Critical Section을 보호](https://github.com/junsugi/ServerSkills/blob/0cfc15943585ebdd110e6ca091952e6c73dfca44/Server/Object/Item/Service/LockPickItemStrategy.cs#L14-L31)했다.

```csharp
lock (_lock)
{
    if (!gameRoom.HasRoomItem(objectId))
        return;

    RoomItem roomItem = gameRoom.GetRoomItem(objectId)!;
    Item item = roomItem.Item;

    Thread.Sleep(100);

    player.Inventory.TryAdd(item);
    gameRoom.RemoveRoomItem(objectId);
}
```

---

## Metrics

### Single Claim Validation Log

```text
[PICK_BEGIN] roomId=2 playerId=16777230 itemId=33554533
[Pick Success] Room=2 Player=16777230 Item=33554533/집행검
[PICK_COMPLETE] resultCode=Success

[PICK_BEGIN] roomId=2 playerId=16777222 itemId=33554533
[Pick Failed] Room=2 Player=16777222 Item=33554533
[PICK_COMPLETE] resultCode=InvalidRequest
```

|Metric|Value|
|---|---|
|Total Rooms|4|
|Items Per Room|1|
|Expected Success|4|
|Actual Success|4|
|Duplicate Claims|0|

---

## Observation

Lock Pickup 적용 이후,
각 Room에서 정확히 하나의 Pickup 요청만 성공하였다.

Expected Success는 4건이었고 Actual Success 역시 4건으로 나타났으며,
Duplicate Claims는 0건으로 유지되었다.

이후 동일 아이템에 대한 Pickup 요청은 InvalidRequest로 처리되었다.

## Interpretation

Pickup 전체 흐름을 하나의 Critical Section으로 보호하면서,
HasRoomItem(), Inventory 추가, RemoveRoomItem() 사이의 
Race Window를 제거할 수 있었다.

이를 통해 동일 RoomItem에 대한 중복 획득은 방지할 수 있었다.

Lock 기반 구조에서도 단순 실패 처리는 가능하다.
예를 들어 Inventory 추가에 실패하면 RoomItem 제거를 수행하지 않는 방식으로
기본적인 정합성은 유지할 수 있다.

하지만 Claim / Commit / Rollback 단계가 하나의 Critical Section 안에 묶이기 때문에,
Pickup 전체 흐름의 Lock 범위가 커진다.

향후 DB 저장, Inventory 검증, 재시도 처리 같은 외부 실패 요인이 추가될수록
Critical Section 내부 책임이 증가하고,
Claim / Commit / Rollback의 경계를 명확히 분리하기 어려워진다.

## Result

Lock 기반 구조는 동일 아이템 중복 획득을 방지하며 정합성을 확보할 수 있었다.
하지만 Pickup 전체 흐름을 하나의 Critical Section으로 직렬화하기 때문에,
동시 요청이 증가할수록 Lock Contention이 발생할 수 있다.

또한 Claim / Commit / Rollback 단계가 Lock 내부에 함께 묶이면서,
부분 실패 처리와 재시도 흐름을 확장하기 어려운 구조적 한계가 있었다.

---

# Step 3 — Atomic Claim

## Structure

Item 데이터 자체에 Claim 상태를 두지 않고, Room 내부에 존재하는 [RoomItem이 Claim 상태를 관리](https://github.com/junsugi/ServerSkills/blob/0cfc15943585ebdd110e6ca091952e6c73dfca44/Server/Game/Room/RoomItem.cs#L3-L20)하도록 분리했다.

RoomItem은 `_claimedByPlayerId`를 가지고 있으며, `Interlocked.CompareExchange`를 통해 아직 선점되지 않은 상태(0)일 때만 특정 PlayerId로 Claim하도록 구성했다.

```csharp
public bool TryClaim(int playerId)
{
    return Interlocked.CompareExchange(
        ref _claimedByPlayerId,
        playerId,
        0) == 0;
}
```

Pickup 요청은 먼저 Claim을 시도하고, Claim 성공 시에만 Commit 단계로 진행하도록 변경했다.

```csharp
if (!roomItem.TryClaim(player.ObjectId))
{
    return;
}
```

또한 Inventory 추가 실패 시에는 [RollbackClaim을 통해 Claim 상태를 복구](https://github.com/junsugi/ServerSkills/blob/0cfc15943585ebdd110e6ca091952e6c73dfca44/Server/Object/Item/Service/ClaimPickItemStrategy.cs#L39-L52)하도록 구성했다.
```csharp
if (!player.Inventory.TryAdd(roomItem.Item))
{
	roomItem.RollbackClaim(player.ObjectId);
}
```

---

## Metrics

### Atomic Claim 

| Check Point           | Result              |
| --------------------- | ------------------- |
| Claim Success         | 각 Room당 1건          |
| Commit Success        | Claim 성공 요청만 Commit |
| Duplicate Claims      | 0                   |
| Already Pickup Reject | 정상 처리               |
| Rollback Recovery     | 정상 복구               |
| Lost Claim            | 0                   |
#### Normal Atomic Claim Log

```text
[CLAIM_SUCCESS] roomId=2, playerId=16777230, itemId=33554533
[CLAIM_FAIL] roomId=2, playerId=16777234, itemId=33554533, ClaimedByPlayerId=16777230

[COMMIT_SUCCESS] roomId=2, playerId=16777230, itemId=33554533

[CLAIM_FAIL] roomId=2, playerId=16777238, itemId=33554533, reason=ALREADY_PICKUP
```

### Rollback Recovery 

| Metric           | Value     |
| ---------------- | --------- |
| Claim Success    | 발생        |
| Commit Fail      | 발생        |
| Rollback Success | 발생        |
| Lost Claim       | 0         |
| Consistency      | Recovered |
#### Rollback Recovery Test Log

```text
[CLAIM_SUCCESS] roomId=2, playerId=16777242, itemId=33554533

[COMMIT_FAIL] roomId=2, playerId=16777242, itemId=33554533, reason=INVENTORY_REJECTED

[ROLLBACK_SUCCESS] roomId=2, playerId=16777242, itemId=33554533
```

### Idempotency

| Metric                      | Value     |
| --------------------------- | --------- |
| Request Tracking            | Enabled   |
| Duplicate Request Detection | Enabled   |
| RequestId Reuse             | Supported |
| Lifecycle Logging           | Enabled   |
#### Idempotency Test Log

```text
[19:54:34.556][PICK_IDEMPOTENCY_BEGIN] roomId=2, playerId=16777238, itemId=33554533, requestId=777
[19:54:34.658][PICK_IDEMPOTENCY_COMPLETE] roomId=2, playerId=16777238, itemId=33554533, requestId=777, resultCode=Success
[19:54:34.659][PICK_IDEMPOTENCY_REPLAY] roomId=2, playerId=16777238, itemId=33554533, requestId=777, resultCode=Success

---

[20:02:26.171][PICK_IDEMPOTENCY_BEGIN] roomId=2, playerId=16777222, itemId=33554533, requestId=777
[20:02:26.273][PICK_IDEMPOTENCY_COMPLETE] roomId=2, playerId=16777222, itemId=33554533, requestId=777, resultCode=Success
[20:02:26.274][PICK_IDEMPOTENCY_CONFLICT] roomId=2, playerId=16777222, itemId=34554431, requestId=777, originalItemId=33554533
[20:02:26.275][PICK_IDEMPOTENCY_REPLAY] roomId=2, playerId=16777222, itemId=34554431, requestId=777, resultCode=InvalidRequest
```


---
## Observation

Atomic Claim 적용 이후, 
동일한 `RoomItem`에 대한 중복 획득은 발생하지 않았다.

각 Room에서는 하나의 요청만 Claim에 성공했으며, 
이미 Claim되었거나 Pickup이 완료된 아이템에 대한 이후 요청은 
`Claim Fail` 또는 `Already Pickup Reject`로 처리되었다.

또한 Commit 실패 상황에서는 `RollbackClaim`이 수행되어, 
Lost Claim 없이 Claim 상태가 정상적으로 복구되는 것을 확인할 수 있었다.

중복 `RequestId`를 사용하는 요청에 대해서는 기존 처리 결과를 재사용하여 
동일 요청이 반복 처리되지 않도록 Idempotency 처리가 동작하였다.

추가로 동일한 `RequestId`에 대해 서로 다른 `ItemId`가 들어오는 경우에는 
Conflict로 판단하여 요청을 거부하도록 처리하였다.

## Interpretation

Atomic Claim 구조에서는 Pickup 전체 흐름을 Lock으로 감싸지 않고,
RoomItem 단위의 Claim 상태만 원자적으로 보호하도록 변경하였다.

RoomItem은 `_claimedByPlayerId` 값을 가지고 있으며,
`Interlocked.CompareExchange` 를 통해 아직 Claim되지 않은 상태일 때만
특정 PlayerId가 Claim에 성공할 수 있도록 구성하였다.

이를 통해 여러 요청이 동시에 동일 아이템에 접근하더라도,
Claim 단계에서 단 하나의 요청만 Commit 단계로 진입할 수 있다.

또한 Claim과 Commit 단계를 분리하면서,
Inventory 추가 실패와 같은 부분 실패 상황에서는
RollbackClaim을 통해 Claim 상태를 복구할 수 있었다.

즉, Atomic Claim 구조는 Lock 기반 구조처럼 전체 Pickup 흐름을 직렬화하지 않고도,
아이템 단위로 중복 획득을 방지할 수 있는 구조이다.

## Result

Atomic Claim 구조에서는 동일 아이템에 대해 하나의 요청만 Claim에 성공하도록 제한하여,
중복 획득 없이 Pickup 정합성을 유지할 수 있었다.

또한 Claim / Commit / Rollback 단계를 분리함으로써,
Commit 실패와 같은 부분 실패 상황에서도 Claim 상태를 안전하게 복구할 수 있었다.

이를 통해 **전체 Pickup 흐름을 하나의 Lock으로 직렬화하지 않고도,**
**RoomItem 단위의 원자적 Claim 처리만으로 아이템 획득 정합성을 보장**할 수 있었다.

---

## Comparison

| Structure | Protection Scope | Main Problem | Result |
|---|---|---|---|
| Unsafe Pickup | 없음 | Check-Then-Act Race | Exception / 중복 처리 위험 |
| Lock Pickup | Pickup 전체 Lock | Lock Contention | Duplicate Claims 0 |
| Atomic Claim | RoomItem 단위 Claim | Claim 경쟁 최소화 | Duplicate Claims 0 / Rollback 가능 |

---

# Final Result

동일 아이템에 대한 동시 Pickup 요청에서는
Check와 Act가 분리될 경우 Race Condition이 발생할 수 있었다.

Lock 기반 구조는 정합성을 확보할 수 있었지만,
Pickup 전체 흐름을 직렬화하면서 확장성과 부분 실패 처리에 한계가 있었다.

Atomic Claim 구조에서는 RoomItem 단위로 Claim을 원자적으로 처리하고,
Commit 실패 시 Rollback할 수 있도록 구성하여
**전체 흐름을 Lock으로 보호하지 않고도 아이템 획득 정합성을 유지**할 수 있었다.

---

# Limitations

- 단일 서버 / 단일 프로세스 환경에서 테스트
- RoomItem 단위 Atomic Claim만 검증- DB 저장 및 영속성 처리 미포함
- 서버 재시작 / 장애 상황에서의 Claim 복구 정책 미구현
- Multi-Server 환경에서의 Distributed Claim은 미검증

---

# Future Work

- Claim Timeout 정책 추가
- Distributed Claim 실험
- Persistent Claim Log 적용
- Retry 기반 Pickup 처리
- Multi-Server Ownership 구조 실험