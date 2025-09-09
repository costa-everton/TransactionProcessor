📄 Documentção da API

Base URL: http://localhost:5000/api

Esta documentação descreve os endpoints de contas e transações, exemplos de request/response e códigos de erro.
Você também pode importar diretamente a collection completa do Postman (JSON) para testar todas as operações com valores prontos.

📑 Regras de Negócio da API de Transações

# 1. Idempotência e ReferenceId

Cada transação deve ter um ReferenceId único.
Caso seja enviada uma nova transação com o mesmo ReferenceId, os comportamentos são:

Se todos os parâmetros forem iguais (conta, operação, valor, moeda, etc.):
→ A API retorna a mesma transação existente (idempotência garantida).

Se algum parâmetro for diferente:
→ A API rejeita a transação com erro:
```json
{
  "errorCode": "error.duplicate_reference",
  "errorMessage": "ReferenceId already used for a different transaction"
}

🔑 Isso garante que ReferenceId funciona como chave idempotente, evitando operações duplicadas ou inconsistentes.

# 2. Operações suportadas
* Credit → Credita valor na conta.
* Debit → Debita valor, respeitando saldo + limite de crédito.
* Reserve → Reserva valor do saldo disponível.
* Capture → Captura valor previamente reservado.
* Reversal → Estorna uma transação de referência (OriginalReferenceId é obrigatório).
* Transfer → Transfere valor de uma conta para outra.

## Cada operação tem validações próprias:
* Se a conta de origem ou destino não existir → error.invalid_account
* Se não houver saldo suficiente → error.insufficient_funds
* Se não houver reserva suficiente para capturar → error.insufficient_reserved
* Se faltar DestinationAccountId em transferência → error.destination_account_required
* Se faltar OriginalReferenceId em reversão → error.original_reference_required
* Se a transação original não existir ou não tiver sucesso → error.original_transaction_not_found
* Se a operação não for reconhecida → error.unknown_operation

# 3. Erros padronizados
A API retorna erros padronizados no formato:
```json
{
  "errorCode": "error.invalid_account",
  "errorMessage": "The specified account does not exist"
}

# Lista de errorCode:

| Código                                     | Significado                                                    |
| ------------------------------------------ | -------------------------------------------------------------- |
| `error.invalid_account`                    | Conta não encontrada                                           |
| `error.insufficient_funds`                 | Saldo insuficiente                                             |
| `error.insufficient_available_for_reserve` | Saldo disponível insuficiente para reserva                     |
| `error.insufficient_reserved`              | Reserva insuficiente para captura                              |
| `error.destination_account_required`       | Conta de destino obrigatória em transferência                  |
| `error.duplicate_reference`                | `ReferenceId` já utilizado em transação diferente              |
| `error.unknown_operation`                  | Operação não reconhecida                                       |
| `error.original_reference_required`        | Referência original obrigatória em reversão                    |
| `error.original_transaction_not_found`     | Transação original não encontrada ou não concluída com sucesso |
| `error.exception`                          | Erro inesperado no servidor                                    |


# 4. Confiabilidade e Concorrência
* Todas as operações rodam em transações de banco (Serializable).
* Caso haja concorrência (duas operações simultâneas na mesma conta), a API reexecuta até 5 tentativas (MAX_RETRIES = 5) antes de falhar.
* Isso garante consistência de saldo e evita race conditions.

# 5. Respostas de sucesso
Transações bem-sucedidas retornam:
```json
{
  "id": 42,
  "transactionId": "uuid-gerado",
  "referenceId": "abc-123",
  "operation": "Credit",
  "accountIdentifier": "ACC001",
  "amount": 100,
  "currency": "BRL",
  "status": "Success",
  "timestampUtc": "2025-09-08T22:59:00.49963Z",
  "balanceAfter": 450,
  "reservedAfter": 0,
  "metadata": {}
}

# 📌 Resumo:
* ReferenceId = chave idempotente.
* Transações duplicadas retornam o mesmo resultado.
* Se o ReferenceId for usado com dados diferentes → erro de duplicidade.
* Todas as operações seguem regras de validação de saldo, contas e reversão.
* Erros padronizados e consistentes para integração.
* Confiabilidade garantida com transações de banco e controle de concorrência.


# 🔗 Importar Collection Postman

Você pode copiar todo o JSON abaixo e importar no Postman:

<details> <summary>Clique para expandir o JSON da collection completa</summary>
```json
    {
      "info": {
        "name": "TransactionProcessor API - Complete",
        "_postman_id": "c1234567-89ab-4def-9012-abcdef345678",
        "description": "Collection completa com todas as operações e exemplos de erro",
        "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
      },
      "item": [
        {
          "name": "Accounts",
          "item": [
            {
              "name": "Create Account",
              "request": {
                "method": "POST",
                "header": [{"key": "Content-Type","value": "application/json"}],
                "url":{"raw":"http://localhost:5000/api/accounts","protocol":"http","host":["localhost"],"port":"5000","path":["api","accounts"]},
                "body":{"mode":"raw","raw":"{\n  \"accountId\": \"ACC001\",\n  \"accountHolderName\": \"Usuario Teste\",\n  \"initialBalance\": 1000,\n     \"creditLimit\": 5000\n}"}
              }
            },
            {
              "name": "Get Account by ID",
              "request":{"method":"GET","url":{"raw":"http://localhost:5000/api/accounts/ACC001","protocol":"http","host":["localhost"],"port":"5000","path":   ["api","accounts","ACC001"]}}
            },
            {
              "name": "Get All Accounts",
              "request":{"method":"GET","url":{"raw":"http://localhost:5000/api/accounts","protocol":"http","host":["localhost"],"port":"5000","path":["api",   "accounts"]}}
            },
            {
              "name": "Delete Account",
              "request":{"method":"DELETE","url":{"raw":"http://localhost:5000/api/accounts/ACC001","protocol":"http","host":["localhost"],"port":"5000",   "path":["api","accounts","ACC001"]}}
            }
          ]
        },
        {
          "name": "Transactions",
          "item": [
            {
              "name": "Credit Account",
              "request": {
                "method": "POST",
                "header":[{"key":"Content-Type","value":"application/json"}],
                "url":{"raw":"http://localhost:5000/api/transactions","protocol":"http","host":["localhost"],"port":"5000","path":["api","transactions"]},
                "body":{"mode":"raw","raw":"{\n  \"Operation\": \"credit\",\n  \"AccountId\": \"ACC001\",\n  \"Amount\": 500,\n  \"Currency\": \"BRL\",\n   \"ReferenceId\": \"c-001\",\n  \"Metadata\": {\"description\": \"Depósito em conta\"}\n}"}
              }
            },
            {
              "name": "Debit Account",
              "request": {
                "method": "POST",
                "header":[{"key":"Content-Type","value":"application/json"}],
                "url":{"raw":"http://localhost:5000/api/transactions","protocol":"http","host":["localhost"],"port":"5000","path":["api","transactions"]},
                "body":{"mode":"raw","raw":"{\n  \"Operation\": \"debit\",\n  \"AccountId\": \"ACC001\",\n  \"Amount\": 200,\n  \"Currency\": \"BRL\",\n    \"ReferenceId\": \"d-001\",\n  \"Metadata\": {\"description\": \"Pagamento\"}\n}"}
              }
            },
            {
              "name": "Reserve Amount",
              "request": {
                "method": "POST",
                "header":[{"key":"Content-Type","value":"application/json"}],
                "url":{"raw":"http://localhost:5000/api/transactions","protocol":"http","host":["localhost"],"port":"5000","path":["api","transactions"]},
                "body":{"mode":"raw","raw":"{\n  \"Operation\": \"reserve\",\n  \"AccountId\": \"ACC001\",\n  \"Amount\": 100,\n  \"Currency\": \"BRL\",\n      \"ReferenceId\": \"r-001\",\n  \"Metadata\": {\"description\": \"Reserva para compra\"}\n}"}
              }
            },
            {
              "name": "Capture Reserved Amount",
              "request": {
                "method": "POST",
                "header":[{"key":"Content-Type","value":"application/json"}],
                "url":{"raw":"http://localhost:5000/api/transactions","protocol":"http","host":["localhost"],"port":"5000","path":["api","transactions"]},
                "body":{"mode":"raw","raw":"{\n  \"Operation\": \"capture\",\n  \"AccountId\": \"ACC001\",\n  \"Amount\": 100,\n  \"Currency\": \"BRL\",\n      \"ReferenceId\": \"cp-001\",\n  \"Metadata\": {\"description\": \"Confirmação da reserva\"}\n}"}
              }
            },
            {
              "name": "Reversal Transaction",
              "request": {
                "method": "POST",
                "header":[{"key":"Content-Type","value":"application/json"}],
                "url":{"raw":"http://localhost:5000/api/transactions","protocol":"http","host":["localhost"],"port":"5000","path":["api","transactions"]},
                "body":{"mode":"raw","raw":"{\n  \"Operation\": \"reversal\",\n  \"AccountId\": \"ACC001\",\n  \"Amount\": 150,\n  \"Currency\": \"BRL\",\n     \"ReferenceId\": \"rv-001\",\n  \"OriginalReferenceId\": \"c-001\",\n  \"Metadata\": {\"description\": \"Estorno\"}\n}"}
              }
            },
            {
              "name": "Transfer Between Accounts",
              "request": {
                "method": "POST",
                "header":[{"key":"Content-Type","value":"application/json"}],
                "url":{"raw":"http://localhost:5000/api/transactions","protocol":"http","host":["localhost"],"port":"5000","path":["api","transactions"]},
                "body":{"mode":"raw","raw":"{\n  \"Operation\": \"transfer\",\n  \"AccountId\": \"ACC002\",\n  \"DestinationAccountId\": \"ACC001\",\n      \"Amount\": 350,\n  \"Currency\": \"BRL\",\n  \"ReferenceId\": \"t-001\",\n  \"Metadata\": {\"description\": \"Transferência\"}\n}"}
              }
            }
          ]
        }
      ]
    }
</details>

🔑 Endpoints de Conta (/accounts)

| Método | Endpoint         | Descrição              |
| ------ | ---------------- | ---------------------- |
| POST   | `/accounts`      | Criar nova conta       |
| GET    | `/accounts/{id}` | Consultar conta por ID |
| GET    | `/accounts`      | Listar todas as contas |
| DELETE | `/accounts/{id}` | Deletar conta          |

Exemplo de request POST
```json
{
  "accountId": "ACC001",
  "accountHolderName": "Usuario Teste",
  "initialBalance": 1000,
  "creditLimit": 5000
}

Exemplo de resposta
```json
{
  "id": 1,
  "accountIdentifier": "ACC001",
  "holderName": "Usuario Teste",
  "balance": 1000,
  "creditLimit": 5000,
  "reservedBalance": 0,
  "status": "success"
}

💳 Endpoints de Transações (/transactions)

| Operação | Endpoint        | Descrição                                                            |
| -------- | --------------- | -------------------------------------------------------------------- |
| POST     | `/transactions` | Executa crédito, débito, transferência, reserva, captura ou reversão |

Exemplo de request (crédito)
```json
{
  "Operation": "credit",
  "AccountId": "ACC001",
  "Amount": 500,
  "Currency": "BRL",
  "ReferenceId": "c-001",
  "Metadata": { "description": "Depósito em conta" }
}

Exemplo de request (transferência)
```json
{
  "Operation": "transfer",
  "AccountId": "ACC002",
  "DestinationAccountId": "ACC001",
  "Amount": 350,
  "Currency": "BRL",
  "ReferenceId": "t-001",
  "Metadata": { "description": "Transferência" }
}

Exemplo de resposta
```json
{
  "id": 1,
  "transactionId": "fa6a301a-5118-4dee-adc5-02d947318a64",
  "referenceId": "c-001",
  "operation": "credit",
  "accountIdentifier": "ACC001",
  "amount": 500,
  "currency": "BRL",
  "status": "success",
  "timestampUtc": "2025-09-08T19:12:16Z",
  "balanceAfter": 1500,
  "reservedAfter": 0
}


⚠️ Códigos de Erro

| Código | Descrição                                  |
| ------ | ------------------------------------------ |
| 400    | Parâmetros inválidos ou saldo insuficiente |
| 404    | Conta ou recurso não encontrado            |
| 409    | Transação duplicada (ReferenceId já usada) |
| 500    | Erro inesperado no servidor                |

Exemplo de erro:
```json
{
  "errorCode": "error.account_already_exists",
  "errorMessage": "Account already exists"
}
