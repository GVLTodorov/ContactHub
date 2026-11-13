# syntax=docker/dockerfile:1
FROM node:24-slim AS frontend
WORKDIR /app
RUN chown node:node /app
USER node
COPY --chown=node:node frontend/package*.json ./
RUN npm ci --loglevel=error
COPY --chown=node:node frontend/ ./
EXPOSE 4700
CMD ["npx", "ng", "serve", "--host", "0.0.0.0", "--port", "4700"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY backend/ ./
RUN dotnet publish ContactHub.Api -c Release -o /out --nologo -v q

# Last stage: a default build produces the API image.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS backend
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/database && chown app:app /app/database
COPY --from=backend-build /out ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContactHub.Api.dll"]
