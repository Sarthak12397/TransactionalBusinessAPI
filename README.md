# Payment Processing System

A Payment Processing system is a backend service which is responsible for handling full lifecycle of financial transaction between customer and a merchant. It acts as an intermediary which ensures payment requests sent by customers are validated, processed and finalized reliably across multiple system such as payment gateways and banking networks.
The system ensures to guarantee correctness, consistency and fault tolerance in transaction handling. In real world scenarios, payment fails due to network issues, external service downtime or insufficient funds. A robust payment system ensures that such failures are handled safely, retried when appropriate and it never result in duplicate or inconsistent transactions.


## Problem 
The current problem in financial world is not having reliable and robust payment processing system. Without a structured payment processing system, every transactions become unreliable and risky. A customer may accidentally trigger duplicate payments which leads to multiple charges for a single payment. The current temporary failure such as network interruptions or service downtime can leave transaction inuncertain state which neither business owner nor customer knows whether payment succeeded or failed. Funds can be either deducted without confirmation or transaction gets stuck in mid process without clear recovery path. The lack of control and visibility can result in customer distrust, operational chaos for handling payments at scale.

## Proposed Solution
Digital payments are widely used today, yet users still face issues such as being charged twice or not knowing whether a payment went through successfully, especially during network or bank failures. Businesses often struggle to determine why a payment failed or whether it should be retried, resulting in confusion and manual intervention.

To address this, the system manages every payment through a strict state machine, ensuring each transaction moves through well-defined, controlled states and is never ambiguous or lost. Each transaction is assigned a correlation ID, enabling full traceability across services, logs, and retries, so every action can be tracked end-to-end. Failures are classified intelligently: transient issues are safely retried, while permanent failures are escalated with explicit reasons such as insufficient funds or external rejection. Combined with idempotency to prevent duplicate charges and observability for monitoring and diagnostics, this approach ensures transactions are processed in a deterministic, transparent, and fully recoverable manner.

## Transaction Lifecycle
A transaction moves through defined states:
<img width="994" height="998" alt="mermaid-diagram (2)" src="https://github.com/user-attachments/assets/136b7610-0191-4fc3-be9e-49b40cfa4808" />

## Example Flow

**Creating and processing a transaction:**

**1. Create**
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
**2. Submit → system takes over**

POST /api/transactions/{id}/submit
Pending → Submitted → [Hangfire fires] → Processing → Completed

**3. If a transient failure occurs:**

Processing → RetryScheduled (retryCount: 1, nextRetry: +30s)
→ Processing     (retryCount: 2, nextRetry: +60s)
→ Completed

**4. If a permanent failure occurs:**

Processing → PermanentlyFailed (reason: "Insufficient funds")
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


# Key Concepts

### Authorization
This is the first decision point in the transaction lifecycle. It validates whether a transaction can proceed through checking conditions such as available balance and payment method validity. 

### Clearing and Settlement
It manages the movement of funds from customer’s account to merchant’s account. This ensures that once transaction is approved, it is finalized and reflected correctly in both systems.

### State Management
It tracks the lifecycle of a transaction through clearly defined states for eg, Pending, Processing, Completed, Failed. It helps system to prevent ambiguity and ensures that every transaction is always known, recoverable state.

### Failure handling and Retries
It handles transient failures by retrying operations safely. It correctly identifies permanent failures to avoid unnecessary risks.

### Idempotency
It ensures that repeated request does not result in duplicate transactions which is critical in financial systems.

### Observability
It provides logging and monitors transaction flow, detect failures and diagnose issues in productions environments.



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





