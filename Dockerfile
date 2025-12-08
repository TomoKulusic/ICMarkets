FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["ICMarkets/ICMarkets.csproj", "ICMarkets/"]
RUN dotnet restore "ICMarkets/ICMarkets.csproj"
COPY . .
WORKDIR "/src/ICMarkets"
RUN dotnet build "ICMarkets.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ICMarkets.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ICMarkets.dll"]
