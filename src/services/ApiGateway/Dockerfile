FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/ApiGateway/Savora.ApiGateway.csproj", "services/ApiGateway/"]
RUN dotnet restore "services/ApiGateway/Savora.ApiGateway.csproj"
COPY . .
WORKDIR "/src/services/ApiGateway"
RUN dotnet build "Savora.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Savora.ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Savora.ApiGateway.dll"]

