# Plano de implementação da API de gestão de usuários

> Para executar este plano tarefa a tarefa, use `superpowers:subagent-driven-development` ou `superpowers:executing-plans`. Marque as etapas com as caixas de seleção.

**Objetivo.** Entregar uma API de gestão de usuários que persista o CRUD em PostgreSQL e ofereça listagem paginada com ordenação determinística.

**Arquitetura.** A `Api` recebe HTTP e usa `ISender.Send` para executar commands e queries da `Application`. A `Application` valida as requisições em um behavior e acessa o banco pelo contrato de repositório. `Api` e `Infra` são componentes da mesma camada externa, com a composição em `Api/Program.cs`.

**Tecnologias.** .NET 10, ASP.NET Core Controllers e OpenAPI, MediatR 12.5.0, FluentValidation, EF Core 10, Npgsql 10.x, PostgreSQL 17, Docker Compose, xUnit e Testcontainers.

**Especificação.** [API de gestão de usuários](../../especificacao-api.md).

## Restrições gerais

- A solução fica em `backend/UserManagement.slnx`. Todos os projetos usam `net10.0`.
- Referências de produção permitidas: `Application → Domain`, `Infra → Application, Domain` e `Api → Application, Infra`. Os controllers enviam requests ao MediatR e a `Infra` concentra as implementações do EF Core.
- Use inglês no código, em nomes de arquivos de código, rotas, chaves JSON, migrations e schema. Preserve `cpf`. Escreva guias e notas do repositório em português brasileiro.
- Versione as migrations, o manifesto do `dotnet-ef`, `appsettings.json` e `appsettings.Development.json`. Use `ConnectionStrings__DefaultConnection` ou User Secrets para a credencial da API. Preserve o volume do Compose e o `.env` local.
- A listagem recebe os parâmetros de paginação e ordenação definidos na especificação. Aplique a direção solicitada ao campo principal e sempre use `id ASC` no desempate.
- A interface da avaliação tem desenho próprio. A última tarefa verifica o contrato com HTTP e descreve o roteiro com a interface quando ela estiver disponível.

## Foco da revisão

- **CPF com zero inicial e outros caracteres.** O CPF mascarado mantém o zero inicial, e símbolos ou dígitos Unicode fora da máscara são rejeitados. Ver `CpfTests`, tarefa 2.
- **Cadastro com campos gerados pelo servidor.** `id` e `createdAt` enviados pelo cliente não substituem os valores gerados pela API. Ver `CreateIgnoresServerManagedFields`, tarefa 4.
- **Conflito em atualização.** Reenviar o e-mail e o CPF do próprio registro funciona. Usar o identificador de outro usuário retorna `409` e preserva o registro anterior. Ver `UpdateCollisionPreservesStoredUser`, tarefa 5.
- **Página muito além do resultado.** `page=2147483647&pageSize=100` devolve uma página vazia com os totais corretos. Ver `FarPageReturnsEmptyItemsWithTotals`, tarefa 6.
- **Empate na fronteira de páginas.** Quando dois nomes iguais ocupam páginas consecutivas, o ID menor vem primeiro também em `desc`. Ver `NameTiesKeepIdAscendingAcrossPages`, tarefa 6.

---

## Distribuição dos arquivos

Os caminhos das tarefas e os comandos de teste são relativos a `backend/`. O `README.md` fica na raiz do repositório. Na tarefa 1, comece com `cd backend`.

Quando a execução incluir commits, use `git add -- <arquivos da tarefa>` e `git commit -m "<mensagem sugerida>"` após verificar cada etapa.

| Caminho | Responsabilidade |
| --- | --- |
| `UserManagement.slnx`, `.config/dotnet-tools.json` | Projetos da solução e CLI de migrations. |
| `src/Domain/Users/` | Entidade, status e normalização de CPF. |
| `src/Application/Abstractions/`, `src/Application/Behaviors/` | Contrato de persistência, registro e execução dos validadores. |
| `src/Application/Users/` | Commands, queries, handlers, validadores e resultados. |
| `src/Infra/Persistence/` | DbContext, mapeamento, repositório PostgreSQL e migrations. |
| `src/Api/Controllers/`, `src/Api/Contracts/`, `src/Api/Errors/` | Entrada e saída HTTP, tradução de falhas e metadados OpenAPI. |
| `tests/Application.Tests/` | Regras do domínio, validadores e behavior com dependências de teste. |
| `tests/Api.IntegrationTests/` | Contrato HTTP e persistência com PostgreSQL descartável. |
| `compose.yaml`, `.env.example`, `../.gitignore`, `../README.md` | Ambiente local, arquivos ignorados e instruções de execução. |

### Tarefa 1. Criar a solução e o host HTTP

**Arquivos.** Criar `UserManagement.slnx`, `src/Domain/Domain.csproj`, `src/Application/Application.csproj`, `src/Infra/Infra.csproj`, `src/Api/Api.csproj`, `src/Api/Program.cs`, os dois appsettings em `src/Api/`, `tests/Application.Tests/Application.Tests.csproj`, `tests/Api.IntegrationTests/Api.IntegrationTests.csproj` e `tests/Api.IntegrationTests/HostingTests.cs`.

**Interfaces.** O host expõe `public partial class Program` para `WebApplicationFactory<Program>`. A documentação usa `GET /openapi/v1.json` em `Development`.

- [ ] **Gerar os seis projetos e a solução.** Use os templates abaixo, remova os arquivos de demonstração e confira que os seis projetos têm `TargetFramework` igual a `net10.0`.

```bash
cd backend
dotnet new sln --name UserManagement --format slnx
dotnet new classlib --name Domain --output src/Domain --no-restore
dotnet new classlib --name Application --output src/Application --no-restore
dotnet new classlib --name Infra --output src/Infra --no-restore
dotnet new webapi --name Api --output src/Api --use-controllers --no-openapi --no-restore
dotnet new xunit --name Application.Tests --output tests/Application.Tests --no-restore
dotnet new xunit --name Api.IntegrationTests --output tests/Api.IntegrationTests --no-restore
dotnet sln UserManagement.slnx add src/*/*.csproj tests/*/*.csproj
```

- [ ] **Declarar as referências.** Adicione as referências de produção da seção de restrições. `Application.Tests` referencia `Application` e `Domain`, e `Api.IntegrationTests` referencia `Api`. Adicione `Microsoft.AspNetCore.OpenApi` 10.0.15 à `Api` e `Microsoft.AspNetCore.Mvc.Testing` 10.0.15 a `Api.IntegrationTests`. [Testes de integração do ASP.NET Core](https://learn.microsoft.com/pt-br/aspnet/core/test/integration-tests?view=aspnetcore-10.0).
- [ ] **Expor o ponto de entrada ao projeto de testes.** Acrescente `public partial class Program { }` ao final do `Program.cs` gerado pelo template.
- [ ] **Escrever o teste do host.** Em `OpenApiIsAvailableOnlyInDevelopment`, use `WebApplicationFactory<Program>` com `UseEnvironment` e confira `Assert.Equal(expectedStatus, (await client.GetAsync("/openapi/v1.json")).StatusCode)`, com `200` em `Development` e `404` em `Production`.
- [ ] **Confirmar a falha.** Rode `dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~HostingTests`. Antes de `MapOpenApi`, a variação `Development` recebe `404`.
- [ ] **Configurar o host.** Registre controllers e OpenAPI em `Program.cs`, mapeie controllers e mapeie OpenAPI apenas em `Development`. Os appsettings compartilham padrões sem credencial. Use `ConnectionStrings:DefaultConnection` sem valor secreto, logging de desenvolvimento e `Cors:AllowedOrigins` inicialmente vazio. Registre a política CORS para origens explícitas e aplique-a quando configurada. Rode `dotnet user-secrets init --project src/Api` para criar o `UserSecretsId`.
- [ ] **Verificar a estrutura.** Rode `dotnet sln UserManagement.slnx list`, `dotnet build UserManagement.slnx` e `dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~HostingTests`. Espere seis projetos, compilação bem-sucedida e os dois cenários aprovados.
- [ ] **Registrar a etapa.** Prepare a solução, os projetos, o host e o teste. Commit sugerido `Set up the .NET API solution`.

### Tarefa 2. Modelar o usuário e validar CPF

**Arquivos.** Criar `src/Domain/Users/Cpf.cs`, `src/Domain/Users/User.cs`, `src/Domain/Users/UserStatus.cs`, `tests/Application.Tests/Users/CpfTests.cs` e `tests/Application.Tests/Users/UserTests.cs`.

**Interfaces.** `Cpf.TryNormalize(string? value, out string normalized): bool`. `User.Create(Guid id, string fullName, string email, string cpf, DateTime createdAt, UserStatus status): User`. `User.Update(string fullName, string email, string cpf, UserStatus status): void`. `UserStatus` tem `Active` e `Inactive`. `User` expõe `Id`, `FullName`, `Email`, `Cpf`, `CreatedAt` e `Status` para leitura.

- [ ] **Escrever testes de CPF e da entidade.** Em `NormalizeValidCpf`, use entradas `012.345.678-90` e `01234567890` e confirme `Assert.Equal("01234567890", normalized)` depois de `Assert.True(Cpf.TryNormalize(input, out var normalized))`. Em `RejectInvalidCpf`, confirme `Assert.False(...)` para `null`, `111.111.111-11`, `012.345.678-91`, `012/345/678-90` e `０1234567890`. Em `UpdateKeepsIdentityAndCreationDate`, forneça ID e timestamp UTC fixos e verifique que ambos são preservados ao atualizar os quatro campos editáveis. Teste também `"  Sample Person  "` e `"  PERSON@EXAMPLE.TEST  "`, que devem ser persistidos como `"Sample Person"` e `"person@example.test"`.
- [ ] **Confirmar a falha.** Rode `dotnet test tests/Application.Tests --filter 'FullyQualifiedName~CpfTests|FullyQualifiedName~UserTests'`. Espere falha pelos tipos ou métodos de domínio ausentes.
- [ ] **Implementar CPF.** Aceite 11 dígitos ASCII ou a máscara exata, rejeite dígitos repetidos e calcule os dois verificadores. Para cada verificador, multiplique os primeiros 9 ou 10 dígitos pelos pesos decrescentes de 10 ou 11 até 2. O valor esperado é 0 quando o resto por 11 é menor que 2, ou 11 menos o resto nos demais casos.
- [ ] **Implementar a entidade.** Normalize nome e e-mail, valide os limites de 150 e 254 caracteres após `Trim`, persista o CPF normalizado e preserve `Id` e `CreatedAt` na atualização. Exija ID não vazio, timestamp UTC e status definido na criação. Permita materialização do EF com um construtor privado sem parâmetros.
- [ ] **Verificar o domínio.** Repita o comando de testes e confira todos os casos aprovados.
- [ ] **Registrar a etapa.** Prepare apenas os arquivos da tarefa. Commit sugerido `Model users and normalize CPF`.

### Tarefa 3. Persistir usuários e gerar a migration inicial

**Arquivos.** Criar `src/Application/Abstractions/IUserRepository.cs`, `src/Application/Users/UserConflictException.cs`, `src/Application/Users/UserNotFoundException.cs`, `src/Infra/DependencyInjection.cs`, `src/Infra/Persistence/UserManagementDbContext.cs`, `src/Infra/Persistence/Configurations/UserConfiguration.cs`, `src/Infra/Persistence/Repositories/UserRepository.cs` e a migration e o snapshot em `src/Infra/Persistence/Migrations/`. Criar `tests/Api.IntegrationTests/Fixtures/PostgresFixture.cs`, `tests/Api.IntegrationTests/Fixtures/ApiFactory.cs` e `tests/Api.IntegrationTests/PostgresPersistenceTests.cs`. Atualizar `src/Api/Program.cs` e os projetos da infraestrutura, startup e testes de integração.

**Interfaces.** `IUserRepository` oferece `Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)`, `Task AddAsync(User user, CancellationToken cancellationToken)`, `Task UpdateAsync(User user, CancellationToken cancellationToken)` e `Task DeleteAsync(User user, CancellationToken cancellationToken)`. `UserConflictException(string field)` expõe `Field` e `UserNotFoundException(Guid id)` representa ausência. `AddInfrastructure(IServiceCollection services)` registra DbContext e repositório como scoped.

- [ ] **Adicionar dependências.** Em `Infra`, instale `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.2, `Microsoft.EntityFrameworkCore.Relational` 10.0.15 e `Microsoft.Extensions.Configuration.Abstractions` 10.0.15. Em `Api`, adicione `Microsoft.EntityFrameworkCore.Design` 10.0.15 com `PrivateAssets=all`. Em `Api.IntegrationTests`, adicione `Testcontainers.PostgreSql` 4.10.0. [Dependências do provedor Npgsql](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.2#dependencies-body-tab) e [módulo PostgreSQL do Testcontainers](https://dotnet.testcontainers.org/modules/postgres/).
- [ ] **Preparar integração isolada.** `PostgresFixture` inicia `postgres:17-bookworm` em porta dinâmica por coleção de testes. Crie um banco de nome único por teste e descarte o contêiner ao fim da coleção. `ApiFactory : WebApplicationFactory<Program>` injeta a conexão desse banco em `ConnectionStrings:DefaultConnection` e aplica `Database.MigrateAsync` antes de liberar o cliente HTTP. Nomeie a coleção xUnit `PostgreSQL`.
- [ ] **Escrever testes de persistência.** `MigrationCreatesUsersWithUtcTimestamp` insere um usuário com `01234567890` e `2026-01-01T10:00:00Z`, abre outro escopo e confere CPF, status e data com `DateTimeKind.Utc`. `UniqueIndexesReportConflictField` tenta gravar CPF normalizado ou e-mail normalizado igual ao de outro usuário em novo escopo e confirma `Assert.ThrowsAsync<UserConflictException>(...)` e `Assert.Equal(expectedField, error.Field)`.
- [ ] **Confirmar a falha.** Rode `dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~PostgresPersistenceTests`. Espere falha pela persistência ainda ausente.
- [ ] **Mapear e registrar a persistência.** Configure `users(id, full_name, email, cpf, created_at, status)` com os tipos e limites da especificação, colunas obrigatórias, `ux_users_email`, `ux_users_cpf` e uma restrição para `active` e `inactive`. Use conversão de `UserStatus` para esses dois valores, `timestamp with time zone` para `CreatedAt` e `ValueGeneratedNever` para `Id`. Registre `UseNpgsql` com a conexão obtida de `IConfiguration` quando o DbContext for resolvido. Chame o registro em `Program.cs`.
- [ ] **Implementar o repositório.** Use consulta com rastreamento para `GetByIdAsync`, e um `SaveChangesAsync(cancellationToken)` por operação de escrita. Mapeie `UniqueViolation` pelo nome exato do índice para `UserConflictException("email")` ou `UserConflictException("cpf")`. Converta exclusão concorrente de um registro já lido em `UserNotFoundException` quando a gravação do registro afetar zero linhas.
- [ ] **Criar o manifesto e gerar a migration.** Com o `.env` local já existente, execute os comandos abaixo em Bash. Confira que os arquivos gerados em `Infra` criam os índices e a restrição e que a conexão usada pelo teste vem da fixture. [Referência da CLI do EF Core](https://learn.microsoft.com/pt-br/ef/core/cli/dotnet).

```bash
dotnet new tool-manifest --output .config
dotnet tool install dotnet-ef --version 10.0.15 --tool-manifest .config/dotnet-tools.json
set -a
. ./.env
set +a
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Host=127.0.0.1;Port=${POSTGRES_PORT:-5432};Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
dotnet ef migrations add InitialCreate --project src/Infra --startup-project src/Api --output-dir Persistence/Migrations
```

- [ ] **Verificar a persistência.** Repita o comando da tarefa e rode `dotnet ef migrations has-pending-model-changes --project src/Infra --startup-project src/Api`. Espere testes aprovados e modelo correspondente ao snapshot.
- [ ] **Registrar a etapa.** Prepare manifesto, migration, código e testes da tarefa. Commit sugerido `Persist users in PostgreSQL`.

### Tarefa 4. Cadastrar usuários e padronizar erros

**Arquivos.** Criar `src/Application/DependencyInjection.cs`, `src/Application/Behaviors/ValidationBehavior.cs`, `src/Application/Users/UserDetails.cs`, `src/Application/Users/UserInputRules.cs`, `src/Application/Users/CreateUser.cs`, `src/Api/Controllers/UsersController.cs`, `src/Api/Contracts/CreateUserRequest.cs`, `src/Api/Contracts/UserResponse.cs`, `src/Api/Errors/ApiExceptionHandler.cs`, `src/Api/Errors/ApiProblemDetails.cs`, `tests/Application.Tests/Users/CreateUserValidatorTests.cs`, `tests/Application.Tests/Behaviors/ValidationBehaviorTests.cs` e `tests/Api.IntegrationTests/CreateUserTests.cs`. Atualizar `src/Application/Application.csproj` e `src/Api/Program.cs`.

**Interfaces.** `CreateUserCommand(string? FullName, string? Email, string? Cpf, string? Status) : IRequest<UserDetails>`. `UserDetails(Guid Id, string FullName, string Email, string Cpf, DateTime CreatedAt, string Status)` tem `From(User user)`. `UserResponse` usa os mesmos campos e mapeia `UserDetails` em `UserResponse.From(UserDetails user)`. O controller expõe `POST /api/users`.

Para reutilizar as regras no PUT, exponha `UserInputRules.Configure<T>(AbstractValidator<T> validator, Expression<Func<T, string?>> fullName, Expression<Func<T, string?>> email, Expression<Func<T, string?>> cpf, Expression<Func<T, string?>> status): void` para os dois validators.

- [ ] **Escrever testes de validação.** `CreateUserValidatorTests` verifica `FullName` em branco e com 151 caracteres, e-mail malformado ou com mais de 254 caracteres após `Trim`, CPF com dígito errado e status `pending` ou `null`. Em `InvalidRequestDoesNotInvokeHandler`, registre um `ProbeCommand`, um validator e um `ProbeHandler` que incrementa um contador na primeira linha. Envie o command inválido pelo `ISender` e confira `Assert.ThrowsAsync<ValidationException>(...)` e `Assert.Equal(0, probe.Calls)`. Um command válido incrementa esse contador uma vez.
- [ ] **Escrever testes HTTP.** `CreateReturns201LocationAndNormalizedUser` envia CPF mascarado, e-mail em maiúsculas e status omitido. Confirme `201`, `Location = /api/users/{id}`, ID não vazio, CPF sem máscara, e-mail em minúsculas, status `active` e `createdAt` UTC igual ao relógio fixo da fixture. `CreateIgnoresServerManagedFields` inclui `id` e `createdAt` no JSON e confere os valores gerados. `InvalidCreateReturnsFieldErrors` exige `400`, `application/problem+json`, `traceId` e chaves como `fullName` e `cpf`. `MalformedJsonReturnsProblemDetails` envia `{` e exige `errors.body`.
- [ ] **Escrever testes de unicidade.** Em `CreateCollisionReturns409`, teste colisões de e-mail com variação de caixa e CPF com ou sem máscara. Em `ConcurrentCreatesWithSameCpfAcceptExactlyOne`, dispare duas requisições com e-mails distintos e o mesmo CPF e confirme um `201`, um `409` com `field = "cpf"` e apenas uma linha para aquele CPF.
- [ ] **Confirmar a falha.** Rode `dotnet test tests/Application.Tests --filter 'FullyQualifiedName~CreateUserValidatorTests|FullyQualifiedName~ValidationBehaviorTests'` e `dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~CreateUserTests`. Espere tipos ausentes nos testes de aplicação e rota ainda ausente no host.
- [ ] **Implementar o behavior.** Adicione `MediatR` 12.5.0 e `FluentValidation.DependencyInjectionExtensions` 12.1.1 à `Application`. `AddApplication(IServiceCollection services)` registra handlers, validadores públicos com `AddValidatorsFromAssembly` e `AddOpenBehavior(typeof(ValidationBehavior<,>))`. Em `Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)`, execute `ValidateAsync`, agregue falhas e lance `ValidationException` antes de chamar `next()`. [Registro do MediatR 12.5.0](https://github.com/LuckyPennySoftware/MediatR/blob/v12.5.0/README.md#registering-with-iservicecollection) e [injeção de dependências do FluentValidation](https://docs.fluentvalidation.net/en/latest/di.html).
- [ ] **Implementar o cadastro.** O validator usa as regras compartilhadas e `Cpf.TryNormalize`. O handler chama `User.Create` com `Guid.NewGuid()` e `TimeProvider.GetUtcNow().UtcDateTime`, grava e devolve `UserDetails`. Registre `TimeProvider.System` na API e substitua por um relógio fixo no teste de timestamp.
- [ ] **Implementar HTTP e erros.** Inicialize `CreateUserRequest.Status` com `"active"` e rejeite `null` explícito pelo validator. O endpoint envia o command e responde com `Created("/api/users/{id}", ...)`, substituindo `{id}` pelo ID real. Registre `AddProblemDetails`, `IExceptionHandler` e `UseExceptionHandler`. Use `ApiProblemDetails` no handler e em `InvalidModelStateResponseFactory` para normalizar chaves de campos e rotas e acrescentar `traceId` com `Activity.Current?.Id ?? context.TraceIdentifier`. Mapeie validação para `400`, ausência para `404`, conflito para `409` com extensão `field`, e exceção inesperada para `500` com mensagem genérica. Registre o tipo da falha e `traceId` sem incluir CPF, e-mail ou corpo.
- [ ] **Verificar o cadastro.** Repita os comandos de teste e espere todos os casos aprovados, incluindo os de concorrência.
- [ ] **Registrar a etapa.** Inclua os arquivos do fluxo e os testes. Commit sugerido `Add user registration and request validation`.

### Tarefa 5. Consultar, atualizar e excluir por ID

**Arquivos.** Criar `src/Application/Users/GetUser.cs`, `src/Application/Users/UpdateUser.cs`, `src/Application/Users/DeleteUser.cs`, `src/Api/Contracts/UpdateUserRequest.cs` e `tests/Api.IntegrationTests/UserLifecycleTests.cs`. Alterar `src/Api/Controllers/UsersController.cs`.

**Interfaces.** `GetUserQuery(Guid Id) : IRequest<UserDetails>`, `UpdateUserCommand(Guid Id, string? FullName, string? Email, string? Cpf, string? Status) : IRequest<UserDetails>` e `DeleteUserCommand(Guid Id) : IRequest`. Use os métodos do `IUserRepository` da tarefa 3 e as regras comuns de `UserInputRules`.

- [ ] **Escrever testes HTTP.** `GetUpdateDeleteUserLifecycle` cria, segue o `Location`, atualiza os quatro campos, confere `200`, ID e data originais, confirma leitura com `inactive`, exclui com `204` e verifica `404` no próximo GET. `InvalidIdReturns400` usa `not-a-uuid` e o UUID de zeros em GET, PUT e DELETE e exige `errors.id`. `MissingUserReturns404` testa os três métodos com um UUID válido ausente. `UpdateRequiresStatus` exige `400` e `errors.status` quando o campo é omitido.
- [ ] **Escrever testes de conflitos e falha inesperada.** Em `UpdateCollisionPreservesStoredUser`, atualize sem mudar o CPF e e-mail do próprio usuário e confirme `200`. Em seguida, tente usar o CPF ou e-mail de outro registro, confira `409` com o campo correto e consulte novamente para provar que a alteração falhou integralmente. Em `UnexpectedErrorReturnsGenericProblem`, substitua `IUserRepository` por uma implementação que lança `InvalidOperationException("probe-internal-error")` na leitura. Confirme `500`, `application/problem+json`, `traceId` preenchido e `Assert.DoesNotContain("probe-internal-error", body)`.
- [ ] **Confirmar a falha.** Rode `dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~UserLifecycleTests`. Espere falhas pelo contrato HTTP ainda indisponível.
- [ ] **Implementar os casos de uso.** Valide `Id` diferente de `Guid.Empty` nos três requests. Na atualização, replique as regras de entrada pela implementação compartilhada e exija status `active` ou `inactive`. Obtenha o registro pelo repositório em cada operação e lance `UserNotFoundException` quando ausente. No PUT, aplique `User.Update` e persista a entidade rastreada. No DELETE, remova e grave.
- [ ] **Expor o contrato HTTP.** Mapeie GET, PUT e DELETE em `{id}`. Receba `Guid id` do binding e deixe IDs malformados passarem pelo tratamento de ModelState. Retorne `200` com `UserResponse` no GET e PUT, e `204` sem corpo no DELETE. Declare os códigos e tipos de resposta no OpenAPI.
- [ ] **Verificar o ciclo.** Repita o comando e rode também `dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~CreateUserTests`. Espere todos os casos aprovados.
- [ ] **Registrar a etapa.** Prepare contratos, handlers, controller e testes. Commit sugerido `Add user lookup update and deletion`.

### Tarefa 6. Implementar listagem paginada e determinística

**Arquivos.** Criar `src/Application/Users/ListUsers.cs`, `src/Api/Contracts/ListUsersParameters.cs`, `src/Api/Contracts/ListUsersResponse.cs`, `tests/Application.Tests/Users/ListUsersValidatorTests.cs` e `tests/Api.IntegrationTests/ListUsersTests.cs`. Atualizar `src/Application/Abstractions/IUserRepository.cs`, `src/Infra/Persistence/Repositories/UserRepository.cs`, `src/Api/Controllers/UsersController.cs` e a implementação de teste do repositório em `tests/Api.IntegrationTests/UserLifecycleTests.cs`.

**Interfaces.** `ListUsersQuery(int Page = 1, int PageSize = 20, string? SortBy = "createdAt", string? SortDirection = "desc") : IRequest<ListUsersResult>`. `ListUsersResult(IReadOnlyList<UserDetails> Items, int Page, int PageSize, int TotalItems, int TotalPages)`. O repositório expõe `Task<(IReadOnlyList<User> Items, int TotalItems)> ListAsync(int page, int pageSize, UserSortField sortField, SortDirection sortDirection, CancellationToken cancellationToken)`. Os enums têm `FullName`, `Email`, `Cpf`, `CreatedAt`, `Status` e `Asc` ou `Desc`, respectivamente.

- [ ] **Escrever testes de parâmetros.** `ListUsersValidatorTests` aceita os padrões, `pageSize=1`, `pageSize=100` e as dez combinações de campo e direção. Rejeita `page=0`, `pageSize=0`, `pageSize=101`, `sortBy=id` e `sortDirection=up`, com a chave do campo afetado. Pela API, `InvalidListParametersReturn400` confirma também que `page=abc`, `page=` e `sortBy=` produzem `400` com a respectiva chave de query string.
- [ ] **Escrever testes de paginação.** `DefaultsReturnPageOneWithNewestFirst` confirma `page=1`, `pageSize=20`, `totalItems=4`, `totalPages=1` e sequência `B,A,D,C` com os dados abaixo. Com `page=2&pageSize=3`, confirme `items.Count == 1` e `totalPages == 2`. `EmptyListReturnsZeroPages` confirma os totais zero. `FarPageReturnsEmptyItemsWithTotals` usa `2147483647` e `100` e espera `200`, itens vazios, quatro itens ao todo e uma página.
- [ ] **Escrever testes das dez ordenações.** Use IDs UUID terminados em `0001`, `0002`, `0003` e `0004` para A a D, mesmo prefixo e IDs crescentes. Insira D, C, B, A para exercitar o desempate. Use as datas às 10h UTC em janeiro de 2026.

| ID | `fullName` | `email` | `cpf` | Dia de `createdAt` | `status` |
| --- | --- | --- | --- | --- | --- |
| A | Taylor Young | z@example.test | 12345678909 | 02 | inactive |
| B | Alex Brown | b@example.test | 01234567890 | 03 | active |
| C | Alex Brown | a@example.test | 52998224725 | 01 | active |
| D | Morgan Lee | m@example.test | 11144477735 | 02 | inactive |

Em `SortsByRequestedFieldAndDirection`, compare a sequência completa de IDs retornados com `Assert.Equal(expectedIds, actualIds)`.

| `sortBy` | `asc` | `desc` |
| --- | --- | --- |
| `fullName` | B, C, D, A | A, D, B, C |
| `email` | C, B, D, A | A, D, B, C |
| `cpf` | B, D, A, C | C, A, D, B |
| `createdAt` | C, A, D, B | B, A, D, C |
| `status` | B, C, A, D | A, D, B, C |

Em `NameTiesKeepIdAscendingAcrossPages`, use `pageSize=1` e confira B antes de C nas páginas 1 e 2 em `asc` e nas páginas 3 e 4 em `desc`.

- [ ] **Confirmar a falha.** Rode `dotnet test tests/Application.Tests --filter FullyQualifiedName~ListUsersValidatorTests` e `dotnet test tests/Api.IntegrationTests --filter FullyQualifiedName~ListUsersTests`. Espere os tipos e a rota ainda ausentes.
- [ ] **Implementar validação e paginação.** Mapeie os textos do contrato para os enums após a validação na `Application`. Na `Infra`, use `AsNoTracking()`, conte os registros antes de paginar e calcule o deslocamento em `long`. Retorne itens vazios se o deslocamento for maior ou igual ao total. Para as demais páginas, escolha a expressão de ordenação correspondente, aplique `ThenBy(user => user.Id)` e só então `Skip` e `Take`. Ordene CPF pela string normalizada e status pelo valor persistido. Na `Application`, calcule `totalPages` sem somar `pageSize` ao total, usando `0` para coleção vazia e `1 + (totalItems - 1) / pageSize` nos demais casos.
- [ ] **Expor `GET /api/users`.** Receba `[FromQuery] ListUsersParameters` com valores iniciais para parâmetros omitidos. Use inteiros não anuláveis para página e tamanho e strings anuláveis para ordenação, para que valores vazios passem pela validação. Encaminhe a query pelo MediatR. Descreva os quatro parâmetros e o limite de `pageSize` no OpenAPI. `ListUsersResponse` contém `Items` no formato `UserResponse`, `Page`, `PageSize`, `TotalItems` e `TotalPages` em camelCase no JSON.
- [ ] **Verificar a listagem.** Repita os dois comandos e confira a tradução para SQL com ordenação primária seguida de `id ASC` antes da paginação. Para inspecionar a consulta, use `ToQueryString()` em depuração ou um interceptor de teste sem valores sensíveis.
- [ ] **Registrar a etapa.** Prepare os arquivos de listagem e testes. Commit sugerido `Add deterministic user pagination and sorting`.

### Tarefa 7. Verificar execução local e documentar o roteiro

**Arquivos.** Atualizar `../README.md` em português brasileiro. Conferir os arquivos existentes `compose.yaml`, `.env.example` e `../.gitignore`.

**Interfaces.** PostgreSQL em `127.0.0.1:5432` por padrão. API de desenvolvimento em `http://127.0.0.1:5080`. OpenAPI em `/openapi/v1.json`.

- [ ] **Documentar a execução.** Inclua pré-requisitos, estrutura da solução, criação do `.env` a partir do exemplo quando estiver ausente, exportação da conexão conforme tarefa 3, uso de `appsettings.Development.json`, comandos do Docker e comandos de restauração, migration, build, teste e execução da API. Inclua uma chamada de listagem com `pageSize`, `sortBy` e `sortDirection`.
- [ ] **Validar configuração e compilação.** Rode os comandos abaixo, com conexão local configurada, e confira que as migrations foram aplicadas, o build concluiu e a suíte está verde. `git check-ignore -v .env` deve localizar a regra que protege a credencial.

```bash
docker compose config --quiet
docker compose up -d --wait postgres
dotnet tool restore
dotnet restore UserManagement.slnx
dotnet build UserManagement.slnx --no-restore
dotnet ef database update --project src/Infra --startup-project src/Api
dotnet test UserManagement.slnx --no-build
git check-ignore -v .env
```

- [ ] **Executar a verificação HTTP.** Inicie com `dotnet run --project src/Api --no-launch-profile --urls http://127.0.0.1:5080` e `ASPNETCORE_ENVIRONMENT=Development`. Confirme que o OpenAPI descreve os cinco endpoints. Cadastre um usuário fictício com CPF e e-mail livres, siga o `Location`, atualize o status, consulte a listagem paginada e confirme que o registro consta na página esperada. Execute `docker compose restart postgres` e `docker compose up -d --wait postgres`. Consulte o mesmo ID, confirme os dados preservados e então exclua o usuário de demonstração. O GET final retorna `404`.
- [ ] **Conferir a integração com a interface de avaliação.** Quando a interface estiver disponível, repita o roteiro de cadastro, listagem, consulta, atualização e exclusão pelo navegador. Configure `Cors:AllowedOrigins` com a origem local exata se a interface for servida de outra origem.
- [ ] **Registrar a etapa.** Prepare o guia local concluído. Commit sugerido `Document local setup and API verification`.
