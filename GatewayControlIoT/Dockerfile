FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["GatewayControlIoT.csproj", "./"]
RUN dotnet restore "GatewayControlIoT/GatewayControlIoT.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "GatewayControlIoT.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GatewayControlIoT.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GatewayControlIoT.dll"]
