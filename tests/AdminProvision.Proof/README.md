# Admin provisioning proof

This integration test runs the development admin image against a disposable
PostgreSQL database and a fixed test-only key. It checks login, tenant registration,
provisioning from the actual Razor form, retry and inactive status. It must not
be used as a production configuration.

From the repository root, after building the admin image:

```powershell
$env:PROOF_ADMIN_HASH = 'Proof-Password-123!' | docker run --rm -i ftm-platform-admin --hash-password
docker compose -p ftm-admin-proof -f tests/AdminProvision.Proof/compose.yaml up --force-recreate --abort-on-container-exit --exit-code-from proof
docker compose -p ftm-admin-proof -f tests/AdminProvision.Proof/compose.yaml down
```

The accompanying `PostgresModel.Proof` test verifies database isolation and
transaction rollback. Neither test enables the customer API's multi-tenant mode.
