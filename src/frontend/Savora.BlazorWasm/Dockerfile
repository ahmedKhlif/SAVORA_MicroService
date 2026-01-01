FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["frontend/Savora.BlazorWasm/Savora.BlazorWasm.csproj", "frontend/Savora.BlazorWasm/"]
COPY ["shared/Savora.Shared/Savora.Shared.csproj", "shared/Savora.Shared/"]
RUN dotnet restore "frontend/Savora.BlazorWasm/Savora.BlazorWasm.csproj"
COPY . .
WORKDIR "/src/frontend/Savora.BlazorWasm"
RUN dotnet build "Savora.BlazorWasm.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Savora.BlazorWasm.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
COPY --from=publish /app/publish/wwwroot /usr/share/nginx/html
COPY frontend/Savora.BlazorWasm/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80

