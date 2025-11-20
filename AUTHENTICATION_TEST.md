# Sistema de Autenticação - Instruções de Teste

## Credenciais de Teste

O sistema foi populado com os seguintes usuários de teste:

### 1. Admin User (Uma organização)
- **Email:** admin@example.com
- **Senha:** admin123
- **Organização:** Empresa Exemplo (Owner)
- **Características:** Usuário administrador com role global "admin"

### 2. João Silva (Uma organização)
- **Email:** joao@example.com
- **Senha:** senha123
- **Organização:** Empresa Exemplo (Member)
- **Características:** Usuário normal com role "member" na organização

### 3. Maria Santos (Múltiplas organizações)
- **Email:** maria@example.com
- **Senha:** maria123
- **Organizações:**
  - Empresa Exemplo (Admin)
  - Startup Tech (Owner)
- **Características:** Usuário com acesso a múltiplas organizações

## Como Testar

### 1. Login com Senha

1. Acesse a aplicação (será redirecionado para `/Auth/Login`)
2. Digite um dos emails acima
3. Se o usuário tem apenas uma organização, será direcionado para escolher o método de login
4. Se tem múltiplas organizações (Maria), primeiro escolherá a organização
5. Escolha "Login com Senha"
6. Digite a senha correspondente
7. Após login bem-sucedido, será redirecionado para a página inicial

### 2. Login com OTP (Email)

**Importante:** Certifique-se de que o MailHog está rodando no Docker:
```bash
docker-compose up mailhog
```

Acesse o MailHog em: http://localhost:8025

1. Acesse a aplicação
2. Digite um dos emails de teste
3. Escolha a organização (se aplicável)
4. Escolha "Login por Código (OTP)"
5. O sistema enviará um código de 6 dígitos para o email
6. Acesse o MailHog em http://localhost:8025 para ver o email
7. Digite o código recebido
8. Após validação, será autenticado

## Funcionalidades Implementadas

### Autenticação
- ✅ Login com email e senha
- ✅ Login com OTP (One-Time Password) via email
- ✅ Suporte a múltiplas organizações
- ✅ Seleção de organização durante login
- ✅ Logout

### Segurança
- ✅ Hashing de senhas com BCrypt (work factor 12)
- ✅ Sessões gerenciadas no banco de dados
- ✅ Cookies de autenticação criptografados
- ✅ CSRF protection nos formulários
- ✅ OTP com expiração de 5 minutos
- ✅ Sliding expiration nas sessões (24 horas)

### Autorização
- ✅ Atributo `[CustomAuthorize]` para proteger controllers
- ✅ Atributo `[OrganizationAuthorize]` para verificar roles na organização
- ✅ Suporte a roles globais e por organização
- ✅ Página de "Acesso Negado" customizada

### Interface do Usuário
- ✅ Telas de login responsivas com Bootstrap 5
- ✅ Dropdown com informações do usuário no layout
- ✅ Exibição da organização atual
- ✅ Opção para trocar de organização
- ✅ Indicadores visuais de role do usuário

### Infraestrutura
- ✅ BaseController com propriedades de contexto do usuário
- ✅ Serviços injetáveis (DI) para autenticação, email, sessão
- ✅ Integração com Entity Framework Core
- ✅ Seed data para testes
- ✅ Integração com MailHog para desenvolvimento

## Propriedades Disponíveis no BaseController

Todos os controllers que herdam de `BaseController` têm acesso a:

```csharp
// IDs
CurrentUserId          // Guid do usuário atual
CurrentOrganizationId  // Guid da organização ativa

// Strings
CurrentUserName        // Nome do usuário
CurrentUserEmail       // Email do usuário
CurrentOrganizationName // Nome da organização
CurrentOrganizationRole // Role na organização (owner, admin, member)
CurrentGlobalRole      // Role global (admin, user)

// Objetos completos
CurrentUser           // Objeto User com todas as propriedades
CurrentOrganization   // Objeto Organization
CurrentMembership     // Relação User-Organization

// Helpers
IsOrganizationAdmin   // True se é admin ou owner
IsGlobalAdmin        // True se tem role global admin
UserHasOrganizationRole("role1", "role2") // Verifica roles
UserHasGlobalRole("admin")                // Verifica role global
```

## Estrutura de Dados

### Tabelas do Banco
- **user**: Usuários do sistema
- **organization**: Organizações/Empresas
- **memberships**: Vínculos usuário-organização com roles
- **account**: Credenciais de autenticação (senhas, OAuth)
- **session**: Sessões ativas dos usuários

### Roles por Organização
- **owner**: Dono da organização (todos os privilégios)
- **admin**: Administrador (quase todos os privilégios)
- **member**: Membro regular
- **viewer**: Apenas visualização

### Roles Globais
- **admin**: Administrador do sistema
- **user**: Usuário normal

## Troubleshooting

### Email não chegando
- Verifique se o MailHog está rodando: `docker ps`
- Acesse http://localhost:8025
- Verifique se a porta 1025 está liberada

### Erro de conexão com banco
- Verifique se o PostgreSQL está rodando: `docker ps`
- Verifique a connection string em Web.config
- Certifique-se de que as migrations foram aplicadas

### Sessão expirando rapidamente
- As sessões têm validade de 24 horas com sliding expiration
- Cada atividade renova o tempo de expiração
- Verifique a tabela `session` no banco

## Próximos Passos

Para expandir o sistema, você pode:

1. Implementar recuperação de senha
2. Adicionar autenticação two-factor (2FA)
3. Implementar OAuth (Google, Microsoft, etc.)
4. Adicionar logs de auditoria
5. Implementar rate limiting
6. Adicionar testes unitários e de integração