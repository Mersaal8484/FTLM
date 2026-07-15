# =============================================================================
# FUELTRACK FRONTEND - Production Hardened Multi-Stage Dockerfile
# =============================================================================
# Stage 1: Build React app
FROM node:20-alpine AS builder

WORKDIR /app

COPY package*.json ./
RUN --mount=type=cache,target=/root/.npm npm install

COPY . .

ARG REACT_APP_API_URL
ENV REACT_APP_API_URL=$REACT_APP_API_URL
ENV NODE_ENV=production
ENV GENERATE_SOURCEMAP=false
ENV INLINE_RUNTIME_CHUNK=false

RUN npm run build

# Stage 2: Serve with hardened nginx
FROM nginx:alpine AS runner

RUN rm -rf /etc/nginx/conf.d/default.conf /usr/share/nginx/html/* && \
    addgroup -S appgroup && adduser -S appuser -G appgroup

COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=builder /app/build /usr/share/nginx/html

RUN chown -R appuser:appgroup /usr/share/nginx/html && \
    chmod -R 500 /usr/share/nginx/html && \
    chown -R appuser:appgroup /var/cache/nginx && \
    chown -R appuser:appgroup /var/log/nginx && \
    chmod -R 700 /var/cache/nginx && \
    chmod -R 700 /var/log/nginx && \
    touch /var/run/nginx.pid && \
    chown -R appuser:appgroup /var/run/nginx.pid

USER appuser
EXPOSE 80

HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

CMD ["nginx", "-g", "daemon off;"]

LABEL org.opencontainers.image.title="FuelTrack Frontend" \
      org.opencontainers.image.description="React-based frontend for FuelTrack" \
      org.opencontainers.image.version="2.0.0" \
      org.opencontainers.image.vendor="FuelTrack"
