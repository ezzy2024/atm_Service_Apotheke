# Stage 1: Runtime Base with Native QuestPDF Dependencies
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       fontconfig \
       libfreetype6 \
       fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

# Stage 2: SDK Build Environment
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ServiceApotheke.API.csproj", "./"]
RUN dotnet restore "./ServiceApotheke.API.csproj"
COPY . .
RUN dotnet build "ServiceApotheke.API.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "ServiceApotheke.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 4: Final Image Assembly
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ServiceApotheke.API.dll"]
