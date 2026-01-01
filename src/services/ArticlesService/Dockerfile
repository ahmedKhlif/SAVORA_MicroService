FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["services/ArticlesService/Savora.ArticlesService.csproj", "services/ArticlesService/"]
COPY ["shared/Savora.Shared/Savora.Shared.csproj", "shared/Savora.Shared/"]
RUN dotnet restore "services/ArticlesService/Savora.ArticlesService.csproj"
COPY . .
WORKDIR "/src/services/ArticlesService"
RUN dotnet build "Savora.ArticlesService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Savora.ArticlesService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Savora.ArticlesService.dll"]

