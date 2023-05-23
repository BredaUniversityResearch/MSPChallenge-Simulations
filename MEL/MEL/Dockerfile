FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MEL.core/MEL.core.csproj", "MEL.core/"]
RUN dotnet restore "MEL.core/MEL.core.csproj"
COPY . .
WORKDIR "/src/MEL.core"
RUN dotnet build "MEL.core.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MEL.core.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MEL.core.dll"]
