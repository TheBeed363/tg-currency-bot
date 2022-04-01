FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000

RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ../../src/TgCurrencyBot/TgCurrencyBot.csproj .
RUN dotnet restore "./TgCurrencyBot.csproj"
COPY ../../src/TgCurrencyBot .
WORKDIR "/src/."
RUN dotnet build "TgCurrencyBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TgCurrencyBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TgCurrencyBot.dll"]