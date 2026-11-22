# 04 · WCF Services and Modernization

- [`legacy/IReportService.cs`](legacy/IReportService.cs): a classic WCF service (contracts, fault contract, bindings).
- [`Program.cs`](Program.cs): the same operation as an ASP.NET Core 8 REST API (`dotnet run`, then `POST /api/v1/reports/drawdown`).

```mermaid
flowchart LR
    subgraph Before
      C1["Clients (SOAP)"] --> W["WCF service<br/>.NET Framework · IIS"]
    end
    subgraph Transition["Strangler pattern"]
      C2["Clients"] --> GW["API gateway / router"]
      GW -->|migrated ops| N["New REST/gRPC service<br/>.NET 8 · containers"]
      GW -->|not yet migrated| W2["Legacy WCF"]
    end
    subgraph After
      C3["Clients (REST/JSON)"] --> N2[".NET 8 service on AKS/OpenShift"]
    end
    Before --> Transition --> After
```

## Basic
- **ABC**: Address (where), Binding (how: `basicHttpBinding`, `wsHttpBinding`, `netTcpBinding`), Contract (what: `ServiceContract`, `OperationContract`, `DataContract`).
- Errors: `FaultException<T>` + `[FaultContract]`; never expose exception details in production.

## Intermediate
- Instancing: **PerCall** (stateless, scalable), PerSession, Single. Concurrency: Single / Reentrant / Multiple.
- Security: transport (HTTPS), message (WS-Security), certificates, Windows auth.

## Advanced — modernization options
| Option | When |
|-------|------|
| **CoreWCF** | Keep SOAP contracts, move to .NET 8 with minimal code change |
| **gRPC** | Fast internal service-to-service calls, contract-first |
| **REST/JSON** | Broad consumers, browsers, API gateway features (this sample) |

Run old and new side by side, compare responses, migrate consumers gradually, then retire the WCF endpoint.
