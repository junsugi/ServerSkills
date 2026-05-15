# Real-Time TCP Game Server Portfolio

C# 기반 실시간 게임 서버에서 발생하는  
동시성 문제와 병목을 재현하고,  
측정 기반으로 개선 과정을 기록한 프로젝트입니다.

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

| Metric           | Description                     |
| ---------------- | ------------------------------- |
| RTT              | Client ↔ Server Round Trip Time |
| QueueWait        | JobQueue 대기 시간                  |
| Execute          | 실제 작업 처리 시간                     |
| P95 / P99        | Tail Latency 측정                 |
| Broadcast Count  | 전송 fan-out 측정                   |
| DoubleGrantCount | Race Condition 검증               |

---

# Scenarios

## Scenario 1 — EnterGame 동시성

동시에 1000개의 클라이언트에서 접속할 경우 발생하는  
Lock Contention 및 Queue 병목을 재현하고 개선했습니다.

### Topics

- Direct Lock
- Single JobQueue
- GameRoom Sharding
- Ownership Partitioning
- QueueWait / RTT / P99 분석
### Results

- Lock Contention 감소
- Queue Ownership 분산
- P95/P99 Latency 안정화

→ [Scenario 1 Detail](Docs/scenario1-enter-game.md)

---

## Scenario 2 — AOI Broadcast 최적화

1000개의 클라이언트의 이동 패킷에 대한 Broadcast fan-out 비용을 측정하고,  
Distance-based AOI(Interest Management)를 적용해 네트워크 전송 비용을 감소시켰습니다.

### Topics

- Naive Broadcast
- Distance-based AOI  
- Interest Management
- Fan-out Reduction

### Results

- Broadcast 대상 감소
- Send packet 비용 감소
- RTT 안정화

→ [Scenario 2 Detail](Docs/scenario2-aoi-broadcast.md)

---

## Scenario 3 — Item Pickup Race Condition

동시에 동일 아이템을 획득하는 상황에서 발생하는  
Race Condition을 재현하고,  
Atomic Claim 및 Server-authoritative 처리로 해결했습니다.

### Topics

- Check-Then-Act Race
- Atomic Claim
- Claim / Commit / Rollback
- Idempotency
- Conflict Resolution

### Results

- DoubleGrantCount 0건 달성
- 중복 요청 방어
- 정합성 보장

→ [Scenario 3 Detail](Docs/scenario3-item-pickup.md)

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
