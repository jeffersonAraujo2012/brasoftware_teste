# Especificação da API de gestão de usuários

## Objetivo e escopo

A API da prova de conceito permite cadastrar, listar, consultar, atualizar e excluir usuários, com persistência em PostgreSQL. Cada usuário tem nome completo, e-mail, CPF, data de cadastro e status. A interface de avaliação deve consumir este contrato para executar as cinco operações. O desenho da interface fica em uma especificação própria. [Avaliação Técnica, seção 2, páginas 2 e 3](Avalia%C3%A7%C3%A3o%20T%C3%A9cnica%20-%20Apps%20%26%20IA%20%28Pleno%29.pdf#page=2).

## Estrutura da solução

A solução ficará em `backend/`, com C# e .NET 10 (`net10.0`) em todos os projetos. A API será hospedada pelo ASP.NET Core 10. A persistência usará EF Core 10 com `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x. [Npgsql para EF Core 10](https://www.npgsql.org/efcore/release-notes/10.0.html).

Use inglês nos nomes de projetos, tipos, métodos, propriedades, comentários de código, rotas, contratos JSON, tabelas e migrations. O identificador `cpf` mantém a sigla oficial do documento brasileiro. A documentação do projeto usa português brasileiro.

```text
.gitignore
docs/
└── especificacao-api.md
backend/
├── UserManagement.slnx
├── compose.yaml
├── .env.example
├── .config/
│   └── dotnet-tools.json
├── src/
│   ├── Domain/
│   │   ├── Domain.csproj
│   │   └── Users/                    # User, UserStatus and CPF rules
│   ├── Application/
│   │   ├── Application.csproj
│   │   ├── Abstractions/             # IUserRepository
│   │   ├── Behaviors/                # ValidationBehavior
│   │   └── Users/                    # Commands, queries, handlers and validators
│   ├── Api/
│   │   ├── Api.csproj
│   │   ├── Controllers/
│   │   ├── Contracts/
│   │   ├── Errors/
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   └── Infra/
│       ├── Infra.csproj
│       └── Persistence/
│           ├── Configurations/
│           ├── Migrations/
│           ├── Repositories/
│           └── UserManagementDbContext.cs
└── tests/
    ├── Application.Tests/
    │   └── Application.Tests.csproj
    └── Api.IntegrationTests/
        └── Api.IntegrationTests.csproj
```

| Componente | Responsabilidade | Referências diretas entre projetos de produção |
| --- | --- | --- |
| `Domain` | Entidade `User`, status e invariantes de domínio. | Nenhuma |
| `Application` | Casos de uso, contratos de persistência, validação e tratamento de resultados dos casos de uso. | `Domain` |
| `Api` | Camada externa. Controllers, contratos HTTP, OpenAPI, mapeamento de erros e composição da injeção de dependências. | `Application` e `Infra` |
| `Infra` | Camada externa. Implementação do repositório, configuração do PostgreSQL e do EF Core, mapeamentos e migrations. | `Application` e `Domain` |

`Api` e `Infra` estão na mesma camada externa. A referência da `Api` para a `Infra` serve à composição das dependências em `Program.cs`. Os controllers enviam requests a `ISender.Send`, repassando o token de cancelamento. Os handlers acessam a persistência pelo contrato da `Application`.

## Casos de uso e validação

Use MediatR 12.5.0 para despachar commands de escrita e queries de leitura. A `Application` registra os handlers do seu assembly e o behavior genérico `ValidationBehavior<TRequest, TResponse>` com `AddOpenBehavior`. A `Api` chama esse registro na inicialização. [MediatR 12.5.0](https://github.com/LuckyPennySoftware/MediatR/blob/v12.5.0/README.md#registering-with-iservicecollection).

Registre os validadores do assembly da `Application` com `AddValidatorsFromAssembly`. O behavior implementa `IPipelineBehavior<TRequest, TResponse>`, chama `ValidateAsync` nos `IValidator<TRequest>` registrados e agrega os erros antes de chamar o handler. Se houver erros, encerra a execução e a `Api` responde com `400` e mensagens por campo. Aplique a validação aos comandos de criação e atualização e às operações que recebem ID. Um UUID vazio é inválido. [Injeção de dependências do FluentValidation](https://docs.fluentvalidation.net/en/latest/di.html) e [validação assíncrona](https://docs.fluentvalidation.net/en/latest/async.html).

Valide também os parâmetros de `ListUsersQuery` com FluentValidation. A `Infra` mapeia cada campo de ordenação aceito para a coluna correspondente e aplica a ordenação e a paginação no PostgreSQL.

## Modelo de usuário

| Campo JSON | Persistência | Regra |
| --- | --- | --- |
| `id` | `uuid`, chave primária | Gerado pelo servidor na criação. |
| `fullName` | `varchar(150)` | Obrigatório. Remova espaços nas extremidades e rejeite valor vazio ou com mais de 150 caracteres. |
| `email` | `varchar(254)`, único | Obrigatório, com formato válido e até 254 caracteres após remover espaços nas extremidades. Normalize para minúsculas antes de persistir. |
| `cpf` | `varchar(11)`, único | Obrigatório. Aceite 11 dígitos ASCII ou a máscara `000.000.000-00`. Valide os dígitos verificadores, rejeite sequências repetidas e armazene como texto de 11 dígitos, preservando zeros à esquerda. |
| `createdAt` | `timestamptz` | Gerada pelo servidor na criação, em UTC. A resposta usa ISO 8601 e o valor é preservado em atualizações. |
| `status` | `varchar(8)` | Valores `active` ou `inactive`. O cadastro assume `active` quando o campo for omitido. A atualização exige o campo. |

A migration inicial cria a tabela `users`, com as colunas `id`, `full_name`, `email`, `cpf`, `created_at` e `status`, restrição para os dois valores de status e índices únicos `ux_users_email` e `ux_users_cpf`. A `Infra` traduz violações desses índices em conflitos da aplicação, inclusive em gravações simultâneas. A exclusão remove o registro. Alterar o status para `inactive` mantém o usuário disponível para consulta.

## Contrato HTTP

| Método e rota | Caso de uso | Resposta de sucesso |
| --- | --- | --- |
| `POST /api/users` | `CreateUserCommand` | `201 Created`, usuário criado e cabeçalho `Location` com a rota de consulta. |
| `GET /api/users` | `ListUsersQuery` | `200 OK`, objeto com os usuários da página e os metadados de paginação. |
| `GET /api/users/{id}` | `GetUserQuery` | `200 OK`, usuário com o ID informado. |
| `PUT /api/users/{id}` | `UpdateUserCommand` | `200 OK`, usuário atualizado. Substitui todos os campos editáveis. |
| `DELETE /api/users/{id}` | `DeleteUserCommand` | `204 No Content`, sem corpo. |

Os corpos de `POST` e `PUT` são JSON. `PUT` exige `fullName`, `email`, `cpf` e `status`. O ID da atualização vem da rota. Os DTOs de entrada expõem apenas campos editáveis.

Exemplo de cadastro com dados fictícios:

```json
{
  "fullName": "Sample Person",
  "email": "person@example.test",
  "cpf": "123.456.789-09"
}
```

Exemplo do corpo retornado na criação e na consulta individual:

```json
{
  "id": "c42c5805-cab8-4723-84e4-ee1ab50b36c9",
  "fullName": "Sample Person",
  "email": "person@example.test",
  "cpf": "12345678909",
  "createdAt": "2026-11-25T15:00:00Z",
  "status": "active"
}
```

### Paginação e ordenação da listagem

Todos os parâmetros de `GET /api/users` são opcionais.

| Parâmetro | Valores aceitos | Padrão |
| --- | --- | --- |
| `page` | Inteiro a partir de `1`. | `1` |
| `pageSize` | Inteiro entre `1` e `100`. | `20` |
| `sortBy` | `fullName`, `email`, `cpf`, `createdAt` ou `status`. | `createdAt` |
| `sortDirection` | `asc` ou `desc`. | `desc` |

Ordene primeiro pelo campo e pela direção solicitados e depois sempre por `id ASC`, inclusive quando a direção principal for `desc`. Só então selecione a página. Para CPF, use o valor normalizado de 11 dígitos. Para status, use a ordem alfabética dos valores `active` e `inactive`. A ordenação determina a posição de cada registro enquanto os dados permanecem os mesmos.

`totalItems` informa o total de usuários. `totalPages` arredonda para cima a divisão de `totalItems` por `pageSize` e vale `0` quando a coleção está vazia. Uma página além da última retorna `200 OK` com `items: []` e os metadados, incluindo o número de página solicitado.

Exemplo de resposta para `GET /api/users?page=2&pageSize=1&sortBy=fullName&sortDirection=asc`.

```json
{
  "items": [
    {
      "id": "c42c5805-cab8-4723-84e4-ee1ab50b36c9",
      "fullName": "Sample Person",
      "email": "person@example.test",
      "cpf": "12345678909",
      "createdAt": "2026-11-25T15:00:00Z",
      "status": "active"
    }
  ],
  "page": 2,
  "pageSize": 1,
  "totalItems": 2,
  "totalPages": 2
}
```

### Erros

As falhas retornam `application/problem+json` com `status`, `title` e `traceId`. Use `ValidationProblemDetails` com `errors` por campo nas falhas de entrada. Nas chaves de `errors`, use os nomes do contrato JSON ou dos parâmetros da query string, `id` para a rota e `body` quando não for possível identificar um campo do corpo.

| Código | Quando retornar |
| --- | --- |
| `400 Bad Request` | JSON malformado, ID inválido ou campos e parâmetros reprovados pela validação. |
| `404 Not Found` | ID válido sem registro em consulta individual, atualização ou exclusão. |
| `409 Conflict` | E-mail ou CPF já cadastrado em outro registro. A extensão `field` contém `email` ou `cpf`. |
| `500 Internal Server Error` | Falha inesperada. Retorne mensagem genérica e registre a ocorrência com o identificador de rastreamento. |

Disponibilize OpenAPI no ambiente `Development`. Configure as origens permitidas em `Cors:AllowedOrigins` quando a interface rodar em outra origem. Use dados fictícios na demonstração e evite registrar corpos, CPF e e-mail nos logs.

## Banco local e configuração

O arquivo `compose.yaml` inicia PostgreSQL com a imagem oficial `postgres:17-bookworm`, volume nomeado e verificação de prontidão. O banco é publicado em `127.0.0.1:5432` por padrão. Ajuste `POSTGRES_PORT` no `.env` se a porta estiver em uso. Em uma nova cópia do repositório, copie `.env.example` para `.env` e preencha `POSTGRES_PASSWORD` com uma senha local, por exemplo, gerada por `openssl rand -hex 24`. Execute no diretório `backend/`. [Imagem oficial do PostgreSQL](https://hub.docker.com/_/postgres) e [variáveis no Docker Compose](https://docs.docker.com/compose/how-tos/environment-variables/variable-interpolation/).

```bash
docker compose up -d --wait postgres
docker compose ps
```

Versione `appsettings.json` e `appsettings.Development.json` com padrões de configuração sem credenciais. Use o arquivo de desenvolvimento para ajustar logs e origens locais permitidas. A API lê `ConnectionStrings:DefaultConnection`, definida pela variável de ambiente `ConnectionStrings__DefaultConnection` ou por User Secrets em desenvolvimento. Versione o manifesto local do `dotnet-ef` 10.x e as migrations da `Infra`. Referencie `Microsoft.EntityFrameworkCore.Design` no projeto de inicialização `Api` como dependência privada de desenvolvimento. [Configuração do ASP.NET Core](https://learn.microsoft.com/pt-br/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0) e [CLI do EF Core](https://learn.microsoft.com/pt-br/ef/core/cli/dotnet).

Com os projetos .NET implementados, use os comandos abaixo em `backend/` para restaurar a solução, aplicar as migrations e executar a API no ambiente de desenvolvimento. O exemplo usa a senha hexadecimal indicada acima.

```bash
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

Para gerar uma migration após alterar o modelo, mantenha `Infra` como projeto de destino e `Api` como projeto de inicialização.

```bash
dotnet ef migrations add AddUserStatusIndex --project src/Infra --startup-project src/Api --output-dir Persistence/Migrations
```

O `.gitignore` na raiz exclui saídas de compilação e teste, arquivos locais da IDE e credenciais de desenvolvimento em `.env`. O exemplo de configuração, o Compose, os appsettings compartilhados e as migrations devem ser versionados.

## Critérios de aceite

- A solução compila com `dotnet build UserManagement.slnx`. Os testes rodam com `dotnet test UserManagement.slnx`.
- Os testes da `Application` verificam campos obrigatórios, CPF inválido, status inválido e se o behavior impede que o handler seja executado quando há erros.
- Os testes de integração aplicam as migrations em PostgreSQL dedicado aos testes. Verificam o ciclo completo do CRUD, a consulta após exclusão, a data de cadastro preservada na atualização e os conflitos de e-mail e CPF normalizados. Inclua uma criação concorrente com a mesma chave para confirmar que apenas uma gravação é aceita.
- Na listagem, cubra os valores padrão, a mudança de tamanho de página, a coleção vazia, a página além da última, os parâmetros inválidos e os cinco campos nas duas direções. Use registros empatados no campo principal para confirmar que `id ASC` resolve o empate nas duas direções e que páginas consecutivas apresentam os registros na ordem esperada.
- A demonstração usa a interface simples para executar as cinco operações. Após reiniciar o contêiner sem remover o volume, os registros cadastrados continuam disponíveis.
