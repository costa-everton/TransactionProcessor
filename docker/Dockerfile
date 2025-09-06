# Use .NET 9 SDK para build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copia csproj e restaura dependências
COPY src/TransactionProcessor/*.csproj ./TransactionProcessor/
RUN dotnet restore TransactionProcessor/*.csproj

# Copia todo o código e builda
COPY src/TransactionProcessor/. ./TransactionProcessor/
WORKDIR /app/TransactionProcessor
RUN dotnet build -c Release -o /app/build

# Publica a aplicação
RUN dotnet publish -c Release -o /app/publish

# Cria container final com runtime apenas
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TransactionProcessor.dll"]
