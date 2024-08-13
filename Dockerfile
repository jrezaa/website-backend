# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-focal AS build
WORKDIR /source
COPY . .
RUN dotnet restore "./backend.csproj" --disable-parallel
RUN dotnet publish "./backend.csproj" -c release -o /app --no-restore


# Serve Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-focal
WORKDIR /app
COPY --from=build /app ./

EXPOSE 6969

ENTRYPOINT ["dotnet", "backend.dll"]