FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder

WORKDIR /src

COPY ["./src/Device.FingerPrint.TransactionAPI/Device.FingerPrint.TransactionAPI.csproj", "Device.FingerPrint.TransactionAPI/"]

WORKDIR "/src/Device.FingerPrint.TransactionAPI"

RUN dotnet nuget add source https://nuget.nexdev.net/api/v2

RUN dotnet restore -v n

COPY ./src/Device.FingerPrint.TransactionAPI .

RUN dotnet publish -r linux-x64 --self-contained false -c Release -o /app

FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS base

WORKDIR /app

COPY --from=builder /app .

ENV TZ="America/La_Paz"

ENTRYPOINT ["dotnet", "Device.FingerPrint.TransactionAPI.dll"]