FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json NuGet.config Directory.Build.props Directory.Packages.props .editorconfig ./
COPY Platform.ServiceDefaults/ Platform.ServiceDefaults/
COPY Platform.ApiGateway/ Platform.ApiGateway/

RUN dotnet restore Platform.ApiGateway/Platform.ApiGateway.csproj
RUN dotnet publish Platform.ApiGateway/Platform.ApiGateway.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Platform.ApiGateway.dll"]
