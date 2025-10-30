# Na https://aka.ms/customizecontainer se dozvíte, jak si přizpůsobit kontejner ladění a jak Visual Studio používá tento dokument Dockerfile k sestavení vašich imagí pro rychlejší ladění.

# Tato fáze se používá při spuštění z VS v rychlém režimu (výchozí pro konfiguraci ladění).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Tato fáze slouží k sestavení projektu služby.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Revalidate.Frontend/Revalidate.Frontend/Revalidate.Frontend.csproj", "Revalidate.Frontend/Revalidate.Frontend/"]
COPY ["Revalidate.Frontend/Revalidate.Frontend.Client/Revalidate.Frontend.Client.csproj", "Revalidate.Frontend/Revalidate.Frontend.Client/"]
RUN dotnet restore "./Revalidate.Frontend/Revalidate.Frontend/Revalidate.Frontend.csproj"
COPY . .
WORKDIR "/src/Revalidate.Frontend/Revalidate.Frontend"
RUN dotnet build "./Revalidate.Frontend.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Tato fáze slouží k publikování projektu služby, který se má zkopírovat do konečné fáze.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Revalidate.Frontend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Tato fáze se používá v produkčním prostředí nebo při spuštění z VS v běžném režimu (výchozí, když se nepoužívá konfigurace ladění).
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Revalidate.Frontend.dll"]