# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY backend/ ./
RUN dotnet publish src/Api -c Release -o /out --nologo -v q

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS backend
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
RUN mkdir -p /app/database && chown app:app /app/database
COPY --from=backend-build /out ./
USER app
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]

FROM node:24-slim AS frontend
WORKDIR /app
RUN chown node:node /app
USER node
COPY --chown=node:node frontend/package*.json ./
RUN npm ci --loglevel=error
COPY --chown=node:node frontend/ ./
EXPOSE 4700
CMD ["npx", "ng", "serve", "--host", "0.0.0.0", "--port", "4700"]

FROM frontend AS frontend-build
RUN npm run build

FROM backend AS app
ENV StaticDir=/app/public
COPY --from=frontend-build /app/dist/frontend/browser /app/public
