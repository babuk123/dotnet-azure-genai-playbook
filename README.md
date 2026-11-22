# .NET · Azure · GenAI Playbook

Hands-on, runnable examples of the technologies I use as a **Senior Software Engineer (.NET full stack)**, from C# fundamentals to Azure Kubernetes Service, API Management and AI agents. Each folder pairs **working code** with a **diagram** and a short explanation that goes from basic to advanced.

> 27 topics. Built with .NET 8 (LTS), with .NET 9/10 and C# 13/14 notes, ASP.NET Core, EF Core, Angular 17+, React 18, Azure, Azure OpenAI, Jenkins and Terraform.

```mermaid
flowchart LR
    UI["Angular / React<br/>front ends"] --> APIM["Azure API Management<br/>(JWT, mTLS, rate limits)"]
    APIM --> API["ASP.NET Core APIs<br/>(.NET 8, clean architecture)"]
    API --> AKS["AKS / OpenShift<br/>containers"]
    API --> SB["Service Bus<br/>(async workflows)"]
    API --> DB[("SQL Server / DB2")]
    API --> AI["Azure OpenAI + AI Search<br/>(RAG, agents)"]
    DEVOPS["Azure DevOps<br/>CI/CD (blue-green, canary)"] -.deploys.-> AKS
```

## Contents

| # | Topic | What you will find |
|---|-------|--------------------|
| 00 | [Banking domain](00-banking-domain) | Onboarding / AML-KYC flow, idempotent party creation in C# |
| 01 | [.NET fundamentals](01-dotnet-fundamentals) | CLR, GC generations, value vs reference types, IDisposable (runnable demo) |
| 02 | [C# language](02-csharp-language) | Records, pattern matching, LINQ, async/await, Span&lt;T&gt; (runnable demo) |
| 03 | [REST API](03-rest-api) | ASP.NET Core minimal API: validation, ProblemDetails, rate limiting, output caching, health checks |
| 04 | [WCF modernization](04-wcf-modernization) | WCF contract, and the same service as a modern REST API; migration path |
| 05 | [Windows service](05-windows-service) | .NET 8 Worker Service running as a Windows service |
| 06 | [SQL Server & DB2](06-sql-server-db2) | Indexing, window functions, SQL Server → DB2 migration cheat sheet |
| 07 | [Design patterns](07-design-patterns) | Strategy, Decorator, Mediator, Outbox, Retry (runnable demo) |
| 08 | [Clean architecture](08-clean-architecture) | Domain / Application / Infrastructure / API layers |
| 09 | [Angular](09-angular) | Standalone components, signals, interceptors, OnPush |
| 10 | [React](10-react) | Hooks, data fetching with cancellation, error boundaries |
| 11 | [Containers & OpenShift](11-containers-openshift) | Container basics, .NET images (chiseled), multi-stage builds, config, probes, graceful shutdown, container-aware GC, OpenShift (runnable app) |
| 12 | [Azure services](12-azure-services) | Choosing compute & messaging, managed identity, Key Vault, Bicep |
| 13 | [AKS](13-aks) | Deployment, HPA, ingress, workload identity, canary release |
| 14 | [API Management](14-api-management) | JWT validation, rate limiting, mTLS client certificates, AI gateway policies |
| 15 | [Azure DevOps](15-azure-devops) | YAML pipelines, blue-green vs canary deployments |
| 16 | [Azure OpenAI & RAG](16-azure-openai-rag) | Chat, embeddings, retrieval-augmented generation with Azure AI Search |
| 17 | [AI agents](17-ai-agents) | Agent loop with tool calling; human-in-the-loop design for banking |
| 18 | [C# coding interview](18-csharp-coding-interview) | 22 problems (basic → advanced) with tested solutions |
| 19 | [What's new .NET 8 → 10](19-dotnet-9-10-whats-new) | C# 12/13/14 and .NET 9/10 features, upgrade checklist (runnable on .NET 8 + .NET 10 file) |
| 20 | [EF Core](20-ef-core) | Fluent config, concurrency tokens, no-tracking, N+1, compiled queries, ExecuteUpdate, migrations |
| 21 | [Functions, Service Bus, Container Apps, Data Factory](21-azure-functions-messaging) | Isolated-worker Functions, dead-lettering, KEDA scale-to-zero, DB2 → Azure SQL pipeline |
| 22 | [API security: OAuth 2.0, JWT, mTLS](22-api-security-oauth-jwt-mtls) | How JWT validation works (runnable), policies, Entra ID + client certificates |
| 23 | [DDD, CQRS, saga, resilience](23-ddd-cqrs-saga-resilience) | Aggregates, value objects, domain events, read models, saga compensation, circuit breaker (runnable) |
| 24 | [Jenkins, Terraform, SonarQube](24-jenkins-terraform-sonarqube) | Jenkinsfile with quality gate and scan, Terraform for AKS + ACR |
| 25 | [Healthcare domain](25-healthcare-domain) | FHIR R4, HL7 v2, LOINC, PHI masking, HIPAA (runnable) |
| 26 | [GenAI PoCs & GitHub Copilot](26-genai-poc-pdf-security-copilot) | PDF chart/table extraction, AI-assisted vulnerability remediation (runnable), Copilot practices |


## Skills → where to find them

| Skill | Folder(s) |
|-------|-----------|
| C#, .NET 8/10, ASP.NET Core Web API | 01, 02, 03, 19 |
| EF Core, SQL Server, DB2 | 20, 06 |
| WCF modernization, Windows services | 04, 05 |
| Angular, React, TypeScript | 09, 10 |
| AKS, Docker, Kubernetes, OpenShift | 11, 13 |
| API Management, OAuth 2.0 / JWT, mTLS | 14, 22 |
| Functions, Service Bus, Key Vault, Data Factory, Container Apps | 12, 21 |
| Azure OpenAI, AI Search (RAG), AI agents | 16, 17, 26 |
| Microservices, DDD, CQRS/Saga, event-driven, resilience | 07, 08, 23 |
| Azure DevOps, Jenkins, Terraform, SonarQube | 15, 24 |
| GitHub Copilot | 26 |
| Banking and healthcare domains | 00, 25 |

## Run the .NET samples

```bash
# .NET 8 SDK required
dotnet run --project 00-banking-domain
dotnet run --project 01-dotnet-fundamentals
dotnet run --project 02-csharp-language
dotnet run --project 03-rest-api            # then call the endpoints listed in 03-rest-api/README.md
dotnet run --project 07-design-patterns
dotnet run --project 11-containers-openshift/ContainerApp   # then /runtime and /health/ready
dotnet run --project 08-clean-architecture      # POST /api/loans
dotnet run --project 16-azure-openai-rag        # offline RAG flow; azure/ has the Azure OpenAI + AI Search version
dotnet run --project 17-ai-agents               # agent loop with human approval
dotnet run --project 22-api-security-oauth-jwt-mtls   # see requests.http
dotnet run --project 23-ddd-cqrs-saga-resilience
dotnet run --project 25-healthcare-domain
dotnet run --project 18-csharp-coding-interview   # runs all solutions with self-checks
```

## About me

Senior Software Engineer with 15+ years in .NET full stack development for banking/wealth management and healthcare: high-volume reporting platforms, onboarding and AML/KYC platforms, cloud migrations to Azure, and GenAI/agent proofs of concept.

[LinkedIn](https://www.linkedin.com/in/babu-krishnan) · AZ-400 · AZ-104 · AZ-900 · Certified Scrum Developer

> All examples are generic and written for learning; they contain no client or employer code or data.
