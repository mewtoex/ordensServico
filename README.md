# Gestão de Ordens de Serviço

Aplicação de exemplo para gerenciar atendimentos de uma assistência técnica: cadastrar clientes, abrir ordens de serviço, atribuir técnicos, registrar mão de obra e peças e emitir comprovantes. O projeto demonstra um fluxo completo entre React, API REST e SQL Server, com regras de negócio e controle de acesso no backend.

## Funcionalidades

- **Autenticação:** JWT, renovação de token, troca de senha e logout com revogação de sessões.
- **Perfis:** Admin gerencia cadastros, usuários, atribuições e relatórios; Técnico consulta suas OS e atualiza itens e status.
- **Ordens de serviço:** filtros por cliente, período e status, paginação, reatribuição de técnico e histórico de alterações.
- **Valores:** mão de obra e peças com quantidades editáveis, preços históricos e total calculado pela API.
- **Preservação do histórico:** exclusão lógica de clientes, catálogo e itens; desativação de usuários e cancelamento de OS.
- **Comprovantes e painel:** downloads em PDF/TXT e indicadores mensais para administradores.
- **Interface:** formulários validados, estados de carregamento e notificações de sucesso e erro com shadcn/ui e Sonner.
- **Operação:** health checks, logs JSON, métricas e scripts de backup com teste de restauração.

## Exemplo de uso

1. O administrador cadastra um cliente, os serviços e as peças disponíveis.
2. Abre uma OS descrevendo o atendimento e atribui um técnico ativo.
3. O técnico inicia o trabalho, adiciona mão de obra e peças e ajusta as quantidades.
4. Se faltar material, marca a OS como **Aguardando peça** e retoma o atendimento quando disponível.
5. Ao concluir, o sistema mantém os valores e o histórico e permite baixar o comprovante.

O fluxo principal é **Aberta → Em andamento → Concluída**. Uma OS em andamento pode aguardar peça e retornar ao atendimento. Ordens abertas podem ser canceladas; concluídas e canceladas não permitem novas alterações. Cada técnico acessa apenas as ordens atribuídas a ele.

## Tecnologias e arquitetura

**Backend:** ASP.NET Core 8, Entity Framework Core, SQL Server e autenticação JWT. Organização em Domain, Application e Infra dentro de um projeto, com DTOs, services e repositories com interfaces e responsabilidade única. Os controllers recebem services; a aplicação depende de contratos de persistência, sem acoplamento ao EF Core.

**Frontend:** React, TypeScript, Vite, Tailwind CSS, shadcn/ui, Sonner, TanStack Query e Zod. Organização por funcionalidade, serviços com interfaces e cliente HTTP centralizado. A API permanece responsável pelas permissões, regras e cálculos.

```text
.
├── backend/
│   ├── src/Os.Api/            # API, domínio, aplicação e infraestrutura
│   ├── tests/                # Testes unitários e integração com SQL Server
│   ├── tools/Os.DatabaseTools/ # Backup e validação de restauração
│   ├── scripts/              # Migrations, testes, saúde e backups
│   ├── compose.yaml          # SQL Server local e volume persistente
│   ├── ServiceOrders.sln
│   ├── global.json           # SDK .NET
│   ├── NuGet.Config
│   ├── .env.example
│   └── README.md             # API, regras, configuração e operação
├── frontend/
│   ├── src/                  # Telas, serviços, componentes e testes
│   ├── .env.example
│   └── README.md             # Execução e organização da interface
├── .github/workflows/        # CI independente para backend e frontend
├── .editorconfig             # Formatação compartilhada
└── .gitignore
```

## Executar localmente

Requisitos: **SDK .NET 8.0.4xx**, ferramenta **dotnet-ef 8**, **Node.js 24**, npm e **SQL Server 2022** (local ou Docker Desktop com o engine iniciado). Os comandos abaixo usam PowerShell e partem da raiz do repositório.

### 1. Backend e banco

```powershell
cd backend
dotnet restore ServiceOrders.sln --configfile NuGet.Config
Copy-Item .env.example .env
```

Edite `backend/.env`: defina uma senha forte em `DB_PASSWORD`, repita essa senha na string de conexão e configure uma chave JWT aleatória de pelo menos 32 caracteres. Se já tiver `.env`, preserve seus valores. O arquivo é ignorado pelo Git.

```powershell
docker compose up -d
# Aguarde o SQL Server aceitar conexões.
.\scripts\Update-Database.ps1
```

O script aplica as migrations e carrega as variáveis do `.env` na sessão PowerShell. Crie o primeiro administrador e inicie a API **nessa mesma sessão**:

```powershell
$env:Seed__Email = 'admin@example.com'
$env:Seed__Password = 'SubstituaPorSuaSenhaAdminForte123!'
dotnet run --project src/Os.Api -- --seed-admin
Remove-Item Env:Seed__Password
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/Os.Api -- --urls http://localhost:5080
```

A API não importa `.env` automaticamente. Em outra sessão, execute o script de migrations novamente ou configure as variáveis de ambiente. Não há cadastro público nem credenciais de demonstração.

- Swagger: [localhost:5080/swagger](http://localhost:5080/swagger). Faça login e use **Authorize** com o access token.
- Saúde da API e do banco: [localhost:5080/health](http://localhost:5080/health).
- Detalhes de configuração, endpoints, migrations e backups: [README do backend](backend/README.md).

### 2. Frontend

Em outro terminal, partindo da raiz:

```powershell
cd frontend
npm ci
Copy-Item .env.example .env.local
npm run dev
```

Acesse [localhost:5173](http://localhost:5173) e entre com o administrador criado. A API padrão é `http://localhost:5080`; altere `VITE_API_URL` em `.env.local` se necessário. O CORS de desenvolvimento já permite `http://localhost:5173`.

Consulte o [README do frontend](frontend/README.md) para detalhes de sessão, notificações, testes e publicação.

## Qualidade e testes

No diretório `backend/`:

```powershell
dotnet format ServiceOrders.sln --no-restore --verify-no-changes
dotnet build ServiceOrders.sln --configuration Release --no-restore
dotnet test tests/Os.Tests --configuration Release --no-build
.\scripts\Test-Integration.ps1 -NoBuild
```

Os testes de integração exigem SQL Server disponível e permissão para criar bancos temporários. Exercitam login, criação de OS, itens, conclusão, permissões, concorrência e restauração de backup. Cada execução usa um banco isolado. Os testes unitários usam mocks e InMemory para os cenários que não exigem SQL Server real.

No diretório `frontend/`:

```powershell
npm run format:check
npm run lint
npm run test
npm run build
```

Os testes da interface simulam respostas HTTP. Eles não substituem um teste ponta a ponta com navegador, API e banco reais.

Os workflows [Backend CI](.github/workflows/backend-ci.yml) e [Frontend CI](.github/workflows/frontend-ci.yml) verificam formatação, compilação e testes. O backend usa SQL Server descartável no GitHub Actions e publica os resultados TRX; o frontend publica o build estático.

## Escopo do exemplo

O foco é demonstrar um caso de uso completo de gestão de atendimentos. O comprovante usa PDF/TXT; envio por e-mail e webhook não faz parte deste MVP. Configurações locais, segredos, dependências, builds e backups ficam fora do Git. O Compose mantém o volume `ordemdeserviso_os-data` para preservar o banco existente após a mudança para `backend/`.
