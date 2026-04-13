# Payment Processing System

A backend API that manages the full lifecycle of a financial transaction — 
from creation to final settlement — with guaranteed consistency, 
fault tolerance, and zero duplicate charges.
> Built with .NET 10 · PostgreSQL · Hangfire · Docker · Serilog


## Problem 
Without a structured payment system, transactions become unreliable:

- Customers get charged twice from duplicate requests
- Network failures leave transactions in unknown states
- No clear recovery path when payments get stuck mid-process
- Businesses cannot determine if a failure should be retried or escalated

## Proposed Solution
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
A transaction moves through defined states:
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

###3. If a transient failure occurs:

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
- State Machine – Ensures deterministic transaction flow and avoids ambiguous states.
- Correlation ID – Guarantees end-to-end traceability across services, retries, and logs.
- Failure Classification & Retry Logic – Differentiates transient vs permanent failures, enabling safe retries and precise error handling.
- Idempotency – Prevents duplicate charges from retries or repeated requests.
- Observability – Integrated logging and monitoring (Serilog + Hangfire) to track and debug issues in real-time.

## What I'd Improve
- **Kafka/RabbitMQ** → Replace Hangfire with event-driven messaging for distributed retry at scale
- **Webhook support** → Real payment gateways push status updates via webhooks instead of polling
- **OpenTelemetry** → Distributed tracing across services beyond single-service correlation IDs  
- **Rate limiting** → Protect API from abuse and prevent resource exhaustion
- **JWT Authentication** → Secure endpoints so only authenticated users can create transactions





