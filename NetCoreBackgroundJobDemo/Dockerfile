FROM microsoft/aspnetcore:2.0-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0-nanoserver-1709 AS build
WORKDIR /src
COPY NetCoreBackgroundJobDemo/NetCoreBackgroundJobDemo.csproj NetCoreBackgroundJobDemo/
RUN dotnet restore NetCoreBackgroundJobDemo/NetCoreBackgroundJobDemo.csproj
COPY . .
WORKDIR /src/NetCoreBackgroundJobDemo
RUN dotnet build NetCoreBackgroundJobDemo.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish NetCoreBackgroundJobDemo.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "NetCoreBackgroundJobDemo.dll"]
