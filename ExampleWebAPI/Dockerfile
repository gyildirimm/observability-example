FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER root
RUN apt-get update && apt-get install -y curl unzip procps
WORKDIR /app
EXPOSE 8080
EXPOSE 5005

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Change to Debug configuration
ARG BUILD_CONFIGURATION=Debug
WORKDIR /src
COPY ["ExampleWebAPI/ExampleWebAPI.csproj", "ExampleWebAPI/"]
RUN dotnet restore "ExampleWebAPI/ExampleWebAPI.csproj"
COPY . .
WORKDIR "/src/ExampleWebAPI"
RUN dotnet build "ExampleWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
# Change to Debug configuration
ARG BUILD_CONFIGURATION=Debug
RUN dotnet publish "ExampleWebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# VSDebug kurulumu
RUN curl -sSL https://aka.ms/getvsdbgsh | /bin/sh /dev/stdin -v latest -l /vsdbg

ENTRYPOINT ["dotnet", "ExampleWebAPI.dll"]