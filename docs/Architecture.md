# 📄 docs/Architecture.md

# Arquitetura do Transaction Processor

## 🏗️ Visão Geral
O Transaction Processor segue uma arquitetura **em camadas**, garantindo separação de responsabilidades e facilidade de manutenção.

Fluxo da requisição: Request → Controller → Service → DbContext/Repository → Banco de Dados → Response


---

## ⚙️ Tecnologias & Decisões Técnicas
- **.NET 9**: framework principal da API
- **Entity Framework Core**: ORM para persistência
- **PostgreSQL**: banco de dados relacional
- **Migrations**: controle de versão do banco
- **Injeção de Dependência (DI)**: serviços e DbContext injetados nos controllers
- **Swagger**: documentação e testes interativos da API

---

## 🔄 Estrutura de Domínio

### **AccountRecord**
Representa uma conta com saldo e limite de crédito:
- `AccountIdentifier`
- `HolderName`
- `Balance`
- `CreditLimit`
- `ReservedBalance`

### **TransactionRecord**
Representa cada operação financeira:
- `TransactionId`
- `ReferenceId` (chave idempotente)
- `Operation` (Credit, Debit, Transfer, etc.)
- `AccountIdentifier` / `DestinationAccountIdentifier`
- `Amount`
- `Currency`
- `Status` (Success, Failed, Pending)
- `ErrorCode` / `ErrorMessage`

---

## 🛢️ Banco de Dados
O banco é gerado via **migrations do EF Core**:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Tabelas principais:
* accounts
* transactions
