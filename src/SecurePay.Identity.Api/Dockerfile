FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/SecurePay.Identity.Api/SecurePay.Identity.Api.csproj", "src/SecurePay.Identity.Api/"]
COPY ["src/SecurePay.Shared.Contracts/SecurePay.Shared.Contracts.csproj", "src/SecurePay.Shared.Contracts/"]

RUN dotnet restore "src/SecurePay.Identity.Api/SecurePay.Identity.Api.csproj"

COPY . .

RUN dotnet publish \
    "src/SecurePay.Identity.Api/SecurePay.Identity.Api.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SecurePay.Identity.Api.dll"]