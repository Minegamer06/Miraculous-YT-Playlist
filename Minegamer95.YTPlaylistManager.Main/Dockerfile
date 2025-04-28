FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Minegamer95.YTPlaylistManager.Main/Minegamer95.YTPlaylistManager.Main.csproj", "Minegamer95.YTPlaylistManager.Main/"]
RUN dotnet restore "Minegamer95.YTPlaylistManager.Main/Minegamer95.YTPlaylistManager.Main.csproj"
COPY . .
WORKDIR "/src/Minegamer95.YTPlaylistManager.Main"
RUN dotnet build "Minegamer95.YTPlaylistManager.Main.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Minegamer95.YTPlaylistManager.Main.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Definiere Auth-Umgebungsvariablen mit leerem Standardwert
ENV GOOGLE_REFRESH_TOKEN=""
ENV YT_CLIENT_ID=""
ENV YT_CLIENT_SECRET=""
ENTRYPOINT ["dotnet", "Minegamer95.YTPlaylistManager.Main.dll"]
