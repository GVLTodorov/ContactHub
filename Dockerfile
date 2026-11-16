# syntax=docker/dockerfile:1
# One image for the whole app: the API plus the Angular UI, served by nginx.
#   8080 - the API (Swagger at /swagger, health at /api/health)
#   4700 - the Angular UI; it also forwards /api and /swagger to the API, so it is the single
#          public port for hosts that route one port (render.yaml)
# The ports match local development, so the Swagger "Client" button and the URLs in README.md
# work unchanged. Tests run in CI (.github/workflows/ci.yml) before this image is built.

# ---- Frontend ----
FROM node:24-slim AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --loglevel=error
COPY frontend/ ./
RUN npm run build

# ---- Backend ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/ ./
RUN dotnet publish ContactHub.Api -c Release -o /out --nologo -v q

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS app
RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
RUN mkdir -p /app/database && chown app:app /app/database
COPY --from=backend /out ./
COPY --from=frontend /src/dist/frontend/browser /srv/frontend
COPY docker/nginx.conf /etc/nginx/nginx.conf
COPY docker/entrypoint.sh /entrypoint.sh
# Strip CRLF in case the script was checked out on Windows.
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080 4700

USER app
ENTRYPOINT ["/entrypoint.sh"]
