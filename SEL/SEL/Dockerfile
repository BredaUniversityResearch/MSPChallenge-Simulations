FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["SEL.core/SEL.core.csproj", "SEL.core/"]
RUN dotnet restore "SEL.core/SEL.core.csproj"
COPY . .
WORKDIR "/src/SEL.core"
RUN dotnet build "SEL.core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SEL.core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SEL.core.dll"]
