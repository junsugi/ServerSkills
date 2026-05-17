# Scenario 1 — EnterGame 동시성
## Intro

```text
동시에 많은 플레이어가 EnterGame 요청을 보낼 경우 발생하는  
동시성 문제와 Queue 병목을 재현하고,  
Ownership 기반 구조로 개선한 시나리오입니다.
```

---

# Goal

다음 문제를 재현하고 개선하는 것을 목표로 했습니다.

- Shared State 동시 접근
- Lock Contention
- Queue Throughput Bottleneck
- Tail Latency 증가
- Single Queue 병목

또한 Avg뿐 아니라 P95 / P99 latency를 함께 측정해  
실제 서버 환경에서 발생하는 Tail Latency 문제를 확인했습니다.

---

# Test Environment

| Category           | Value                       |
| ------------------ | --------------------------- |
| Runtime            | .NET 10                     |
| Protocol           | TCP                         |
| Serialization      | Protobuf                    |
| Client             | DummyClient                 |
| Concurrent Clients | 1000                        |
| Metrics            | RTT / QueueWait / P95 / P99 |

---

# EnterGame Flow

## EnterGame Ownership Evolution


![EnterGameFlow](./Images/Scenario1/enter_game_flow.svg)

---
# Problem

1000개의 클라이언트가 동시에 EnterGame 요청을 보낼 경우,
여러 스레드가 동시에 `ObjectManager` 내부 Shared State(`Player Dictionary`) 에 접근했다.

**초기 구조에서는 Shared State 접근 범위가**  
**하나의 Global Ownership 영역으로 구성되어 있었다.**

동기화 없이 처리할 경우 **Race Condition** 이 발생하였고,
이를 해결하기 위해 **다양한 동시성 처리 구조를 적용**했다.

```csharp
private Dictionary<int, Player> _players = new();

public void Add()
{
	_players.Add(playerId, player);
}
```

---

# Step 1 — Direct Lock

## Structure

모든 `EnterGame` 요청은 동일한 `Player Dictionary` 에 접근하며,
Shared State 보호를 위해 하나의 [Global Lock](https://github.com/junsugi/ServerSkills/blob/98a7835e3c45e549484b0975e9fdbfdbb3965b01/Server/Object/ObjectManager.cs#L19-L31)을 사용하였다.


```csharp
// ObjectManager.cs
public class ObjectManager 
{
	public void Add()
	{
		lock (_lock)
		{
		    _players.Add(playerId, player);
		    Thread.Sleep(100); // 테스트용 병목
		}
	}
}
```


---
## Metrics

| Phase          | Avg RTT |   P95 |   P99 |
| -------------- | ------: | ----: | ----: |
| Initial Load   |   395ms | 646ms | 646ms |
| Mid Load       |   552ms | 760ms | 766ms |
| Sustained Load |   588ms | 790ms | 795ms |
각 Phase는 부하 테스트 중 시간 흐름에 따라 초반 / 중간 / 후반 구간에서 선택한 대표 측정값이다.


![DirectLockAvgRtt](./Images/Scenario1/DirectLock/enter_game_latency_trend.png)
## Observation

부하가 증가할수록 Avg RTT와 P95/P99 RTT가 함께 증가했으며,  
특히 Sustained Load 구간에서는 Tail Latency 증가 폭이 더욱 크게 나타났다.

또한 Avg RTT 대비 P95/P99 RTT 상승 폭이 더 크게 나타나면서,  
일부 요청이 장시간 대기하는 현상이 확인되었다.

## Interpretation

Direct Lock 구조에서는 모든 `EnterGame` 요청이  
동일한 `Player Dictionary` 접근을 위해 하나의 Global Lock을 순차적으로 획득해야 했다.

동시 요청 수가 증가할수록 Lock Wait 시간이 누적되었고,  
결과적으로 Global Lock 기반 직렬화 구조 자체가 병목 지점으로 동작하였다.

## Result

Direct Lock 구조에서는 데이터 정합성은 유지할 수 있었지만,
모든 요청이 하나의 Global Lock에서 직렬화되면서
동시 요청 증가 시 Lock Wait과 Tail Latency가 함께 증가하였다.


---
# Step 2 — Single JobQueue

## Structure

모든 Shared State 변경 작업은 
[ObjectManager의 Signle JobQueue 내부](https://github.com/junsugi/ServerSkills/blob/98a7835e3c45e549484b0975e9fdbfdbb3965b01/Server/Object/ObjectManager.cs#L38-L54)에서만 처리되도록 변경하였다.

```csharp
public class ObjectManager : JobSerializer
{
	public void AddQueue()
	{
		Push(() =>
		{
		    _players.Add(playerId, player);
		    Thread.Sleep(100);  // 테스트용 병목
		});
	}
}
```



---

## Metrics

| Phase          | QueueWait Avg | Execute Avg | ResponseTotal Avg |    P95 |    P99 |    Max |
| -------------- | ------------: | ----------: | ----------------: | -----: | -----: | -----: |
| Initial Load   |         372ms |       100ms |             473ms |  696ms |  707ms |  707ms |
| Mid Load       |         614ms |       100ms |             715ms |  917ms |  923ms |  923ms |
| High Load      |         850ms |       100ms |             951ms | 1144ms | 1151ms | 1151ms |
| Sustained Load |        1105ms |       100ms |            1206ms | 1409ms | 1417ms | 1417ms |
| Peak           |        1164ms |       100ms |            1265ms | 1422ms | 1422ms | 1422ms |

![JobQueue Client RTT](./Images/Scenario1/JobQueue/single_jobqueue_clean_line_breakdown.png)

![JobQueue Client RTT](./Images/Scenario1/JobQueue/single_jobqueue_latency_trend.png)

## Observation

Execute 시간은 약 100ms 수준으로 비교적 일정하게 유지되었지만,
부하가 증가할수록 QueueWait 시간이 지속적으로 증가하는 현상을 확인할 수 있었다.

또한 QueueWait 증가와 함께 Client RTT 및 P95/P99 RTT 역시 지속적으로 증가하였다.

특히 Avg RTT보다 P95/P99 RTT 증가 폭이 더 크게 나타나면서,
일부 요청이 Queue 내부에서 장시간 대기하는 현상이 발생하였다.

## Interpretation

Single JobQueue 구조에서는 Shared State 변경 작업을  
하나의 Queue에서 순차적으로 처리하도록 구성하여,  
Lock 기반 동시 접근 문제를 제거하고 상태 변경을 안전하게 처리할 수 있었다.

하지만 모든 요청을 하나의 Queue에서 순차적으로 처리하는 구조였기 때문에,  
요청 유입량이 Queue 처리 속도를 초과하는 순간 Queue Backlog가 점진적으로 누적되었다.

결과적으로 Lock Contention은 감소했지만,  
Global Ownership 범위 자체는 유지되고 있었기 때문에  
단일 Queue가 새로운 병목 지점으로 동작하였다.

## Result

Single JobQueue 구조에서는 Lock 기반 동시 접근 문제를 제거할 수 있었지만,
모든 요청이 하나의 Queue에서 직렬화되면서
단일 Queue 자체가 새로운 병목 지점으로 동작하였다.

---

# Step 3 — GameRoom Ownership Partitioning

## Structure

기존에는 하나의 공유 영역에서 모든 플레이어 상태를 처리했다면, 
이 단계에서는 [Ownership 범위를 Room 단위](https://github.com/junsugi/ServerSkills/blob/98a7835e3c45e549484b0975e9fdbfdbb3965b01/Server/Game/Room/GameRoom.cs#L69-L93)로 분리했다.

각 `Room`은 독립적인 `State` 와 `Queue` 를 소유한다.
따라서 같은 `Room` 안의 작업은 순차적으로 처리되고, 
서로 다른 `Room` 은 독립적으로 실행될 수 있다.

```csharp
Dictionary<int, GameRoom> gameRooms = GameRoomManager.Instance.Creates(4);

public GameRoom PickRoom(Player player)
{
    int index = Math.Abs(player.ObjectId) % _rooms.Count;
    return _rooms[index];
}
```

플레이어는 `ObjectId % RoomCount` 방식으로 특정 Room에 배정된다.


```csharp
public class GameRoom(...) : JobSerializer
{
    private Dictionary<int, Player> _players = new();
    private Dictionary<int, RoomItem> _roomItems = new();
}
```

각 Room은 독립적인 JobQueue와 Room State를 소유한다.


```csharp
public void EnterGame(...)
{
    Push(() =>
    {
        _players.Add(player.ObjectId, player);
        player.GameRoom = this;
        Thread.Sleep(100);  // 테스트용 병목
    });
}
```

Room State를 변경하는 작업은 직접 실행하지 않고, 
해당 Room의 Queue에 Job으로 넣는다.


```csharp
foreach (GameRoom gameRoom in gameRooms.Values)  
{  
    Task.Run(async () =>  
    {  
        while (true)  
        {   try  
            {  
                gameRoom.Flush();  
                await Task.Delay(1);  
            } 
            catch (Exception e)  
            {                
	            Console.WriteLine(e);  
            }        
        }    
    });
}
```

각 Room은 독립적인 `Flush()` 루프를 가지며,  
서로 다른 Room은 병렬로 작업을 처리할 수 있다.

---

## Metrics

| Metric | Min | Max | Mean |
|---|---:|---:|---:|
| Avg RTT | 113.67ms | 121.86ms | 117.06ms |
| P95 RTT | 164ms | 202ms | 181.95ms |
| P99 RTT | 166ms | 209ms | 189.10ms |
Single JobQueue 단계에서 관찰되었던 지속적인 RTT 증가 현상이 크게 완화되었다.


![Partitioned RTT Trend](./Images/Scenario1/GameRoom/partitioned_rtt_trend.png)



![Partitioned RTT Trend](./Images/Scenario1/GameRoom/per_room_latency_stability.png)


## Observation

1000개의 클라이언트가 동시에 요청을 보내는 상황에서도  
Avg RTT와 P95/P99 RTT는 비교적 안정적으로 유지되었으며,  
이전 단계에서 관찰되었던 지속적인 RTT 증가 현상은 크게 완화되었다.

Per-Room Latency 그래프에서도 Room 간 Latency 편차가 크지 않았으며,  
현재 `ObjectId % RoomCount` 분산 조건에서는  
특정 Room의 Latency가 과도하게 튀는 현상은 관찰되지 않았다.

## Interpretation

GameRoom Ownership Partitioning 이후에는
Ownership 범위를 Room 단위로 분리하였다.

각 Room은 독립적인 Queue 내부에서 상태 변경 작업을 처리하며,
서로 다른 Room 간 작업은 병렬로 실행될 수 있도록 구성하였다.

## Result

GameRoom Ownership Partitioning 이후에는
Ownership 범위를 Room 단위로 분리하면서
Queue 병목이 여러 Room으로 분산되었고,
동시 요청 상황에서도 안정적인 RTT와 Tail Latency를 유지할 수 있었다.

---
## Comparison

| Structure             | Ownership Scope | Main Bottleneck   | Result                |
| --------------------- | --------------- | ----------------- | --------------------- |
| Direct Lock           | Global          | Lock Wait         | RTT / Tail Latency 증가 |
| Single JobQueue       | Global          | QueueWait         | RTT 지속 증가             |
| GameRoom Partitioning | Per Room        | Distributed Queue | RTT 안정화               |

![Client Avg RTT Comparison](./Images/Scenario1/Comparison/entergame_avg_comparison.png)

![Client Avg RTT Comparison](./Images/Scenario1/Comparison/entergame_p99_comparison.png)
## Analysis

Direct Lock과 Single JobQueue 구조는
구현 방식은 서로 달랐지만,
모두 하나의 Global Ownership 영역 내부에서 요청을 직렬화한다는 공통점이 있었다.

Direct Lock 구조에서는 Lock Wait이 주요 병목으로 나타났으며,
Single JobQueue 구조에서는 QueueWait이 새로운 병목으로 전환되었다.

즉, Lock 제거만으로는 병목 문제를 근본적으로 해결할 수 없었으며,  
모든 요청이 하나의 직렬화된 처리 경로를 공유하는 구조 자체가  
Scalability 제한의 원인이었다.

반면 GameRoom Ownership Partitioning 이후에는
Ownership 범위를 Room 단위로 분리하면서
작업 처리 경로를 여러 Queue로 분산할 수 있었다.

그 결과 Queue Backlog 누적 현상이 완화되었고,
동시 요청 상황에서도 Avg RTT 및 Tail Latency가
이전 단계 대비 안정적으로 유지되는 것을 확인할 수 있었다.

---

## Final Result

동시성 문제 해결 과정에서
단순 Lock 제거보다 Ownership 범위를 적절히 분리하는 구조가
Scalability와 Tail Latency 안정화에 더 효과적임을 확인할 수 있었다.


---
# Limitations

- 실제 DB 대신 인위적인 처리 지연(Thread.Sleep 100ms) 사용
- 단일 머신 환경에서 테스트  
- Room 분산 기준이 단순 PlayerId % RoomCount 기반

---

# Future Work

- Room별 QueueWait 모니터링 추가  
- Dynamic Room Balancing 적용  
- Room별 부하 기반 Ownership 재분배 실험  
- Multi-Thread Flush 구조 실험