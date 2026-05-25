# Block 7 — Background Processing

> 🔒 This file will be filled after Block 7 is reviewed.

## Topics That Will Be Covered

- What is a BackgroundService in .NET
- `IHostedService` vs `BackgroundService`
- System.Threading.Channels — producer/consumer queue
- Why we use a Channel instead of a simple List or Queue
- Thread safety — why you can't share a plain List between threads
- How to enqueue a task after saving to DB (fire and forget vs reliable)
- What "simulated processing" means in this context
- The difference between in-process queues (Channels) and out-of-process queues (RabbitMQ, Azure Service Bus)
- When you'd graduate from BackgroundService to a real queue broker
