# Payment Processing System

A Payment Processing system is a backend service which is responsible for handling full lifecycle of financial transaction between customer and a merchant. It acts as an intermediary which ensures payment requests sent by customers are validated, processed and finalized reliably across multiple system such as payment gateways and banking networks.
The system ensures to guarantee correctness, consistency and fault tolerance in transaction handling. In real world scenarios, payment fails due to network issues, external service downtime or insufficient funds. A robust payment system ensures that such failures are handled safely, retried when appropriate and it never result in duplicate or inconsistent transactions.


## Problem 
The current problem in financial world is not having reliable and robust payment processing system. Without a structured payment processing system, every transactions become unreliable and risky. A customer may accidentally trigger duplicate payments which leads to multiple charges for a single payment. The current temporary failure such as network interruptions or service downtime can leave transaction inuncertain state which neither business owner nor customer knows whether payment succeeded or failed. Funds can be either deducted without confirmation or transaction gets stuck in mid process without clear recovery path. The lack of control and visibility can result in customer distrust, operational chaos for handling payments at scale.

