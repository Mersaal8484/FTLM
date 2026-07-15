#!/bin/bash
# generate-certs.sh — Generate self-signed TLS certificates for local development
# Usage: bash nginx/generate-certs.sh

set -euo pipefail

CERT_DIR="$(dirname "$0")/certs"
mkdir -p "$CERT_DIR"

echo "Generating self-signed certificate for FuelTrack development..."
openssl req -x509 -nodes -days 365 \
    -newkey rsa:2048 \
    -keyout "$CERT_DIR/fueltrack.key" \
    -out "$CERT_DIR/fueltrack.crt" \
    -subj "/C=SA/ST=Riyadh/L=Riyadh/O=FuelTrack/CN=localhost" \
    -addext "subjectAltName=DNS:localhost,IP:127.0.0.1"

echo "Certificate generated at $CERT_DIR/"
echo "  - $CERT_DIR/fueltrack.crt"
echo "  - $CERT_DIR/fueltrack.key"
