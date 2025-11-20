# Basic ERP - Sistema Multi-Tenant

Sistema ERP básico multi-tenant desenvolvido com ASP.NET MVC 5 e Entity Framework Core 3.1, utilizando PostgreSQL como banco de dados.

## 📋 Sobre o Projeto

Este é um projeto de portfólio que demonstra a implementação de um sistema ERP básico com arquitetura multi-tenant, permitindo que múltiplas empresas (tenants) utilizem o mesmo sistema com isolamento de dados.

### Funcionalidades Principais

- Arquitetura multi-tenant
- Gerenciamento de tenants (empresas)
- Sistema de usuários por tenant
- Cadastro de clientes
- Controle de vendas

## 🚀 Tecnologias Utilizadas

- **Backend**: ASP.NET MVC 5 (.NET Framework 4.7.2)
- **ORM**: Entity Framework Core 3.1.32
- **Banco de Dados**: PostgreSQL 15
- **Provider**: Npgsql 4.1.9
- **Containerização**: Docker & Docker Compose
- **UI**: Bootstrap 5.2.3

## 📁 Estrutura do Projeto

```
BasicERP/
├── WebApplicationBasic/          # Projeto web ASP.NET MVC
│   ├── Controllers/              # Controllers MVC
│   ├── Views/                    # Views Razor
│   ├── App_Start/                # Configurações (DI, Routes, etc)
│   └── Web.config                # Configurações do app web
├── EntityFrameworkProject/       # Projeto de dados
│   ├── Models/                   # Entidades do banco
│   ├── Data/                     # DbContext e configurações
│   ├── Migrations/               # Migrations do EF Core
│   └── app.config                # Configurações do projeto de dados
└── docker-compose.yml            # Configuração do PostgreSQL
```

## 🛠️ Configuração do Ambiente

### Pré-requisitos

- Visual Studio 2019 ou superior
- .NET Framework 4.7.2
- Docker Desktop
- Git

### Passo 1: Clonar o Repositório

```bash
git clone https://github.com/psielta/basicerp.git
cd reaproveitar
```

### Passo 2: Subir o PostgreSQL com Docker

```bash
docker-compose up -d
```

Isso irá iniciar:
- **PostgreSQL** na porta `5432`
  - Database: `basic_db`
  - Usuário: `adm`
  - Senha: `156879`
- **PgAdmin** na porta `5050`
  - Email: `admin@reaproveitar.com`
  - Senha: `admin`

### Passo 3: Restaurar Pacotes NuGet

Abra a solução `WebApplicationBasic.sln` no Visual Studio e restaure os pacotes:

```
Tools > NuGet Package Manager > Package Manager Console
```

Execute:

```powershell
Update-Package -reinstall
```

### Passo 4: Executar Migrations

No **Package Manager Console**, certifique-se de que:
- **Default project**: `EntityFrameworkProject`
- **Startup project**: `WebApplicationBasic` (em negrito no Solution Explorer)

Execute:

```powershell
Update-Database
```

Isso criará as tabelas no banco de dados:
- `tenants`
- `usuarios`
- `clientes`

### Passo 5: Executar o Projeto

Pressione **F5** no Visual Studio ou clique em **Start**.

A aplicação estará disponível em: `https://localhost:44318/`

## 🗄️ Modelo de Dados

### Tenant
Representa as empresas/organizações que utilizam o sistema.

- Id, Nome, CNPJ, Email, Telefone
- Ativo, DataCriacao, DataAtualizacao

### Usuario
Usuários do sistema vinculados a um tenant.

- Id, TenantId, Nome, Email, SenhaHash
- Role, Ativo, DataCriacao, DataAtualizacao, UltimoLogin

### Cliente
Clientes cadastrados por cada tenant.

- Id, TenantId, Nome, CPF/CNPJ
- Email, Telefone, Celular
- Endereço completo (CEP, Endereco, Numero, Complemento, Bairro, Cidade, Estado)
- Ativo, DataCriacao, DataAtualizacao

## 🔧 Comandos Úteis

### Docker

```bash
# Subir containers
docker-compose up -d

# Parar containers
docker-compose down

# Remover containers e volumes
docker-compose down -v

# Ver logs do PostgreSQL
docker logs reaproveitar-postgres

# Conectar ao PostgreSQL via CLI
docker exec -it reaproveitar-postgres psql -U adm -d basic_db
```

### Entity Framework Migrations

```powershell
# Criar nova migration
Add-Migration NomeDaMigration

# Aplicar migrations pendentes
Update-Database

# Reverter última migration
Update-Database -Migration NomeDaMigrationAnterior

# Ver lista de migrations
Get-Migration
```

### PostgreSQL (dentro do container)

```sql
-- Listar tabelas
\dt

-- Ver estrutura de uma tabela
\d tenants

-- Consultar dados
SELECT * FROM tenants;
SELECT * FROM usuarios;
SELECT * FROM clientes;
```

## 🗺️ Roadmap

### ✅ Concluído
- [x] Configuração do Entity Framework Core com PostgreSQL
- [x] Docker Compose com PostgreSQL e PgAdmin
- [x] Modelo de dados multi-tenant (Tenant, Usuario, Cliente)
- [x] Migrations configuradas
- [x] Injeção de dependência do DbContext
- [x] Interface básica mostrando dados do banco

### 🚧 Em Desenvolvimento

- [ ] **Sistema de Autenticação**
  - Implementar login de usuários
  - Autenticação baseada em cookies/session
  - Hash de senhas com BCrypt
  - Proteção de rotas com [Authorize]

- [ ] **Gestão de Usuários**
  - CRUD completo de usuários
  - Gerenciamento de roles/permissões
  - Associação usuário-tenant

- [ ] **Cadastro de Clientes**
  - CRUD completo de clientes
  - Validação de CPF/CNPJ
  - Busca e filtros
  - Paginação

- [ ] **Cadastro de Produtos**
  - CRUD de produtos
  - Controle de estoque
  - Categorias de produtos
  - Preços e descontos

- [ ] **Sistema de Vendas**
  - Criação de pedidos/vendas
  - Itens de venda
  - Cálculo de totais
  - Histórico de vendas por cliente
  - Relatórios básicos

### 🔮 Futuro

- [ ] Dashboard com gráficos e métricas
- [ ] Relatórios de vendas (PDF/Excel)
- [ ] API REST para integração
- [ ] Migração para .NET 8
- [ ] Testes unitários e de integração
- [ ] CI/CD com GitHub Actions

## 📝 Notas Técnicas

### Injeção de Dependência

O projeto utiliza `Microsoft.Extensions.DependencyInjection` para gerenciar dependências:

- **DbContext**: Scoped (uma instância por requisição HTTP)
- **Controllers**: Transient (criado quando necessário)

### Binding Redirects

Devido ao uso de .NET Framework com EF Core, são necessários binding redirects no `Web.config` para resolver conflitos de versões de assemblies.

### Multi-Tenancy

Todos os modelos possuem uma foreign key `TenantId`, garantindo isolamento de dados entre diferentes tenants. Futuramente, será implementado um filtro global no DbContext para aplicar automaticamente o filtro do tenant logado.

## 🤝 Contribuindo

Este é um projeto de portfólio pessoal, mas sugestões e feedback são bem-vindos!

## 📄 Licença

Este projeto está sob a licença MIT.

## 👤 Autor

[Seu Nome]
- GitHub: [@psielta](https://github.com/psielta)
- LinkedIn: [Mateus Salgueiro](https://www.linkedin.com/in/mateus-salgueiro-525717205/)

---

Desenvolvido como parte do meu portfólio de desenvolvimento web com .NET
