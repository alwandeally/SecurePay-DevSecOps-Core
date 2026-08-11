FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/SecurePay.Transactions.Api/SecurePay.Transactions.Api.csproj", "src/SecurePay.Transactions.Api/"]
COPY ["src/SecurePay.Shared.Contracts/SecurePay.Shared.Contracts.csproj", "src/SecurePay.Shared.Contracts/"]

RUN dotnet restore "src/SecurePay.Transactions.Api/SecurePay.Transactions.Api.csproj"

COPY . .

RUN dotnet publish \
    "src/SecurePay.Transactions.Api/SecurePay.Transactions.Api.csproj" \
    --configuration $BUILD_CONFIGURATION \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "SecurePay.Transactions.Api.dll"]