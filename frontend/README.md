# Ordem · Frontend

React 19, TypeScript estrito, Vite, Tailwind CSS e componentes oficiais shadcn/ui (Radix). Sonner centraliza as notificações e TanStack Query gerencia consultas, cache e invalidação. A interface é em português e se adapta a desktop e celular.

## Executar

Requisitos: Node.js 24 e npm. Inicie o backend em `http://localhost:5080`, conforme a [documentação do backend](../backend/README.md).

```powershell
cd frontend
npm ci
Copy-Item .env.example .env.local
npm run dev
```

Acesse `http://localhost:5173`. O CORS de desenvolvimento do backend já permite essa origem. Configure `VITE_API_URL` em `.env.local` para outra API; a variável é pública e não deve conter credenciais. Use o usuário criado pelo seed do backend; não existe cadastro público ou usuário de demonstração.

## Organização

- `app/`: layout e navegação.
- `features/`: autenticação, painel, ordens, clientes, catálogo e usuários. Cada serviço tem uma interface própria e recebe `IHttpClient`.
- `components/ui/`: componentes shadcn/ui incorporados ao projeto.
- `components/shared/`: formulários, tabelas, paginação, estados e confirmações reutilizáveis.
- `lib/`: contratos da API, cliente HTTP, sessão, erros, notificações, validação e formatação.
- `hooks/`: comportamento compartilhado de ações assíncronas.
- `test/`: fixtures exclusivamente de teste e fluxos com respostas simuladas da API.

As telas não montam requisições HTTP diretamente. Os totais, preços históricos, autorização e regras definitivas permanecem no backend. A interface oferece somente as transições previstas e bloqueia edição de ordens encerradas. Clientes e itens são selecionados com paginação, sem limitar a seleção à primeira página da API.

## Funcionalidades

Login, renovação automática de token, logout global, troca de senha, painel de indicadores, lista e detalhe da OS, filtros por cliente/período/status, criação, reatribuição, itens e quantidades, histórico, PDF/TXT, clientes, catálogo e usuários. Técnico vê apenas ações compatíveis com seu perfil; a autorização efetiva continua na API.

Os indicadores mensais e filtros de data usam limites UTC, alinhados ao backend. A data final é convertida para o início do dia seguinte, exclusivo. Datas de cada atendimento são exibidas no fuso do navegador. Indicadores sem resposta exibem `—`, nunca um zero inventado.

## Sessão e feedback

Tokens ficam em `sessionStorage` por aba, com fallback em memória quando o armazenamento está bloqueado. Não são colocados na URL ou em logs. Isso não oferece a proteção de cookies HttpOnly contra XSS; uma migração para cookies exige alterar o contrato do backend. Uma única renovação é compartilhada entre requisições concorrentes. Refresh revogado encerra a sessão; indisponibilidade temporária permite nova tentativa. Login inválido, rede, 400/403/404/409/429/5xx e validações Problem Details têm feedback centralizado. Não há repetição automática de mutações.

Toasts informam sucessos de ações, downloads e erros. Listagens bem-sucedidas mostram os dados sem gerar toasts repetidos. Erros de campo também aparecem junto ao formulário. Operações em andamento desabilitam envio; exclusões e encerramentos pedem confirmação. Logout encerra as sessões em todos os dispositivos, conforme a API.

## Verificação

```powershell
npm run format:check
npm run lint
npm run test
npm run build
```

Vitest/Testing Library verificam autenticação, permissões visuais, cadastro, logout, validação, tratamento de erros e renovação concorrente. Esses testes simulam respostas HTTP e não substituem a validação ponta a ponta com API e SQL Server reais. O workflow `frontend-ci.yml` executa formatação, lint, testes e build.

## Publicação

`npm run build` gera `dist/`. Configure o servidor estático para retornar `index.html` nas rotas da SPA (por exemplo, `/orders/<id>`), use HTTPS e adicione a origem publicada ao CORS do backend. A URL da API é definida no build. Nenhum deploy ou agendamento foi criado nesta etapa.
