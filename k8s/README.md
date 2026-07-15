# FuelTrack Kubernetes Deployment

## Secrets

Secrets are **not** stored in git. Before deploying, create the secrets manually.

### Option 1: kubectl command

```bash
kubectl create secret generic fueltrack-secrets \
  --from-literal=SA_PASSWORD='<your-sql-server-password>' \
  --from-literal=JWT_SECRET='<openssl rand -base64 64>' \
  --from-literal=SMTP_PASSWORD='<your-smtp-password>' \
  --from-literal=ACCEPT_EULA='Y' \
  -n fueltrack
```

### Option 2: Apply from file

1. Copy `k8s/base/secrets.example.yaml` to `k8s/base/secrets.yaml`
2. Replace all placeholder values with real credentials
3. Apply: `kubectl apply -f k8s/base/secrets.yaml`

### Generating a JWT secret

```bash
openssl rand -base64 64
```

## Deploy

```bash
# Dev
kubectl apply -k k8s/overlays/dev/

# Prod
kubectl apply -k k8s/overlays/prod/
```
