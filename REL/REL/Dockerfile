FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["REL.core/REL.core.csproj", "REL.core/"]
RUN dotnet restore "REL.core/REL.core.csproj"
COPY . .
WORKDIR "/src/REL.core"
RUN dotnet build "REL.core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "REL.core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "REL.core.dll"]
