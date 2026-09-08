# Gestão de Ordens de Serviço — Backend

API REST em ASP.NET Core 8, Entity Framework Core e SQL Server. Código organizado em Domain (entidades e transições), Application (contratos e serviços de aplicação) e Infra (persistência e migrations). Frontend ainda não iniciado.

## Executar no PowerShell

Requisitos: SDK .NET 8.0.4xx (selecionado pelo `global.json`), ferramenta `dotnet-ef` e SQL Server 2022 (local ou Docker). Use senhas próprias; não versione os valores das variáveis.

```powershell
$env:DB_PASSWORD = 'SuaSenhaForteDoBanco123!'
docker compose up -d
$env:ConnectionStrings__Database = "Server=localhost,1433;Database=ServiceOrders;User Id=sa;Password=$env:DB_PASSWORD;TrustServerCertificate=true"
$env:Jwt__Key = 'SubstituaPorUmaChaveAleatoriaComPeloMenos32Caracteres'
dotnet restore ServiceOrders.sln --configfile NuGet.Config
dotnet ef database update --project src/Os.Api
$env:Seed__Email = 'admin@example.com'
$env:Seed__Password = 'SuaSenhaAdminForte123!'
dotnet run --project src/Os.Api -- --seed-admin
Remove-Item Env:Seed__Password
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Os.Api -- --urls http://localhost:5080
```

Swagger: http://localhost:5080/swagger. Faça login em `POST /api/auth/login`, clique em **Authorize** e cole o `accessToken`. Em outros clientes, envie `Authorization: Bearer <accessToken>`. Token expira em uma hora. Usuário desativado perde acesso mesmo com token ainda válido. Não há cadastro público.

Em produção, publique atrás de HTTPS, configure segredos fora do repositório, use usuário de banco com permissões limitadas e certificado válido (remova `TrustServerCertificate=true`). Migrations são executadas explicitamente antes da aplicação.

## Funcionalidades e rotas

- `POST /api/auth/login`, `GET /api/auth/me`.
- Admin: `GET/POST /api/users`; `PATCH /api/users/{id}/active?active=false` desativa um usuário.
- Clientes: `GET/POST /api/customers`, `PUT/DELETE /api/customers/{id}`. Escrita exclusiva do Admin; registros com OS não podem ser excluídos.
- Serviços e peças: `GET/POST /api/catalog`, `PUT/DELETE /api/catalog/{id}`. Escrita exclusiva do Admin; itens utilizados não podem ser excluídos.
- OS: `GET/POST /api/orders`, `GET /api/orders/{id}`. Apenas Admin cria OS e atribui um técnico ativo.
- `PATCH /api/orders/{id}/status`, corpo `{"status":"EmAndamento"}`.
- `POST /api/orders/{id}/items`, corpo `{"catalogItemId":"GUID","quantity":2}`; `DELETE /api/orders/{id}/items/{itemId}`.
- `GET /api/orders/{id}/history`: ator, ação, detalhes e horário UTC.
- `GET /api/orders/{id}/summary`: download de mensagem formatada em TXT. PDF e envio de webhook não estão implementados.
- Admin: `GET /api/reports/monthly?year=2026&month=9`: quantidade criada e receita de OS concluídas no mês (UTC).
- `GET /health`: prontidão com conexão real ao SQL Server; retorna 200 quando saudável e 503 em falhas.
- `GET /health/live`: disponibilidade do processo, independente do banco.

Listagens de clientes, catálogo e OS aceitam `page` (padrão 1) e `pageSize` (padrão 20, máximo 100). OS também aceita `customerId`, `search` (nome do cliente), `status`, `from` (inclusivo), `to` (exclusivo), com datas ISO 8601. Retorno contém `total`, `page`, `pageSize` e `data`.

## Regras

Fluxo: Aberta → EmAndamento → Concluida. EmAndamento pode ir para AguardandoPeca e voltar. Qualquer estado aberto pode ir para Cancelada. Concluida e Cancelada são finais, sem alterações de itens ou status.

Admin acessa todas as OS. Técnico acessa somente suas OS e os clientes relacionados, consulta o catálogo e altera itens/status. Não gerencia usuários, clientes, catálogo ou relatórios.

Mão de obra é um item de catálogo do tipo `Servico`; materiais são `Peca`. Total sempre calculado no servidor com valores decimais. Cada item preserva nome, tipo e preço do momento da inclusão, mesmo se o catálogo mudar. Para alterar quantidade, remova e adicione o item novamente (usa o preço atual).

Criação, status e alterações de itens geram histórico no mesmo salvamento. Rowversion detecta concorrência durante alterações e retorna 409. Senhas usam o PasswordHasher do ASP.NET Core; login limitado a dez tentativas por minuto por IP. Erros seguem Problem Details; validações retornam 400, falta de autenticação 401, permissões 403, ausência 404 e conflitos 409. OS de outro técnico retorna 404.

## Validação

```powershell
dotnet test tests/Os.Tests --configuration Release
.\scripts\Test-Integration.ps1
```

Os testes unitários cobrem regras de negócio, services, contratos, arquitetura e validação decimal em pt-BR/en-US. Os testes de integração usam HTTP, autenticação JWT e SQL Server real, com migrations, isolamento entre técnicos, auditoria, concorrência, CORS, Swagger e saúde do banco.

## Organização e formatação

- `Controllers`: rotas, autorização e respostas HTTP, separados por recurso.
- `Application/Services`: casos de uso; cada implementação possui sua interface em `Application/Interfaces/Services`.
- `Application/Contracts`: DTOs de entrada e saída tipados.
- `Application/Interfaces/Repositories`: contratos de acesso a usuários, clientes, catálogo, OS e relatórios.
- `Application/Abstractions`: contratos de unidade de trabalho, usuário atual e emissão de token.
- `Infra/Repositories`: implementações dos repositories com EF Core.
- `Domain`: entidades em arquivos individuais; regras de status, itens e auditoria da OS.
- `Infra`: EF Core, migrations, JWT e criação do administrador inicial.
- `Configuration`: registro de dependências e configuração da API.
- `Middleware`: tratamento global de exceções.

Fluxo: `Controller → IService → IRepository → OsDb`. Os controllers recebem interfaces de services, e os services recebem interfaces de repositories. A camada Application não referencia EF Core, DbSet ou IQueryable. Os repositories trabalham com entidades internamente; os services retornam DTOs, incluindo clientes e catálogo.

`IUnitOfWork` compartilha o mesmo DbContext dos repositories e faz um único salvamento por operação, mantendo itens, status e auditoria na mesma transação. O repository de OS preserva a verificação de rowversion ao alterar itens. A separação continua por pastas dentro de um projeto.

A formatação é definida em `.editorconfig`. Para aplicar e conferir:

```powershell
dotnet format ServiceOrders.sln --no-restore
dotnet format ServiceOrders.sln --no-restore --verify-no-changes
```

## GitHub Actions

O workflow `.github/workflows/backend-ci.yml` executa em push, pull request e acionamento manual. Ele restaura dependências, confere formatação, compila em Release, executa testes e disponibiliza os resultados TRX por sete dias. Usa apenas permissão de leitura do repositório e não exige credenciais de produção.

Referência: [documentação oficial de CI para .NET](https://docs.github.com/en/actions/tutorials/build-and-test-code/net).

Para reproduzir localmente:

```powershell
dotnet restore ServiceOrders.sln --configfile NuGet.Config
dotnet format ServiceOrders.sln --no-restore --verify-no-changes
dotnet build ServiceOrders.sln --configuration Release --no-restore
dotnet test tests/Os.Tests --configuration Release --no-build --no-restore --logger trx --results-directory TestResults
.\scripts\Test-Integration.ps1 -NoBuild
```

Os testes de services usam mocks das interfaces; os de repositories usam EF InMemory para conferir filtros, paginação e relatórios. InMemory não valida transações, constraints, SQL gerado ou concorrência de SQL Server. Os testes de arquitetura conferem interfaces, injeção de dependência e retornos sem entidades de domínio.

O workflow utiliza um SQL Server temporário com health check no runner do GitHub. A senha declarada no workflow pertence exclusivamente ao banco descartável do CI. Os testes exigem `IntegrationTests__ConnectionString`; não são ignorados silenciosamente quando essa configuração falta.

## Banco local criado

A migration `20260908134144_InitialCreate` foi aplicada ao banco `ServiceOrders` no SQL Server em Docker. Foram conferidas as seis tabelas de aplicação, a tabela `__EFMigrationsHistory`, seis chaves estrangeiras e quinze índices (incluindo chaves primárias e o histórico de migrations).

- Servidor: `127.0.0.1,1433`.
- Banco: `ServiceOrders`.
- Usuário local: `sa`.
- Senha e string de conexão: arquivo `.env`, ignorado pelo Git.
- Dados persistidos no volume `ordemdeserviso_os-data`.

Para reaplicar as migrations pendentes com o SQL Server já pronto:

```powershell
.\scripts\Update-Database.ps1
```

O script carrega a conexão e a chave JWT do `.env` para o processo PowerShell. Na mesma sessão, `dotnet run --project src/Os.Api` usa essa configuração. O comando é idempotente: migrations já aplicadas não são executadas novamente.

## Integração HTTP e SQL Server

`tests/Os.IntegrationTests` usa `WebApplicationFactory` com o provider SQL Server. Cada execução cria um banco `OsIntegration_<GUID>`, aplica migrations e insere somente os usuários de teste. O banco é removido no final após conferir que o nome pertence à execução; o banco `ServiceOrders` não é usado nem apagado pelos testes.

Para executar localmente com o SQL Server do projeto pronto:

```powershell
dotnet restore ServiceOrders.sln --configfile NuGet.Config
dotnet test tests/Os.Tests --configuration Release --no-restore
.\scripts\Test-Integration.ps1
```

O script usa `IntegrationTests__ConnectionString` quando já definida, ou lê a conexão do `.env` apenas para localizar o servidor; os testes substituem sempre o nome do banco. O login SQL precisa de permissão para criar e remover bancos de teste. Em outro ambiente, defina essa variável explicitamente.

Os testes de concorrência sincronizam duas requisições HTTP após a leitura da mesma rowversion. Verificam que uma vence, a outra recebe 409 e nenhum item ou registro de auditoria da operação perdedora é persistido.

## Configuração para o frontend

`Cors:AllowedOrigins` contém as origens permitidas. Em desenvolvimento, `appsettings.Development.json` permite `http://localhost:5173` e `http://localhost:3000`. Em produção, nenhuma origem é permitida por padrão. Configure, por exemplo, `Cors__AllowedOrigins__0=https://app.example.com`. Informe somente origem, sem caminho ou wildcard.

São aceitos os métodos GET, POST, PUT, PATCH e DELETE, com headers Authorization e Content-Type. Location e Content-Disposition são expostos ao navegador. Não há cookies de autenticação nem liberação irrestrita de origens.

## Correções verificadas pela integração

A validação de preços usa cultura invariável para interpretar os limites decimais. Os IDs de itens e auditoria são gerados no domínio e mapeados com `ValueGeneratedNever`, evitando que novos registros sejam tratados como atualizações. A migration `ConfigureClientAssignedChildIds` registra essa mudança no snapshot do EF, sem DDL sobre as tabelas existentes.
