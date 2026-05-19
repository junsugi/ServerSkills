# Real-Time TCP Game Server Portfolio

C# TCP 기반 게임 서버에서 동시 요청, Broadcast 비용, 동일 아이템 중복 획득 문제를 재현하고 개선한 프로젝트입니다.

단순히 기능을 구현하는 것보다 “왜 이런 구조가 필요한가”를 확인하는 데 집중했습니다.  
Lock 기반 처리의 한계, Single JobQueue 병목, Room 단위 Ownership 분리, 
AOI Broadcast, Atomic Claim / Idempotency 처리를 직접 비교하며 게임 서버 구조의 필요성을 검증했습니다.

강의 기반 ServerCore 구조를 참고했으며,
본 프로젝트에서는 다음 부분을 직접 확장했습니다.

- EnterGame 동시 요청 부하 테스트
- QueueWait / RTT / P95 / P99 측정
- Room 단위 Ownership Partitioning 비교
- AOI Broadcast 비용 측정
- Item Pickup Race Condition / Idempotency 실험

---

# Overview

이 프로젝트는 단순 기능 구현보다 다음 문제를 해결하는 데 집중했습니다.

- Shared State 동시 접근
- Lock Contention
- Queue Throughput Bottleneck
- Ownership Partitioning
- Broadcast Fan-out Cost
- Race Condition

각 시나리오에서는 문제를 재현한 뒤,  
측정 가능한 지표를 기반으로 개선 과정을 검증했습니다.

---

# Tech Stack

| Category      | Stack                       |
| ------------- | --------------------------- |
| Runtime       | .NET10 / C#                 |
| Network       | TCP / SocketAsyncEventArgs  |
| Serialization | Protobuf                    |
| Concurrency   | JobQueue 기반 Room Ownership  |
| Architecture  | PacketSession / Handler 구조  |
| Testing       | DummyClient 기반 부하 테스트       |
| Metrics       | RTT / QueueWait / P95 / P99 |

---

# Architecture

![Architecture](Docs/Images/architecture.svg)

---
# Key Metrics

| Metric            | Description                     |
| ----------------- | ------------------------------- |
| RTT               | Client ↔ Server Round Trip Time |
| QueueWait         | JobQueue 대기 시간                  |
| Execute           | 실제 작업 처리 시간                     |
| P95 / P99         | Tail Latency 측정                 |
| Avg Sends/sec     | 초당 평균 패킷 전송 횟수                  |
| Avg Recipients    | 요청 1건당 평균 수신자 수                 |
| Duplicate Claims  | 동일 아이템 중복 획득 검증                 |
| Rollback Recovery | Commit 실패 후 Claim 복구 검증         |

---

# Scenarios

## Scenario 1 — EnterGame 동시성

동시에 1000개의 클라이언트가 EnterGame 요청을 보낼 때 발생하는  
Shared State 동시 접근, Lock Contention, Single Queue 병목을 재현하고,  
GameRoom 단위 Ownership Partitioning으로 개선했습니다.

### Topics
  
- Direct Lock  
- Single JobQueue  
- GameRoom Ownership Partitioning  
- QueueWait / RTT / P95 / P99 분석
### Results

- Global Lock 기반 병목 확인  
- Single JobQueue의 QueueWait 누적 확인  
- Room 단위 Ownership 분리로 Queue 병목 분산  
- P95/P99 Tail Latency 안정화

→ [Scenario 1 Detail](Docs/scenario1-enter-game.md)

---

## Scenario 2 — AOI Broadcast 최적화

100, 200, 300, 400, 500명의 플레이어가 이동 패킷을 전송할 때 발생하는 
Broadcast fan-out 비용을 측정하고,  
500명 기준으로 Full Broadcast와 Range-based AOI 적용 구조를 비교했습니다.
### Topics

- Full Broadcast  
- Range-based AOI  
- Interest Management  
- Fan-out Reduction  
- Avg Recipients / Avg Sends/sec 분석

### Results

- Full Broadcast 구조에서 플레이어 수 증가에 따른 Send/sec 증가 확인  
- 500명 기준 Avg Recipients 120.99명 → Room별 약 2.1~2.45명 수준으로 감소  
- Broadcast Fan-out 및 불필요한 네트워크 전송 비용 감소  
- QueueWait / RTT 일부 개선

→ [Scenario 2 Detail](Docs/scenario2-aoi-broadcast.md)

---

## Scenario 3 — Item Pickup Race Condition

동시에 동일 아이템을 획득하는 상황에서 발생하는  
Race Condition을 재현하고,  
Atomic Claim / Commit / Rollback 구조로 정합성을 보장했습니다.

### Topics

- Check-Then-Act Race
- Atomic Claim
- Claim / Commit / Rollback
- Idempotency
- Conflict Resolution

### Results

- Unsafe Pickup 구조에서 Race Condition 재현  
- Lock 기반 처리로 Duplicate Claims 0건 유지  
- Interlocked 기반 Atomic Claim으로 RoomItem 단위 Claim 보장  
- Commit 실패 시 RollbackClaim으로 Claim 상태 복구  
- 중복 RequestId 기반 Idempotency 처리

→ [Scenario 3 Detail](Docs/Scenario3-item-race-condition.md)

---

# Project Structure

```text
ServerSkills/
│
├─ README.md
│
├─ Docs/
│  ├─ scenario1-enter-game.md
│  ├─ scenario2-aoi-broadcast.md
│  ├─ scenario3-item-pickup.md
│  └─ Images/
│
├─ Server/
├─ DummyClient/
├─ ServerCore/
└─ Common/
```

# References

- [[C#과 유니티로 만드는 MMORPG 게임 개발 시리즈] Part4: 게임 서버](https://www.inflearn.com/course/%EC%9C%A0%EB%8B%88%ED%8B%B0-mmorpg-%EA%B0%9C%EB%B0%9C-part4/dashboard?cid=324941)
- [[C#과 유니티로 만드는 MMORPG 게임 개발 시리즈] Part7: MMO 컨텐츠 구현 (Unity + C# 서버 연동 기초)](https://www.inflearn.com/course/%EC%9C%A0%EB%8B%88%ED%8B%B0-mmorpg-%EA%B0%9C%EB%B0%9C-part7/dashboard?cid=325662)
- [[C#과 유니티로 만드는 MMORPG 게임 개발 시리즈] Part9: MMO 컨텐츠 구현 (DB연동 + 대형 구조 + 라이브 준비)](https://www.inflearn.com/course/%EC%9C%A0%EB%8B%88%ED%8B%B0-mmorpg-%EA%B0%9C%EB%B0%9C-part9/dashboard?cid=325882)
