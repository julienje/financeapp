FROM node:24.14.1-alpine@sha256:01743339035a5c3c11a373cd7c83aeab6ed1457b55da6a69e014a95ac4e4700b AS frontend-build
WORKDIR /app
COPY --link FinanceWeb .
RUN npm ci
RUN npm run build

# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0@sha256:5d0b4af94fe23bcdc6e5df135d460a2fee03aec6979e63c669d2bef399327184 AS backend-build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0-distroless@sha256:f51debfacc772b1a39f90c07cfefabc5b5026d66f17b91bd5a6b97c815d595c4
EXPOSE 8080
WORKDIR /app
COPY --link --from=backend-build /app .
USER $APP_UID
ENTRYPOINT ["./FinanceApp"]