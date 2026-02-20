FROM node:24.13.1-alpine@sha256:4f696fbf39f383c1e486030ba6b289a5d9af541642fc78ab197e584a113b9c03 AS frontend-build
WORKDIR /app
COPY --link FinanceWeb .
RUN npm ci
RUN npm run build

# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0@sha256:774b85a4de4b63b5974d0a599e7166b0ff20f3848d3cc67333beb733405d027b AS backend-build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0-distroless@sha256:973ac891bc21916cb4f579ed3cd5737fac0a1452d30b11a25493df65eefd4786
EXPOSE 8080
WORKDIR /app
COPY --link --from=backend-build /app .
USER $APP_UID
ENTRYPOINT ["./FinanceApp"]