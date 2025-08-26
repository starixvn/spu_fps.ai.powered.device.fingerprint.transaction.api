FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder

WORKDIR /src

# Copy project file first for better layer caching
COPY ["./src/FPS.AI.Powered.Device.Fingerprint.Transaction.API/Device.FingerPrint.TransactionAPI.csproj", "FPS.AI.Powered.Device.Fingerprint.Transaction.API/"]

WORKDIR "/src/FPS.AI.Powered.Device.Fingerprint.Transaction.API"

RUN dotnet nuget add source http://nuget.nexdev.net/api/v2

RUN dotnet restore -v n

# Copy the rest of the source code
COPY . .

RUN dotnet publish -r linux-x64 --self-contained false -c Release -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS base

WORKDIR /app

COPY --from=builder /app .

ENV TZ="America/La_Paz"

ENTRYPOINT ["dotnet", "Device.FingerPrint.TransactionAPI.dll"]