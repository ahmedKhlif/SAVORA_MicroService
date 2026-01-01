FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/AuthService/Savora.AuthService.csproj", "services/AuthService/"]
COPY ["shared/Savora.Shared/Savora.Shared.csproj", "shared/Savora.Shared/"]
RUN dotnet restore "services/AuthService/Savora.AuthService.csproj"
COPY . .
WORKDIR "/src/services/AuthService"
RUN dotnet build "Savora.AuthService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Savora.AuthService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Savora.AuthService.dll"]

