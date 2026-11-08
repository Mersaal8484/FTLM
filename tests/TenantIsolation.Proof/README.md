# PostgreSQL tenant isolation proof

This is an isolated proof, not the application's tenant implementation. It uses
a disposable PostgreSQL database in `tmpfs`; it does not touch the application's
SQL Server database or create persistent volumes. The fixed passwords in the
Compose file are test fixtures only.

From the repository root:

```powershell
docker compose -p ftm-tenant-proof -f tests/TenantIsolation.Proof/compose.yaml up --build --force-recreate --abort-on-container-exit --exit-code-from proof
docker compose -p ftm-tenant-proof -f tests/TenantIsolation.Proof/compose.yaml down
```

If the normal NuGet endpoint is unavailable, set `NUGET_SOURCE` in the shell
before running the first command. A successful run exits with code 0 and prints
`PASS`. It verifies tenant-scoped PostgreSQL roles, unqualified EF Core queries,
concurrent reads with connection pooling, concurrent idempotent schema upgrades,
rollback of failed DDL and history, platform EF migration and unique hosts, and
host/schema identifier rules. It does not
prove that the application's Identity, EF migrations, jobs, API or admin UI are
tenant-safe; those must be implemented and tested separately.
