# Etapa 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copiar csproj e restaurar dependências
COPY src/TransactionProcessor/*.csproj ./src/TransactionProcessor/
RUN dotnet restore ./src/TransactionProcessor/TransactionProcessor.csproj

# Copiar tudo e compilar
COPY . .
RUN dotnet publish ./src/TransactionProcessor/TransactionProcessor.csproj -c Release -o /out

# Etapa 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /out ./
ENTRYPOINT ["dotnet", "TransactionProcessor.dll"]
