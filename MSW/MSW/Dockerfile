FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MSW/MSW.csproj", "MSW/"]
RUN dotnet restore "MSW/MSW.csproj"
COPY . .
WORKDIR "/src/MSW"
RUN dotnet build "MSW.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MSW.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MSW.dll"]
