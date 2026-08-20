# Use the official .NET ASP.NET runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["UrlShortener.csproj", "./"]
RUN dotnet restore "UrlShortener.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "UrlShortener.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "UrlShortener.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage: copy the published output and set the entrypoint
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UrlShortener.dll"]
