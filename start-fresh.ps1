# start-fresh.ps1
Write-Host "=============================="
Write-Host "TransactionProcessor - Start Fresh"
Write-Host "=============================="

# Limpeza completa
Write-Host "Parando e removendo containers e volumes antigos..."
docker-compose down -v
docker system prune -af
docker volume prune -f

# Verificar EF Core CLI
if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host "dotnet-ef não encontrado. Instalando..."
    dotnet tool install --global dotnet-ef
}

# Diretório de migrations
$MIGRATION_DIR = "src/TransactionProcessor/Data/Migrations"

# Criar migration inicial se não existir
if (-not (Test-Path $MIGRATION_DIR) -or ((Get-ChildItem $MIGRATION_DIR) | Measure-Object).Count -eq 0) {
    Write-Host "Criando migration inicial..."
    Push-Location "src/TransactionProcessor"
    dotnet add package Microsoft.EntityFrameworkCore.Design
    dotnet ef migrations add InitialCreate -o Data/Migrations
    Pop-Location
} else {
    Write-Host "Migrations já existem."
}

# Subir PostgreSQL
Write-Host "Iniciando container do banco..."
docker-compose up -d db

# Esperar PostgreSQL
Write-Host "Aguardando PostgreSQL ficar pronto..."
$maxRetries = 20
$retry = 0
$ready = $false
while (-not $ready -and $retry -lt $maxRetries) {
    $logs = docker logs transaction-db 2>&1
    if ($logs -match "database system is ready to accept connections") {
        $ready = $true
    } else {
        Start-Sleep -Seconds 3
        $retry++
    }
}
if (-not $ready) {
    Write-Host "PostgreSQL não ficou pronto. Abortando."
    exit 1
}

# Subir API
Write-Host "Iniciando container da API..."
docker-compose up -d api

# Esperar API subir
Write-Host "Aguardando API iniciar..."
Start-Sleep -Seconds 10

# Aplicar migrations usando CLI local, sem depender do SDK dentro do container
Write-Host "Aplicando migrations..."
Push-Location "src/TransactionProcessor"
dotnet ef database update
Pop-Location

Write-Host "=============================="
Write-Host "TransactionProcessor pronto para uso!"
Write-Host "Swagger: http://localhost:5000"
Write-Host "API POST endpoint: http://localhost:5000/api/transactions"
Write-Host "=============================="
