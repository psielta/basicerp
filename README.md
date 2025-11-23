# Basic ERP - Sistema Multi-Tenant

Sistema ERP basico multi-tenant desenvolvido com ASP.NET MVC 5 e Entity Framework Core 3.1 utilizando PostgreSQL como banco de dados.

## Sobre o Projeto

Este projeto de portfolio demonstra uma base SaaS multi-tenant pronta para evoluir, com modelagem profissional para organizations (tenants), usuarios globais, memberships, contas de autenticacao e sessoes.

### Funcionalidades Principais

- Arquitetura multi-tenant baseada em organizations/memberships
- Usuarios globais com roles, metadata em JSONB e suporte a 2FA
- Contas OAuth/credenciais, tokens e sessoes com IP/user-agent
- Docker Compose com PostgreSQL e PgAdmin
- Base pronta para modulos de clientes, produtos e vendas

## Tecnologias Utilizadas

- **Backend**: ASP.NET MVC 5 (.NET Framework 4.7.2)
- **ORM**: Entity Framework Core 3.1.32
- **Banco**: PostgreSQL 15 + extensoes `uuid-ossp` e `citext`
- **Provider**: Npgsql 4.1.9
- **Autenticação**: OWIN Cookie Authentication + BCrypt.NET
- **Email**: MailKit + MailHog (desenvolvimento)
- **Storage**: MinIO para armazenamento de arquivos (fotos de perfil)
- **Containerizacao**: Docker & Docker Compose
- **UI**: Bootstrap 5.3.3 + Bootstrap Icons

## Estrutura do Projeto

```
BasicERP/
├── WebApplicationBasic/         # Projeto web ASP.NET MVC
│   ├── Controllers/             # Controllers MVC (Base, Auth, Home, Account)
│   ├── Views/                   # Views Razor
│   │   ├── Auth/               # Telas de autenticação
│   │   ├── Account/            # Telas de gerenciamento de conta
│   │   └── Shared/             # Layout e parciais
│   ├── Services/               # Serviços de autenticação, email, sessão, storage
│   ├── Filters/                # Atributos de autorização customizados
│   ├── Models/ViewModels/      # ViewModels para formulários
│   ├── Data/                   # SeedData para testes
│   ├── App_Start/              # Configuracoes (DI, Routes, OWIN)
│   └── Web.config              # Configuracoes do app
├── EntityFrameworkProject/     # Projeto de dados
│   ├── Models/                 # Entidades (organization, user, etc.)
│   ├── Data/                   # DbContext e factory
│   └── app.config              # Connection strings
├── docker-compose.yml          # PostgreSQL + PgAdmin + MailHog + Redis + RabbitMQ + MinIO
├── AUTHENTICATION_TEST.md      # Instruções detalhadas de teste
└── README.md
```

## Configuracao do Ambiente

### Pre-requisitos

- Visual Studio 2019 ou superior
- .NET Framework 4.7.2
- Docker Desktop
- Git

### Passo 1: Clonar o Repositorio

```bash
git clone https://github.com/psielta/basicerp.git
cd basicerp
```

### Passo 2: Subir o PostgreSQL com Docker

```bash
docker-compose up -d
```

Servicos disponiveis:
- **PostgreSQL** na porta `5432`
  - Database: `basic_db`
  - Usuario: `adm`
  - Senha: `156879`
- **PgAdmin** na porta `5050`
  - Email: `admin@basicerp.com`
  - Senha: `admin`
- **MailHog** na porta `8025` (Web UI) e `1025` (SMTP)
  - Interface: http://localhost:8025
- **Redis** na porta `6379`
- **RabbitMQ** na porta `15672` (Management) e `5672` (AMQP)
  - Interface: http://localhost:15672
  - Usuario/Senha: guest/guest
- **MinIO** na porta `9000` (API) e `9001` (Console)
  - Console: http://localhost:9001
  - API: http://localhost:9000
  - Usuario/Senha: minioadmin/minioadmin

### Passo 3: Restaurar Pacotes NuGet

No Visual Studio abra `WebApplicationBasic.sln` e execute:

```
Tools > NuGet Package Manager > Package Manager Console
Update-Package -Reinstall
```

### Passo 4: Executar Migrations

No **Package Manager Console**:
- **Default project**: `EntityFrameworkProject`
- **Startup project**: `WebApplicationBasic`

```powershell
Update-Database
```

Isso cria as tabelas:
- `organization`
- `"user"`
- `memberships`
- `account`
- `session`

### Passo 5: Executar o Projeto

Pressione **F5** no Visual Studio ou clique em **Start**. A aplicacao roda em `https://localhost:44318/`.

## Testando o Sistema de Autenticação

### Usuários de Teste (Criados automaticamente)

1. **Admin User**
   - Email: `admin@example.com`
   - Senha: `admin123`
   - Organização: Empresa Exemplo (Owner)

2. **João Silva**
   - Email: `joao@example.com`
   - Senha: `senha123`
   - Organização: Empresa Exemplo (Member)

3. **Maria Santos** (Múltiplas organizações)
   - Email: `maria@example.com`
   - Senha: `maria123`
   - Organizações: Empresa Exemplo (Admin), Startup Tech (Owner)

### Como Testar

1. **Login com Senha**: Digite o email → Escolha a organização (se aplicável) → Escolha "Login com Senha" → Digite a senha

2. **Login com OTP**: Digite o email → Escolha a organização → Escolha "Login por Código (OTP)" → Verifique o email no MailHog (http://localhost:8025) → Digite o código de 6 dígitos

Para instruções mais detalhadas, consulte o arquivo [AUTHENTICATION_TEST.md](AUTHENTICATION_TEST.md).

## Modelo de Dados

### Organization (tenant)
- `id` UUID com `uuid_generate_v4()`
- `name`, `slug` (citext unico), `logo`
- `metadata` JSONB para dados dinamicos
- `created_at` / `updated_at` com `now()`

### User (global)
- `id`, `name`, `email` (citext unico)
- `email_verified`, `role` (default `user`), `image`
- `metadata` JSONB, `two_factor_enabled`
- `created_at`, `updated_at`

### Membership
- `organization_id`, `user_id`, `role`, `team_id`
- `created_at`, `updated_at`
- Constraints: FK cascata, indice unico `(organization_id, user_id)` e indices auxiliares por FK

### Account
- `provider_id`, `account_id` (par unico)
- `user_id` (FK), tokens de acesso/refresh/id + expiracoes
- `scope`, `password` (para credenciais locais)
- `reset_token`, `reset_token_expires` (recuperação de senha)
- `last_password_change` (auditoria)
- `created_at`, `updated_at`

### Session
- `token` unico, `expires_at`, `created_at`, `updated_at`
- `ip_address` (inet) e `user_agent`
- `user_id` (obrigatorio) e `active_organization_id` (nullable, `ON DELETE SET NULL`)

### Produtos (SPU/SKU)

**ProductTemplate** (SPU - Standard Product Unit):
- Produto "pai" com informações gerais
- `name`, `slug`, `brand`, `description`
- Flags: `is_service`, `is_rental`, `has_delivery`
- Fiscais: `ncm`, `nbs`, `warranty_months`
- Relacionamentos: variantes, categorias, atributos descritivos

**ProductVariant** (SKU - Stock Keeping Unit):
- Variação específica do produto
- `sku` único por organização
- Dimensões: `weight`, `height`, `width`, `length`
- `cost`, `barcode`, `is_active`
- Relacionamentos: atributos de variação

**ProductAttribute**:
- Define tipos de atributos (Cor, Tamanho, Material, etc.)
- `is_variant`: `true` para atributos de variação, `false` para descritivos

**Category**:
- Hierarquia de categorias com `parent_id`
- `path` para navegação breadcrumb

## Comandos Uteis

### Docker

```bash
docker-compose up -d      # Subir containers
docker-compose down       # Parar containers
docker-compose down -v    # Remover containers e volumes

docker-compose down -v # Parar, remover containers E volumes
docker logs basicerp-postgres
docker exec -it basicerp-postgres psql -U adm -d basic_db
```

### Entity Framework Migrations

```powershell
Add-Migration NomeDaMigration
Update-Database
Update-Database -Migration NomeAnterior
Get-Migration
```

### PostgreSQL (dentro do container)

```sql
-- Listar tabelas
\dt

-- Ver estrutura
\d organization
\d "user"
\d memberships

-- Consultar dados
SELECT * FROM organization;
SELECT * FROM "user";
SELECT * FROM memberships;
SELECT * FROM account;
SELECT * FROM session;
```

## Roadmap

### Concluido
- [x] Configuracao EF Core + PostgreSQL
- [x] Docker Compose com PostgreSQL, PgAdmin, MailHog, Redis e RabbitMQ
- [x] Modelo de dados multi-tenant (organization, user, memberships, account, session)
- [x] Injecao de dependencia do DbContext e Serviços
- [x] Interface basica exibindo contagens do banco
- [x] **Sistema de Autenticacao Completo**
  - [x] Login com email e senha
  - [x] Login com OTP (One-Time Password) via email
  - [x] Suporte a múltiplas organizações
  - [x] Hash de senhas com BCrypt (work factor 12)
  - [x] Sessões gerenciadas no banco de dados
  - [x] Cookies de autenticação OWIN criptografados
  - [x] Proteção de rotas com atributos customizados
  - [x] BaseController com contexto do usuário
  - [x] Seed data para testes
  - [x] **Recuperação de Senha**
    - [x] Esqueceu a senha com envio de email
    - [x] Token seguro de reset (24 horas)
    - [x] Página de redefinição de senha
    - [x] Email de confirmação após alteração
  - [x] **Gerenciamento de Conta ("Minha Conta")**
    - [x] Perfil do usuário com informações completas
    - [x] Edição de perfil (nome, email, foto)
    - [x] Alteração de senha com validação
    - [x] Gerenciamento de sessões ativas
    - [x] Visualização e troca de organizações
    - [x] Indicador de força de senha
    - [x] Preview em tempo real na edição
  - [x] **Upload de Fotos de Perfil**
    - [x] Integração com MinIO para storage
    - [x] Interface IStorageService implementada
    - [x] Upload de arquivos na edição de perfil
    - [x] Validação de tipos de arquivo (jpg, png)
    - [x] Redimensionamento e otimização de imagens (max 800x800, 85% qualidade)

  - [x] **Sistema de Produtos Completo (SPU/SKU)**
    - [x] Cadastro de produtos simples e com variações
    - [x] Wizard de criação com 5 passos
    - [x] Wizard de edição completo
    - [x] Categorias hierárquicas
    - [x] Sistema de atributos (variação + descritivos)
    - [x] Geração automática de variantes (produto cartesiano)
    - [x] Gerenciamento individual de variantes
    - [x] Soft deletes e auditoria completa
  - [x] **Dashboard Executivo**
    - [x] Cards de estatísticas por organização
    - [x] Gráficos Chart.js (produtos por tipo, status de variantes)
    - [x] Lista de produtos recentes
    - [x] Ações rápidas

### Em Desenvolvimento

- [ ] **Gestão de Usuários**
  - CRUD completo de usuários
  - Definição de roles
  - Associação usuário-organization via memberships

- [ ] **Camada de Clientes**
  - CRUD de clientes por organization
  - Validação de CPF/CNPJ
  - Busca, filtros e paginação

- [ ] **Preços e Estoque**
  - Tabela de preços por variante
  - Controle de estoque por SKU
  - Histórico de movimentações

- [ ] **Sistema de Vendas**
  - Pedidos/vendas e itens
  - Cálculo de totais
  - Histórico por cliente e relatórios básicos

### Futuro

- [ ] Relatórios (PDF/Excel)
- [ ] API REST para integração
- [ ] Migração para .NET 8
- [ ] Testes unitários/integrados
- [ ] CI/CD com GitHub Actions

## Modulo de Estoque

O sistema possui um modulo de estoque baseado em eventos, multi-tenant e focado em SKUs (`product_variant`), com as seguintes pecas principais:

- **Locais de Estoque (`stock_location`)**
  - Cada organizacao pode ter varios locais (almoxarifados, lojas, locais virtuais).
  - Campos principais: `code`, `name`, `is_default`, `is_virtual`, `is_active`.
  - Tela em **Estoque > Locais** para criar/editar locais.

- **Saldo de Estoque (`stock_balance`)**
  - Projecao atual de saldo por `organization_id` + `location_id` + `variant_id`.
  - Campos: `on_hand` (fisico), `reserved` (reservado), `last_movement_at`.
  - Constraint garante que `on_hand >= reserved` e nunca negativo.

- **Livro-Razao (`stock_ledger`)**
  - Cada movimentacao gera um evento com `delta_on_hand` e `delta_reserved`.
  - Tipos de movimento (enum `StockMovementType`):
    - `StockReceived` (entrada)
    - `StockReserved` (reserva)
    - `StockReleased` (liberacao de reserva)
    - `StockShipped` (saida/baixa)
    - `StockAdjusted` (ajuste de inventario)
    - `StockTransferredOut` / `StockTransferredIn` (transferencia entre locais)
  - Cada linha tem `deduplication_key` para idempotencia por organizacao.

- **Reservas (`stock_reservation`)**
  - Representa reservas explicitas de estoque por SKU/local.
  - Campos principais: `quantity`, `status` (Active/Cancelled/Consumed), `reserved_at`, `expires_at`.
  - Hoje a UI cria reservas manuais; no futuro, a ideia e vincular a pedidos/empenhos.

- **Outbox de Eventos (`stock_outbox_event`)**
  - Implementa o Outbox Pattern para integracao assincrona (Kafka).
  - Campos: `aggregate_type`, `aggregate_id`, `event_type`, `payload` (JSON), `status`.
  - Toda movimentacao grava na outbox dentro da mesma transacao do ledger/saldo.

- **Camada de Servico (`StockService`)**
  - Expe operacoes de dominio:
    - `ReceiveAsync` (entrada manual)
    - `AdjustAsync` (ajuste para novo saldo fisico)
    - `TransferAsync` (entre locais)
    - `ReserveAsync` / `ReleaseReservationAsync`
    - `ShipAsync` (baixa, opcionalmente consumindo reserva)
    - Consultas: saldos, livro-razão recente, reservas ativas.
  - Garante:
    - Idempotencia via `deduplication_key` gerada no backend.
    - Consistencia entre `stock_balance`, `stock_ledger` e `stock_reservation`.
    - Validacao de tenant: SKU e local precisam pertencer a mesma organizacao.
    - Gera automaticamente `source_type = "AJUSTE MANUAL"` e um `source_id` (GUID) para
      todas as movimentacoes feitas pela UI.

- **UI de Estoque**
  - **Dashboard de Estoque** (`/Stock/Index`):
    - Lista saldos por SKU/local (fisico, reservado, disponivel).
    - Lista reservas ativas e permite liberacao manual.
    - Lista ultimas movimentacoes (livro-razão).
  - **Movimentacoes Manuais** (`/Stock/Receive`, `/Stock/Adjust`, `/Stock/Transfer`, `/Stock/Reserve`, `/Stock/Ship`):
    - Usuario seleciona SKU, local (e local destino, quando aplicavel), quantidade e um motivo opcional.
    - O sistema gera internamente a fonte, o identificador e a chave de idempotencia.
  - **Locais de Estoque** (`/Stock/Locations`):
    - Cadastro e edicao de locais, inclusive marcacao de local padrao.

### Roadmap do Modulo de Estoque

Proximos passos planejados para evoluir o modulo:

- **Outbox Relay para Kafka**
  - Criar um processo/worker (console app ou Windows Service) que:
    - Leia registros de `stock_outbox_event` com `status = 0` (pendente),
    - Publique os eventos no Kafka usando `Confluent.Kafka`,
    - Atualize `status` para `1` (publicado) ou `2` (erro), registrando `error` e `published_at`.
  - Permitir configuracao de topico/chave por tipo de evento (ex.: `stock-events`, `stock-reservations`).

- **Worker de Consumo (Kafka -> Outros Contextos)**
  - Implementar um worker separado que consuma eventos de estoque para:
    - Manter projecoes em outros servicos (e-commerce, PNCP, relatorios, etc.).
    - Atualizar caches e indices de busca dependentes de disponibilidade.

- **Integracao com Modulos de Negocio**
  - Substituir gradualmente movimentacoes manuais por integracoes:
    - Reservas disparadas por pedidos/vendas.
    - Entradas disparadas por compras/recebimento/NFe.
    - Baixas disparadas por faturamento/expedicao.

## Notas Tecnicas

### Injecao de Dependencia

`Microsoft.Extensions.DependencyInjection` gerencia as dependencias:

- `ApplicationDbContext`: Transient (evita problemas de disposed)
- `IPasswordHasher`, `IEmailService`, `ISessionService`: Transient
- `IAuthenticationService`: Transient
- `IStorageService` (MinIOStorageService): Transient
- `IImageProcessingService`: Transient
- Controllers: Transient

### Binding Redirects

Como o projeto usa .NET Framework + EF Core, os binding redirects definidos no `Web.config` evitam conflitos de versao.

### Multi-Tenancy

A separacao de dados acontece via `memberships`: cada usuario pode pertencer a varias organizations, e os relacionamentos trazem `organization_id`/`user_id` em todas as tabelas dependentes. Um filtro global de tenant podera ser adicionado futuramente no `DbContext`.

### Storage com MinIO

O sistema utiliza MinIO para armazenamento de arquivos, especialmente fotos de perfil dos usuários:

- **Bucket**: `user-profiles` (criado automaticamente se não existir)
- **Política**: Configurada para leitura pública das imagens
- **Serviço**: `IStorageService` abstrai operações de upload/download/delete
- **Implementação**: `MinIOStorageService` usando SDK oficial do MinIO

### Processamento de Imagens

Sistema completo de processamento de imagens antes do upload:

- **Redimensionamento automático**: Máximo 800x800 pixels mantendo proporção
- **Otimização**: Compressão JPEG em 85% de qualidade
- **Validação**: Aceita apenas JPG, JPEG e PNG
- **Limite de tamanho**: 5MB (configurável no Web.config)
- **Configurações personalizáveis** no Web.config:
  - `Image:MaxWidth`: Largura máxima (padrão 800)
  - `Image:MaxHeight`: Altura máxima (padrão 800)
  - `Image:Quality`: Qualidade JPEG 0-100 (padrão 85)
  - `Image:MaxFileSize`: Tamanho máximo em bytes (padrão 5242880)

## Contribuindo

Projeto de portfolio pessoal, mas feedbacks sao bem-vindos! Abra uma issue ou envie um PR.

## Licenca

Projeto sob licenca MIT.

## Autor

Mateus Salgueiro  
- GitHub: [@psielta](https://github.com/psielta)  
- LinkedIn: [Mateus Salgueiro](https://www.linkedin.com/in/mateus-salgueiro-525717205/)

---

Desenvolvido como parte do meu portfolio de desenvolvimento web com .NET.
