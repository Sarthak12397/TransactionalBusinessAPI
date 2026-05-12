# Payment Processing System

A production-style payment processing system designed to prevent duplicate charges, recover safely from transient failures, and maintain transactional correctness under failure conditions.
> Built with .NET 10 · PostgreSQL · Hangfire · Docker · Serilog

## Financial Backend System Suite

Part of a production-style fintech backend portfolio:

- Payment Processing System — transaction execution & retry orchestration
- Financial Ledger Engine — immutable double-entry source of truth
- Reconciliation Engine — cross-system consistency verification


> See also: [Data Reconciliation Engine](https://github.com/Sarthak12397/Transaction-Reconciliation-Engine)
> — the audit-side system responsible for verifying cross-system transaction consistency.

## Core Guarantees 
| Guarantee | How |
|-----------|-----| 
| Exactly-once charge behavior | Idempotency keys enforced at DB constraint level |
| No duplicate transactions under concurrent retries | Atomic ExecuteUpdateAsync — only one worker claims a transaction |
| Safe recovery from partial failures | Stuck transaction recovery job detects and reschedules stalled payments |
| No invalid transaction state under failure | State machine guards reject illegal or partial lifecycle transitions |

## Problem 
Without deterministic payment processing, financial transactions become unreliable:

- Customers get charged twice from duplicate requests  
- Network failures leave transactions in unknown states  
- No clear recovery path when payments get stuck mid-process  
- Businesses cannot determine if a failure should be retried or escalated  

## Solution
This system solves those problems through four guarantees:

- **State machine** — every transaction moves through strict, controlled 
  states. No ambiguity. No illegal jumps.
- **Idempotency** — duplicate requests return the same result. 
  Money moves once.
- **Failure classification** — transient failures are retried safely. 
  Permanent failures are escalated immediately.
- **Observability** — every action is logged with a Correlation ID 
  for full end-to-end traceability.

## Transaction Lifecycle
A transaction moves through defined states. Every transition is explicitly guarded to prevent ambiguous or partially completed payment states.
<img width="994" height="998" alt="mermaid-diagram (2)" src="https://github.com/user-attachments/assets/136b7610-0191-4fc3-be9e-49b40cfa4808" />

## Example Flow

**Creating and processing a transaction:**

### 1. Create
```json
POST /api/transactions
{
  "amount": 1000,
  "currency": "NPR",
  "idempotencyKey": "order-xyz-001",
  "description": "Order payment"
}
```
```json
{
  "transactionId": "3fa85f64-...",
  "status": "Pending",
  "retryCount": 0
}
```
### 2. Submit → system takes over

POST /api/transactions/{id}/submit

Pending → Submitted → [Hangfire fires] → Processing → Completed

### 3. If a transient failure occurs:

Processing 

→ RetryScheduled (retryCount: 1, nextRetry: +30s)

→ Processing 

→ RetryScheduled (retryCount: 2, nextRetry: +60s)

→ Completed

### 4. If a permanent failure occurs:

Processing 

→ PermanentlyFailed (reason: "Insufficient funds")

→ No retry. Escalated.

---

## Failure Handling

| Scenario | Classification | System Behavior |
|----------|---------------|-----------------|
| Duplicate request | — | Blocked via idempotency key |
| Network timeout | Transient | Retry with exponential backoff |
| External service down | Transient | Retry with exponential backoff |
| Insufficient funds | Permanent | Marked failed, no retry |
| Stuck transaction | — | Recovered via background job |


## Architecture Diagram
<img width="1889" height="2838" alt="mermaid-diagram (1)" src="https://github.com/user-attachments/assets/1572c9ef-f274-4ebd-a0b1-87e8e0947ade" />

## How to Run 
### With Docker (recommended):
```bash
git clone https://github.com/Sarthak12397/TransactionalBusinessAPI.git
cd TransactionalBusinessAPI
docker-compose up --build
```
API runs on http://localhost:8080

### Without Docker:
```bash
git clone https://github.com/Sarthak12397/TransactionalBusinessAPI.git
cd TransactionalBusinessAPI
```
Configure `appsettings.Development.json` with your PostgreSQL connection string.

```bash
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```
API runs on http://localhost:5089

Hangfire dashboard: http://localhost:5089/hangfire


## API Endpoints
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/transactions` | Create a transaction |
| GET | `/api/transactions/{id}` | Get transaction status |
| POST | `/api/transactions/{id}/submit` | Submit for processing |

### Internal Operations *(system controlled — not exposed)*
| Operation | Trigger |
|-----------|---------|
| `process` | Hangfire job after submit |
| `complete` | Hangfire job on success |
| `fail` | Hangfire job on failure classification |


## Design Decisions

| Decision | Why |
|----------|-----|
| State machine | Deterministic flow — no ambiguous states |
| Idempotency keys | Same request, same result — no double charges |
| Failure classification | Transient retried, permanent escalated |
| Correlation IDs | Full traceability across retries and logs |
| Hangfire | Persistent jobs — retries survive restarts |
| Serilog | Structured logging — queryable, not just readable |

## Scaling & Future Hardening
| Improvement          | Reason                                                                 | Impact | Priority |
|---------------------|------------------------------------------------------------------------|--------|----------|
| Event-driven messaging (Kafka / RabbitMQ) | Distributed retry orchestration across multiple services | High   | High     |
| Webhook support     | Real gateways push status updates — no polling needed                  | High   | High     |
| OpenTelemetry       | Distributed tracing beyond single-service correlation IDs              | Medium | Medium   |
| JWT Authentication  | Secure endpoints — only authenticated users create transactions        | High   | High     |
| Rate limiting       | Prevent API abuse and resource exhaustion                              | High   | High     |




