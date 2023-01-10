FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["CEL.core/CEL.core.csproj", "CEL.core/"]
RUN dotnet restore "CEL.core/CEL.core.csproj"
COPY . .
WORKDIR "/src/CEL.core"
RUN dotnet build "CEL.core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CEL.core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CEL.core.dll"]
