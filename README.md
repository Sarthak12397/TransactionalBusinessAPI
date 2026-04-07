# Payment Processing System

A Payment Processing system is a backend service which is responsible for handling full lifecycle of financial transaction between customer and a merchant. It acts as an intermediary which ensures payment requests sent by customers are validated, processed and finalized reliably across multiple system such as payment gateways and banking networks.
The system ensures to guarantee correctness, consistency and fault tolerance in transaction handling. In real world scenarios, payment fails due to network issues, external service downtime or insufficient funds. A robust payment system ensures that such failures are handled safely, retried when appropriate and it never result in duplicate or inconsistent transactions.


## Problem 
The current problem in financial world is not having reliable and robust payment processing system. Without a structured payment processing system, every transactions become unreliable and risky. A customer may accidentally trigger duplicate payments which leads to multiple charges for a single payment. The current temporary failure such as network interruptions or service downtime can leave transaction inuncertain state which neither business owner nor customer knows whether payment succeeded or failed. Funds can be either deducted without confirmation or transaction gets stuck in mid process without clear recovery path. The lack of control and visibility can result in customer distrust, operational chaos for handling payments at scale.

## Proposed Solution
Digital payments are widely used today, yet users still face issues such as being charged twice or not knowing whether a payment went through successfully, especially during network or bank failures. Businesses often struggle to determine why a payment failed or whether it should be retried, resulting in confusion and manual intervention.

To address this, the system manages every payment through a strict state machine, ensuring each transaction moves through well-defined, controlled states and is never ambiguous or lost. Each transaction is assigned a correlation ID, enabling full traceability across services, logs, and retries, so every action can be tracked end-to-end. Failures are classified intelligently: transient issues are safely retried, while permanent failures are escalated with explicit reasons such as insufficient funds or external rejection. Combined with idempotency to prevent duplicate charges and observability for monitoring and diagnostics, this approach ensures transactions are processed in a deterministic, transparent, and fully recoverable manner.

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
1) Clone the repository:
```bash
 git clone https://github.com/Sarthak12397/TransactionalBusinessAPI.git
 ```

2) With Docker (recommended):
```bash 

docker-compose up --build
```
### Without Docker:

3) Configure the database connection in appsettings.json (PostgreSQL or SQL Server).
4) Apply EF Core migrations:
```bash 
dotnet ef database update
```

5) Start the application:
```bash 
dotnet run
```
6) Hangfire dashboard is available at /hangfire for monitoring background jobs. 


## API Endpoints
1) POST   /api/transactions              → Create transaction
2) GET    /api/transactions/{id}         → Get transaction status
3) POST   /api/transactions/{id}/submit  → Submit for processing
4) POST   /api/transactions/{id}/process → Process transaction
5) POST   /api/transactions/{id}/complete → Complete transaction
6) POST   /api/transactions/{id}/fail    → Fail with reason


## Design Decisions
- State Machine – Ensures deterministic transaction flow and avoids ambiguous states.
- Correlation ID – Guarantees end-to-end traceability across services, retries, and logs.
- Failure Classification & Retry Logic – Differentiates transient vs permanent failures, enabling safe retries and precise error handling.
- Idempotency – Prevents duplicate charges from retries or repeated requests.
- Observability – Integrated logging and monitoring (Serilog + Hangfire) to track and debug issues in real-time.

## What I'd Improve
- Add Kafka/RabbitMQ for distributed event handling
- Add webhook support for real payment gateway integration
- Add distributed tracing (OpenTelemetry)
- Add rate limiting on API endpoints
- Add JWT authentication






