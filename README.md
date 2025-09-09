# Transaction Processor

Um sistema de processamento de transações financeiras com suporte a múltiplos tipos de operação: **crédito, débito, reserva, captura, estorno e transferência**.

O projeto foi desenvolvido em **.NET 9, Entity Framework Core e PostgreSQL**, com suporte a **Docker** para execução isolada.

---

## 🚀 Funcionalidades
- Criação e gerenciamento de contas
- Operações de crédito e débito com validações de saldo
- Transações de reserva, captura e reversão
- Transferências entre contas
- Controle de saldo disponível e saldo reservado
- Persistência em banco de dados PostgreSQL
- Idempotência em transações via `ReferenceId`

---

## 🛠️ Tecnologias Utilizadas
- **C# .NET 9**
- **Entity Framework Core**
- **PostgreSQL**
- **Docker / Docker Compose**
- **Swagger (API Docs)**

---

### 1. Clonar o repositório
```bash
git clone https://github.com/costa-everton/TransactionProcessor.git
cd TransactionProcessor

2. Subir os containers (API + Banco de Dados)
docker-compose up --build

3. Acessar a API

Swagger UI: http://localhost:5000
API Base URL: http://localhost:5000/api


## 📚 Documentação
- [APIDocumentation](./docs/API.md) → exemplos de requisições e respostas  
- [Architecture](./docs/Architecture.md) → estrutura do projeto e decisões técnicas  

## 🔧 Rodando o Projeto com Docker
O projeto já está configurado para rodar com Docker e docker-compose, então você pode escolher rodar os comandos manualmente ou simplesmente executar o script fornecido.

1. Pré-requisitos
Antes de rodar, certifique-se de ter instalado:
```bash
Docker
Docker Compose

Verifique se estão funcionando com:
```bash
docker -v
docker compose version

2. Subindo os containers com Docker Compose
Na raiz do projeto, execute:

### 1. Construir as imagens
```bash
docker compose build

### 2. Subir os containers em background
```bash
docker compose up -d

### 3. Verificar se os containers estão rodando
```bash
docker compose ps

Se precisar ver os logs do serviço da API em tempo real:
```bash
docker compose logs -f api

3. Banco de Dados e Migrations
O container do banco (Postgres) já é iniciado automaticamente pelo Docker.
Caso seja necessário aplicar as migrations manualmente (se não estiver configurado para rodar automaticamente no entrypoint), use:
```bash
docker compose exec api dotnet ef database update

Isso cria todas as tabelas necessárias no banco.

4. Testando a API
A API ficará disponível em: http://localhost:5000

Exemplo de endpoints:
POST /accounts → Criar conta
GET /accounts/{id} → Buscar conta por ID
DELETE /accounts/{id} → Deletar conta
POST /transactions → Criar transação

5. Derrubar os containers
Quando terminar de usar, derrube os containers com:
```bash
docker compose down

Se quiser apagar volumes (dados do banco):
```bash
docker compose down -v


## 🚀 Script Automatizado (Windows)

Além dos comandos manuais, você pode rodar o projeto de forma automatizada usando o script PowerShell start-fresh.ps1, que já está na raiz do projeto.

1. Executar o script
No PowerShell, dentro da pasta do projeto, rode:
```powershell
.\start-fresh.ps1

Esse script já cuida de:
- Construir as imagens do Docker
- Subir os containers
- Aplicar migrations no banco de dados

2. Permissões (primeira execução)
Se for a primeira vez rodando scripts PowerShell no seu PC, pode ser necessário liberar a execução de scripts.
No PowerShell aberto como Administrador, rode:
```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser

Depois disso, o script funcionará normalmente.

3. O que acontece depois de rodar o script
- A API estará disponível em: http://localhost:5000
- O Postgres estará rodando no container definido no docker-compose.yml.

Você pode verificar se tudo está rodando com:
```bash
docker compose ps


## 🚀 Rodando o script no macOS
1. Instalar o PowerShell (se ainda não tiver)
No macOS, basta rodar:
```bash
brew install --cask powershell

Depois você poderá chamar pwsh no terminal.

2. Executar o script
Entre na pasta do projeto e rode:
```bash
pwsh ./start-fresh.ps1

3. Permissões (primeira vez)
No macOS também pode ser necessário liberar execução de scripts:
```bash
chmod +x ./start-fresh.ps1

Depois disso, o script roda normalmente.

4. O que acontece após rodar
- API disponível em http://localhost:5000
- Containers Docker rodando (docker compose ps)
- Banco com migrations aplicadas automaticamente