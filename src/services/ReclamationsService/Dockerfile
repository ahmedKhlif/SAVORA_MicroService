FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/ReclamationsService/Savora.ReclamationsService.csproj", "services/ReclamationsService/"]
COPY ["shared/Savora.Shared/Savora.Shared.csproj", "shared/Savora.Shared/"]
RUN dotnet restore "services/ReclamationsService/Savora.ReclamationsService.csproj"
COPY . .
WORKDIR "/src/services/ReclamationsService"
RUN dotnet build "Savora.ReclamationsService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Savora.ReclamationsService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Savora.ReclamationsService.dll"]

