FROM node:24.15.0-alpine@sha256:d1b3b4da11eefd5941e7f0b9cf17783fc99d9c6fc34884a665f40a06dbdfc94f AS frontend-build
WORKDIR /app
COPY --link FinanceWeb .
RUN npm ci
RUN npm run build

# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0@sha256:28e3d18f3e172d554d7002ee1af97c0a24b31b26ae3b5dd8d2e40a9a6c5760b1 AS backend-build
ARG TARGETARCH
WORKDIR /source

# Copy project file and restore as distinct layers
COPY --link FinanceApp/*.fsproj .
RUN dotnet restore -a $TARGETARCH

# Copy source code and publish app
COPY --link FinanceApp .
COPY --link --from=frontend-build /app/dist ./wwwroot
RUN dotnet publish -a $TARGETARCH --no-restore -o /app

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0-distroless@sha256:ad10865ffdf5706ef7e8cdb3ccb25fe6f766e643b17d7d2b8ee0e1cc00c99efb
EXPOSE 8080
WORKDIR /app
COPY --link --from=backend-build /app .
USER $APP_UID
ENTRYPOINT ["./FinanceApp"]