# API de gestão de usuários

Prova de conceito em .NET 10 para cadastrar, consultar, listar, atualizar e excluir usuários. Os dados são persistidos em PostgreSQL. O [contrato da API](docs/especificacao-api.md) descreve os campos, a validação e as respostas HTTP.

## Estrutura da solução

A solução está em `backend/UserManagement.slnx`.

- `backend/src/Domain` contém a entidade `User` (`Entities/`), o enum `UserStatus` (`Enums/`), os objetos de valor `Cpf` e `Email` (`ValueObjects/`) e a `DomainException` (`Exceptions/`).
- `backend/src/Application` contém os behaviors do MediatR (`Behaviors/`), as interfaces de persistência e leitura (`Interfaces/`), as exceções da aplicação (`Exceptions/`) e os casos de uso em `Users/`. Cada command fica em `Users/Commands/<Caso>/` e cada query em `Users/Queries/<Caso>/`, com request, handler e validator em arquivos separados. Os DTOs ficam em `Users/DTOs/`.
- `backend/src/Api` contém os controllers, os contratos HTTP (`Requests/` e `Responses/`), o tratamento de erros, o conversor JSON do status e o OpenAPI.
- `backend/src/Infra` contém o DbContext, o repositório, o serviço de leitura e as migrations.
- `backend/tests` contém os testes de aplicação e os testes HTTP com PostgreSQL.

## Executar localmente

Instale o SDK .NET 10 e o Docker com Docker Compose. Os comandos abaixo usam Bash e partem da raiz do repositório.

Entre em `backend/`. Se o arquivo `.env` estiver ausente, copie `.env.example` para `.env` e preencha `POSTGRES_PASSWORD` com uma senha local. Para gerar uma senha hexadecimal, use `openssl rand -hex 24`. Se a porta padrão estiver ocupada, ajuste `POSTGRES_PORT` no `.env`.

```bash
cd backend
docker compose up -d --wait postgres

set -a
. ./.env
set +a
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=${POSTGRES_PORT:-5432};Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"

dotnet tool restore
dotnet restore UserManagement.slnx
dotnet ef database update --project src/Infra --startup-project src/Api
dotnet run --project src/Api --no-launch-profile --urls http://127.0.0.1:5080
```

O ambiente `Development` carrega `backend/src/Api/appsettings.Development.json` e serve o OpenAPI em `http://127.0.0.1:5080/openapi/v1.json`. Os appsettings versionados contêm apenas configurações compartilhadas. Mantenha a conexão em variável de ambiente ou em User Secrets. Se a interface rodar em outra origem, configure a origem exata em `Cors:AllowedOrigins`.

## Experimentar o contrato

Faça um cadastro com dados fictícios e use a URL do cabeçalho `Location` para consultar o usuário.

```bash
curl -i -X POST http://127.0.0.1:5080/api/users \
  -H 'Content-Type: application/json' \
  -d '{"fullName":"Sample Person","email":"person@example.test","cpf":"123.456.789-09"}'
```

Para listar de forma paginada, escolha o tamanho da página, o campo e a direção. A consulta desempata sempre por ID crescente.

```bash
curl 'http://127.0.0.1:5080/api/users?page=1&pageSize=10&sortBy=fullName&sortDirection=asc'
```

Na interface da avaliação, demonstre cadastro, listagem, consulta, atualização e exclusão. Para verificar a persistência, reinicie o serviço com `docker compose restart postgres`, espere a prontidão com `docker compose up -d --wait postgres` e consulte o mesmo ID.

## Compilar e testar

Execute a partir de `backend/`.

```bash
dotnet build UserManagement.slnx
dotnet test UserManagement.slnx
```

O xUnit executa os testes de domínio e aplicação. Os testes de integração usam Testcontainers para iniciar um PostgreSQL descartável e aplicar as migrations em um banco isolado por teste. Mantenha o Docker ativo ao executar a suíte completa.
