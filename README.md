# 🚀 ProjectSaaS — Enterprise Document Management Platform

A **production-grade, multi-tenant SaaS platform** built using modern **.NET microservices architecture**, designed to demonstrate **real-world backend engineering practices**.

---

## 📌 Overview

ProjectSaaS is an **enterprise document management system** that enables:

* Multi-tenant document storage
* Workflow-based approval systems
* Event-driven processing
* Real-time notifications
* Scalable microservices architecture

This project is built to reflect **industry-level system design**, not just CRUD applications.

---

## 🏗️ Architecture

### High-Level Flow

```
Client (React)
      │
      ▼
API Gateway (YARP)
      │
 ┌────┼───────────────────────────────┐
 ▼    ▼         ▼         ▼           ▼
Identity  Document  Workflow  Notification
Service   Service   Service   Service
```

---

### Architecture Pattern

* **Clean Architecture**
* **CQRS (Command Query Responsibility Segregation)**
* **Event-Driven Microservices**
* **API Gateway Pattern (YARP)**

Layer Structure:

```
Domain → Application → Infrastructure → API
```

---

## 🧰 Tech Stack

### Backend

* **ASP.NET Core 8**
* **Entity Framework Core + Dapper**
* **MediatR (CQRS)**
* **MassTransit + RabbitMQ**
* **Hangfire (Background Jobs)**

### Infrastructure

* PostgreSQL (Document DB)
* SQL Server (Identity DB)
* Redis (Caching & Upload Sessions)
* MinIO (Object Storage)
* MailHog (Email Testing)
* Seq (Logging)

### Frontend

* React 18 (Planned)
* Zustand + TanStack Query

### DevOps

* Docker & Docker Compose
* Health Checks
* Integration Testing

---

## 🔑 Key Features

### 🔐 Authentication & Multi-Tenancy

* JWT-based authentication
* Tenant isolation using:

  * Repository filtering
  * PostgreSQL Row-Level Security (RLS)

---

### 📂 Document Management

* Chunked file uploads (resumable)
* Versioning support
* MinIO-based storage

---

### 🔄 Workflow Engine

* Multi-stage approval workflows
* SLA-based escalation (Hangfire jobs)
* State machine enforcement (Stateless)

---

### 📩 Notifications System

* Event-driven notifications via RabbitMQ
* Email delivery using MailKit
* In-app notifications with read/unread tracking

---

### ⚡ Event-Driven Architecture

* Integration events via MassTransit
* Loose coupling between services
* Asynchronous processing

---

## 🔌 Services Overview

| Service              | Port | Responsibility       |
| -------------------- | ---- | -------------------- |
| Gateway (YARP)       | 5000 | Entry point, routing |
| Identity Service     | 5001 | Auth, JWT            |
| Document Service     | 5002 | File handling        |
| Workflow Service     | 5003 | Approval engine      |
| Notification Service | 5004 | Alerts & emails      |

---

## ⚙️ How to Run Locally

### 1. Start Infrastructure

```bash
cd infrastructure
docker-compose up -d
```

---

### 2. Start Services

Run each service in separate terminals:

```bash
dotnet run (inside each service API folder)
```

---

### 3. Verify Health

```bash
http://localhost:5001/health
http://localhost:5002/health
http://localhost:5003/health
http://localhost:5004/health
```

---

### 4. Access Gateway

```bash
http://localhost:5000
```

---

## 🧪 Testing

### Integration Tests

```bash
dotnet test tests/integration
```

* ✅ 17/17 tests passing
* Covers full pipeline:

  * Upload → Workflow → Notification

---

## 📊 Engineering Practices

* ✅ Clean Architecture separation
* ✅ CQRS pattern (EF Core + Dapper)
* ✅ Result pattern (no exceptions for business logic)
* ✅ Domain-driven design principles
* ✅ Integration testing
* ✅ Structured logging (Seq)
* ✅ Background jobs (Hangfire)

---

## ⚠️ Key Engineering Decisions

### Why Clean Architecture?

Ensures:

* Separation of concerns
* Testability
* Maintainability

---

### Why API Gateway (YARP)?

* Centralized routing
* Simplified frontend integration
* Solves CORS issues
* Enables security enforcement

---

### Why Event-Driven Design?

* Decouples services
* Improves scalability
* Enables async workflows

---

## 📈 Scalability Considerations

* Horizontal scaling via microservices
* Message queue (RabbitMQ) for async processing
* Redis caching layer
* Stateless services (scalable containers)

---

## 🔮 Roadmap

### Week 5+

* React frontend UI
* Real-time updates (SignalR)
* Elasticsearch search

### Week 7+

* OpenTelemetry tracing
* Resilience with Polly

### Week 8+

* Load testing (k6)
* Audit service
* Production hardening

---

## 📸 Demo (Planned)

* API demo via Postman
* UI walkthrough
* Workflow execution demo

---

## 💡 Why This Project Stands Out

This is **not a simple CRUD project**.

It demonstrates:

* Microservices architecture
* Distributed systems design
* Background processing
* Event-driven workflows
* Real-world SaaS patterns

---

## 👨‍💻 Author

**Mohammed Rahees**

---

## ⭐ Recruiter Notes

This project highlights:

* Backend system design skills
* Enterprise-grade architecture
* Production-ready engineering practices
* Real-world problem solving

---
