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
- **Containerizacao**: Docker & Docker Compose
- **UI**: Bootstrap 5.3.3 + Bootstrap Icons

## Estrutura do Projeto

```
BasicERP/
├── WebApplicationBasic/         # Projeto web ASP.NET MVC
│   ├── Controllers/             # Controllers MVC (Base, Auth, Home)
│   ├── Views/                   # Views Razor
│   │   ├── Auth/               # Telas de autenticação
│   │   └── Shared/             # Layout e parciais
│   ├── Services/               # Serviços de autenticação, email, sessão
│   ├── Filters/                # Atributos de autorização customizados
│   ├── Models/ViewModels/      # ViewModels para formulários
│   ├── Data/                   # SeedData para testes
│   ├── App_Start/              # Configuracoes (DI, Routes, OWIN)
│   └── Web.config              # Configuracoes do app
├── EntityFrameworkProject/     # Projeto de dados
│   ├── Models/                 # Entidades (organization, user, etc.)
│   ├── Data/                   # DbContext e factory
│   └── app.config              # Connection strings
├── docker-compose.yml          # PostgreSQL + PgAdmin + MailHog + Redis + RabbitMQ
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
- `created_at`, `updated_at`

### Session
- `token` unico, `expires_at`, `created_at`, `updated_at`
- `ip_address` (inet) e `user_agent`
- `user_id` (obrigatorio) e `active_organization_id` (nullable, `ON DELETE SET NULL`)

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

### Em Desenvolvimento

- [ ] **Gestao de Usuarios**
  - CRUD completo de usuarios
  - Definicao de roles
  - Associacao usuario-organization via memberships

- [ ] **Camada de Clientes**
  - CRUD de clientes por organization
  - Validacao de CPF/CNPJ
  - Busca, filtros e paginacao

- [ ] **Cadastro de Produtos**
  - CRUD de produtos
  - Estoque e categorias
  - Precos e descontos

- [ ] **Sistema de Vendas**
  - Pedidos/vendas e itens
  - Calculo de totais
  - Historico por cliente e relatórios basicos

### Futuro

- [ ] Dashboard com graficos
- [ ] Relatorios (PDF/Excel)
- [ ] API REST para integracao
- [ ] Migracao para .NET 8
- [ ] Testes unitarios/integrados
- [ ] CI/CD com GitHub Actions

## Notas Tecnicas

### Injecao de Dependencia

`Microsoft.Extensions.DependencyInjection` gerencia as dependencias:

- `ApplicationDbContext`: Scoped (uma instancia por requisicao)
- Controllers: Transient

### Binding Redirects

Como o projeto usa .NET Framework + EF Core, os binding redirects definidos no `Web.config` evitam conflitos de versao.

### Multi-Tenancy

A separacao de dados acontece via `memberships`: cada usuario pode pertencer a varias organizations, e os relacionamentos trazem `organization_id`/`user_id` em todas as tabelas dependentes. Um filtro global de tenant podera ser adicionado futuramente no `DbContext`.

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
