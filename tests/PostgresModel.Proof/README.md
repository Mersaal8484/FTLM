# FuelTrack PostgreSQL model proof

Run from the repository root with Docker:

```powershell
docker compose -p ftm-model-proof -f tests/PostgresModel.Proof/compose.yaml up --build --force-recreate --abort-on-container-exit --exit-code-from proof
docker compose -p ftm-model-proof -f tests/PostgresModel.Proof/compose.yaml down
```

The database uses temporary storage and fixed test-only credentials. Set the
`NUGET_SOURCE` environment variable if the default NuGet endpoint is not reachable.
The test creates the actual FuelTrack EF model in two PostgreSQL schemas, inserts
Identity users and branches, exercises connection pooling and checks PostgreSQL
privileges. It also checks the SQL Server filter expression. It is an integration
proof, not a production tenant provisioning or upgrade system. It exercises
concurrent schema upgrades and rollback, AES-GCM credential encryption, key and
password rotation, report scope filtering, notification recipient scope, and
mounted secret file validation. Existing connections are not revoked by password
rotation and must be coordinated by the production deployment.
