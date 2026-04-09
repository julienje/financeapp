FROM node:24.14.1-alpine@sha256:01743339035a5c3c11a373cd7c83aeab6ed1457b55da6a69e014a95ac4e4700b AS frontend-build
WORKDIR /app
COPY --link FinanceWeb .
RUN npm ci
RUN npm run build

# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0-azurelinux3.0@sha256:52066c5f2566b1e251f0a7ff6c9380d655054d560d6f88a724d23b72448b9a3a AS backend-build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0-distroless@sha256:4926af2df5bb2c23934ad25d7dc7c08910e8affd38327f99e8ee2d770d8ce145
EXPOSE 8080
WORKDIR /app
COPY --link --from=backend-build /app .
USER $APP_UID
ENTRYPOINT ["./FinanceApp"]