# Gestão de Ordens de Serviço

API REST em ASP.NET Core 8, Entity Framework Core e SQL Server. Código organizado em Domain (entidades e transições), Application (contratos e serviços de aplicação) e Infra (persistência e migrations). Frontend React/TypeScript em `frontend/`, com shadcn/ui e Sonner.

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

Swagger: http://localhost:5080/swagger. Faça login em `POST /api/auth/login`, clique em **Authorize** e cole o `accessToken`. Em outros clientes, envie `Authorization: Bearer <accessToken>`. O access token expira em uma hora; o login também retorna refresh token de uso único com validade de sete dias. Usuário desativado perde acesso mesmo com token ainda válido. Não há cadastro público.

Em produção, publique atrás de HTTPS, configure segredos fora do repositório, use usuário de banco com permissões limitadas e certificado válido (remova `TrustServerCertificate=true`). Migrations são executadas explicitamente antes da aplicação.

## Funcionalidades e rotas

- `POST /api/auth/login`, `GET /api/auth/me`.
- Admin: `GET/POST /api/users`; `PATCH /api/users/{id}/active?active=false` desativa um usuário.
- Clientes: `GET/POST /api/customers`, `PUT/DELETE /api/customers/{id}`. Escrita exclusiva do Admin; exclusão lógica preserva os vínculos com OS existentes.
- Serviços e peças: `GET/POST /api/catalog`, `PUT/DELETE /api/catalog/{id}`. Escrita exclusiva do Admin; exclusão lógica preserva os itens já utilizados.
- OS: `GET/POST /api/orders`, `GET /api/orders/{id}`. Apenas Admin cria OS e atribui um técnico ativo.
- `PATCH /api/orders/{id}/status`, corpo `{"status":"EmAndamento"}`.
- `POST /api/orders/{id}/items`, corpo `{"catalogItemId":"GUID","quantity":2}`; `DELETE /api/orders/{id}/items/{itemId}`.
- `GET /api/orders/{id}/history`: ator, ação, detalhes e horário UTC.
- `GET /api/orders/{id}/summary`: download do resumo em TXT.
- `GET /api/orders/{id}/pdf`: download do comprovante PDF, com paginação automática e fontes incluídas no backend. Webhook não implementado; foi escolhida a alternativa PDF.
- Admin: `GET /api/reports/monthly?year=2026&month=9`: quantidade criada e receita de OS concluídas no mês (UTC).
- `GET /health`: prontidão com conexão real ao SQL Server; retorna 200 quando saudável e 503 em falhas.
- `GET /health/live`: disponibilidade do processo, independente do banco.

Listagens de clientes, catálogo e OS aceitam `page` (padrão 1) e `pageSize` (padrão 20, máximo 100). OS também aceita `customerId`, `search` (nome do cliente), `status`, `from` (inclusivo), `to` (exclusivo), com datas ISO 8601. Retorno contém `total`, `page`, `pageSize` e `data`.

## Regras

Fluxo: Aberta → EmAndamento → Concluida. EmAndamento pode ir para AguardandoPeca e voltar. Qualquer estado aberto pode ir para Cancelada. Concluida e Cancelada são finais, sem alterações de itens ou status.

Admin acessa todas as OS. Técnico acessa somente suas OS e os clientes relacionados, consulta o catálogo e altera itens/status. Não gerencia usuários, clientes, catálogo ou relatórios.

Mão de obra é um item de catálogo do tipo `Servico`; materiais são `Peca`. Total sempre calculado no servidor com valores decimais. Cada item preserva nome, tipo e preço do momento da inclusão, mesmo se o catálogo mudar. A alteração de quantidade preserva o ID e o preço original do item. Remover e adicionar novamente usa o preço atual do catálogo.

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

## Sessões, senha e edição de usuários

- `POST /api/auth/refresh`: público, corpo `{"refreshToken":"token retornado no login"}`. Retorna um novo par access/refresh; o refresh anterior deixa de valer. Token inválido, vencido, consumido ou revogado retorna 401. Duas renovações simultâneas permitem apenas uma confirmação; a outra recebe 409.
- `POST /api/auth/change-password`: autenticado, corpo `{"currentPassword":"senha atual","newPassword":"nova senha com pelo menos 12 caracteres"}`. Retorna 204 e invalida todos os access/refresh tokens anteriores. Faça login novamente com a nova senha. Senha atual incorreta retorna 400.
- `PUT /api/users/{id}`: Admin, corpo com `name`, `email` e `role` (`Admin` ou `Tecnico`). Mudanças de e-mail ou perfil invalidam as sessões anteriores. Alterar somente o nome não exige novo login.

O próprio administrador não pode remover seu perfil Admin. Para mudar o perfil de um técnico com OS abertas, reatribua essas ordens primeiro. Desativar e reativar um usuário não restaura tokens antigos.

Refresh tokens são gerados com 32 bytes aleatórios e persistidos apenas como SHA-256. A tabela `RefreshSessions` usa rowversion para impedir consumo duplicado. A versão de segurança e a rowversion de `Users` protegem alterações simultâneas e revogação de sessões. A migration `AddRefreshSessionsAndUserSecurity` adiciona esses campos e a tabela. Tokens emitidos antes desta mudança precisam ser substituídos por novo login.

O limite padrão de login/refresh continua sendo dez requisições por minuto por IP. Pode ser configurado por `RateLimiting:LoginPermitLimit` (1 a 1000); os testes usam 100 para exercitar os fluxos, além de um teste específico do limitador.

## Reatribuição e quantidade

- `PATCH /api/orders/{id}/technician`: Admin, corpo `{"technicianId":"GUID"}`. Aceita somente técnico ativo e OS não encerrada. O técnico anterior perde o acesso à ordem; o novo técnico passa a poder consultá-la e atualizar seus itens/status.
- `PATCH /api/orders/{id}/items/{itemId}/quantity`: Admin ou técnico atribuído, corpo `{"quantity":3}`. Quantidade entre 1 e 10000. Preserva o preço histórico e recalcula o total no backend.

As duas operações registram ator, data/hora e valores anterior/novo no histórico. OS concluídas ou canceladas não permitem essas alterações.

## Comprovante PDF

O endpoint `/api/orders/{id}/pdf` segue as mesmas permissões da consulta da OS. O arquivo contém cliente, descrição, datas em UTC, status, serviços/peças, quantidades, preços históricos, subtotais e total. Descrições e nomes longos quebram linha; os itens paginam automaticamente com cabeçalho e número de página.

O serviço `IOrderPdfService` consulta o DTO autorizado e delega a geração a `IOrderPdfRenderer`. A infraestrutura usa [PDFsharp](https://docs.pdfsharp.net/General/Overview/Overview.html) e fontes Bitstream Vera embutidas, com licença em `src/Os.Api/Resources/Fonts/bitstream-vera-license.txt`. Nenhuma instalação de fonte do sistema ou serviço externo é necessária.

Os testes adicionais validam rotação concorrente, expiração e revogação de tokens, troca de senha, edição de usuários, transferência de acesso entre técnicos, preços históricos e download/paginação do PDF. O documento de teste com 45 itens foi renderizado e conferido visualmente.

### Exclusão lógica

Clientes, catálogo e itens da OS possuem `DeletedAt` (UTC). Os endpoints DELETE marcam a data e retornam 204; repetir a exclusão retorna 404. Clientes e catálogo excluídos não aparecem nas listagens operacionais nem podem ser editados ou usados em novas inclusões. As consultas filtram esses cadastros nos repositories, preservando a navegação das OS antigas para seus dados históricos.

Itens removidos continuam no banco com seus preços e quantidades originais e registro de auditoria, mas ficam fora dos DTOs, comprovantes, totais e receita mensal. Uma OS encerrada continua bloqueada para alterações. Usuários mantêm a desativação por `Active`, com revogação de tokens; OS mantêm o cancelamento por status, sem apagar histórico.

A migration `AddSoftDeletion` adiciona três colunas nullable sem apagar registros existentes. O teste de integração cobre exclusão de cadastros vinculados, permissões, bloqueio de reutilização, preservação da OS/PDF, remoção de itens e cálculo da receita.

## Logout

`POST /api/auth/logout` exige JWT e não recebe corpo. Retorna 204 e revoga todas as sessões do usuário autenticado, em todos os dispositivos: access tokens e refresh tokens anteriores passam a retornar 401. Outros usuários não são afetados. O frontend deve apagar os tokens locais após sucesso e voltar ao login. Uma requisição que já passou pela autenticação antes da revogação pode concluir normalmente. Repetir a chamada com o token revogado retorna 401. Não é necessária migration adicional.

## Logs e monitoramento

Os logs do backend são JSON no console, com timestamp UTC. Cada requisição gera método, template da rota, status, duração e `TraceId`. O identificador também aparece no header `X-Trace-Id` (exposto pelo CORS) e nos erros tratados pelo middleware. Corpos, senhas, tokens, query strings e caminhos brutos não são incluídos no log de requisições. Scopes automáticos do framework estão desabilitados no console para evitar caminhos brutos. Erros inesperados registram o tipo e o trace, sem serializar mensagens SQL ou dados. Os níveis podem ser configurados em `Logging:LogLevel`; o padrão restringe logs do framework e do EF para evitar detalhes de consultas. A configuração usa o [logging nativo do ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-8.0).

- `/health/live`: confirma que a API responde.
- `/health`: verifica também SQL Server; retorna 503 quando indisponível.
- `/metrics`: exige JWT de Admin e expõe contadores por classe de status e histograma de duração no formato Prometheus. Não inclui identificadores de usuários/clientes. Os contadores pertencem à instância e reiniciam com o processo.

```powershell
.\scripts\Test-Health.ps1 -BaseUrl http://localhost:5000 -MaxLatencyMs 2000
```

A checagem emite JSON e termina com código 1 se a API/banco falhar ou ultrapassar a latência máxima, permitindo integração com um executor externo de monitoramento. Não instala um agendamento nem um serviço de alertas. Um coletor de métricas deve enviar JWT de Admin válido e cuidar de sua renovação (o access token expira em uma hora). Para produção, encaminhe stdout a um coletor com retenção e configure alertas de indisponibilidade, taxa de 5xx e latência. O endpoint de métricas também é contado como requisição.

## Backup e teste de restauração

A ferramenta `tools/Os.DatabaseTools` usa `IBackupService`/`SqlBackupService`, conexão por variável de ambiente e caminhos de servidor SQL Server Linux. Os scripts são destinados ao serviço `database` do Docker Compose local e usam a conexão de `.env` quando a variável não estiver definida. O login SQL precisa de permissão para backup, criação/restauração e remoção do banco temporário.

Com o Docker disponível:

```powershell
.\scripts\Update-Database.ps1 -StartDatabase
.\scripts\Test-Integration.ps1
.\scripts\Backup-Database.ps1
```

O backup usa `COPY_ONLY, CHECKSUM`, copia o `.bak` para `backups/`, reenvia essa cópia ao SQL Server e testa a restauração. A ferramenta executa `RESTORE VERIFYONLY WITH CHECKSUM`, lê os arquivos lógicos, restaura em `OsRestore_<GUID>` usando arquivos físicos exclusivos, executa `DBCC CHECKDB` e consulta as tabelas da aplicação/migrations. O banco de teste é removido ao terminar. Não usa `WITH REPLACE` nem restaura sobre o banco original. A [verificação de mídia](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql?view=sql-server-ver17) é acompanhada de restauração real para testar a recuperabilidade.

O arquivo `.bak.json` registra SHA-256, data da validação e contagem de registros por tabela. O hash serve como referência para comparações posteriores; não substitui autenticação/assinatura do arquivo. Para testar novamente um backup salvo:

```powershell
.\scripts\Test-Restore.ps1 -BackupPath .\backups\osbackup-<identificador>.bak
```

Backups e seus relatórios ficam fora do Git. As cópias `.bak` no host e no volume do SQL Server são mantidas, inclusive as dos testes; os scripts não aplicam retenção automática. Defina retenção e cópia protegida fora da máquina conforme o ambiente. O backup contém dados de clientes e hashes de credenciais, portanto restrinja o acesso e use armazenamento criptografado em produção. Uma cópia no mesmo disco não protege contra perda desse disco. Os scripts não configuram agendamento. Para recuperação operacional, o teste gera e remove um banco temporário; uma restauração definitiva deve ser planejada separadamente.

O GitHub Actions compila a ferramenta, valida sintaxe dos scripts e inclui os testes de logout, métricas e backup/restauração do SQL Server. Enquanto o Docker local estiver fechado, a migration pendente e os testes de integração desta etapa precisam aguardar sua inicialização. Publicar as alterações e confirmar o workflow do novo commit também é necessário; o resultado de um commit anterior não valida esta etapa.


## Frontend

O frontend em `frontend/` inclui autenticação, painel, clientes, catálogo, usuários, ordens, itens, histórico e comprovantes. Usa serviços com interfaces, cliente HTTP centralizado, validações Zod, TanStack Query e toasts Sonner. Consulte [a documentação do frontend](frontend/README.md).

```powershell
cd frontend
npm ci
npm run dev
```

Abra `http://localhost:5173` com a API disponível em `http://localhost:5080`. Para outra API, configure `VITE_API_URL`. Não há dados de demonstração no aplicativo. O workflow de frontend valida formatação, lint, testes e build separadamente do backend.
