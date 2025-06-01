FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/AiAssistant.Api/AiAssistant.Api.csproj", "AiAssistant.Api/"]
COPY ["src/AiAssistant.Core/AiAssistant.Core.csproj", "AiAssistant.Core/"]
RUN dotnet restore "AiAssistant.Api/AiAssistant.Api.csproj"
COPY src/ .
WORKDIR "/src/AiAssistant.Api"
RUN dotnet build "AiAssistant.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AiAssistant.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AiAssistant.Api.dll"] 