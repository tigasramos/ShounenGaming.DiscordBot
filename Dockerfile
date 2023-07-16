FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ShounenGaming.DiscordBot/ShounenGaming.DiscordBot.csproj", "ShounenGaming.DiscordBot/"]
RUN dotnet restore "ShounenGaming.DiscordBot/ShounenGaming.DiscordBot.csproj"
COPY . .
WORKDIR "/src/ShounenGaming.DiscordBot"
RUN dotnet build "ShounenGaming.DiscordBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShounenGaming.DiscordBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN mkdir logs

ENTRYPOINT ["dotnet", "ShounenGaming.DiscordBot.dll"]