FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/InterventionsService/Savora.InterventionsService.csproj", "services/InterventionsService/"]
COPY ["shared/Savora.Shared/Savora.Shared.csproj", "shared/Savora.Shared/"]
RUN dotnet restore "services/InterventionsService/Savora.InterventionsService.csproj"
COPY . .
WORKDIR "/src/services/InterventionsService"
RUN dotnet build "Savora.InterventionsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Savora.InterventionsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Savora.InterventionsService.dll"]

