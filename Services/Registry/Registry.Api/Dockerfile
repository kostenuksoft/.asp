FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json NuGet.config Directory.Build.props Directory.Packages.props .editorconfig ./
COPY Platform.ServiceDefaults/Platform.ServiceDefaults.csproj Platform.ServiceDefaults/
COPY Platform.EventBus/Platform.EventBus.csproj Platform.EventBus/
COPY Services/Registry/Registry.Domain/Registry.Domain.csproj Services/Registry/Registry.Domain/
COPY Services/Registry/Registry.Dal/Registry.Dal.csproj Services/Registry/Registry.Dal/
COPY Services/Registry/Registry.Bll/Registry.Bll.csproj Services/Registry/Registry.Bll/
COPY Services/Registry/Registry.Api/Registry.Api.csproj Services/Registry/Registry.Api/
RUN dotnet restore Services/Registry/Registry.Api/Registry.Api.csproj

COPY Platform.ServiceDefaults/ Platform.ServiceDefaults/
COPY Platform.EventBus/ Platform.EventBus/
COPY Services/Registry/ Services/Registry/
RUN dotnet publish Services/Registry/Registry.Api/Registry.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Registry.Api.dll"]
