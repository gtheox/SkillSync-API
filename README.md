# SkillSync API

API RESTful desenvolvida em .NET 8 para a plataforma SkillSync, focada em matchmaking de freelancers e projetos usando Inteligência Artificial.

## 📋 Índice

- [Visão Geral](#-visão-geral)
- [Arquitetura](#-arquitetura)
- [Requisitos](#-requisitos)
- [Configuração](#-configuração)
- [Uso da API](#-uso-da-api)
- [API em Produção](#-api-em-produção)
- [Testes](#-testes)
- [Estrutura do Projeto](#-estrutura-do-projeto)

## 🎯 Visão Geral

A SkillSync API é o núcleo do ecossistema SkillSync, uma plataforma de matchmaking entre freelancers e projetos utilizando Inteligência Artificial. A API oferece:

- **Autenticação JWT** para segurança
- **Versionamento de API** (v1.0 e v2.0)
- **Integração com Oracle Database** usando Entity Framework Core
- **Integração com API de IA** (Python/FastAPI) para matchmaking inteligente
- **ML.NET** para classificação automática de categorias de projetos
- **Health Checks** para monitoramento
- **Logging estruturado** com Serilog
- **Swagger/OpenAPI** para documentação interativa

## 🏗️ Arquitetura

### Diagrama C4 Model - Contexto (Level 1)

```mermaid
C4Context
    title Sistema de Contexto - SkillSync

    Person(freelancer, "Freelancer", "Usuário que oferece serviços")
    Person(contratante, "Contratante", "Usuário que publica projetos")
    Person(admin, "Administrador", "Admin que gera dicas via Java App")

    System(skillsync_api, "SkillSync API", "API .NET principal, hub central do ecossistema")
    System(ai_api, "API de IA", "Microserviço Python/FastAPI para matchmaking com Gemini")
    System(mobile_app, "App Mobile", "Aplicativo React Native")
    System(java_app, "Portal Admin Java", "WebApp Spring Boot para gerar dicas")
    SystemDb(oracle_db, "Oracle Database", "Banco de dados relacional central")
    SystemDb(mongodb, "MongoDB", "Banco NoSQL para dados exportados")

    Rel(freelancer, mobile_app, "Usa")
    Rel(contratante, mobile_app, "Usa")
    Rel(mobile_app, skillsync_api, "Consome API REST")
    Rel(skillsync_api, oracle_db, "Lê/Escreve dados")
    Rel(skillsync_api, ai_api, "Chama para gerar matches")
    Rel(java_app, oracle_db, "Escreve dicas")
    Rel(skillsync_api, oracle_db, "Lê dicas geradas")
    Rel(oracle_db, mongodb, "Exporta dados JSON")
```

### Diagrama C4 Model - Container (Level 2)

```mermaid
C4Container
    title Container - SkillSync API

    Person(user, "Usuário", "Freelancer ou Contratante")

    System_Ext(mobile_app, "App Mobile", "React Native")
    System_Ext(ai_api, "API de IA", "Python/FastAPI")
    SystemDb_Ext(oracle_db, "Oracle Database", "Banco de dados relacional")

    Container(skillsync_api, "SkillSync API", ".NET 8, ASP.NET Core", "API RESTful principal")
    ContainerDb(oracle_db_container, "Oracle Database", "Oracle", "Armazena dados")

    Rel(user, mobile_app, "Usa")
    Rel(mobile_app, skillsync_api, "HTTPS")
    Rel(skillsync_api, ai_api, "HTTPS")
    Rel(skillsync_api, oracle_db, "Entity Framework Core")
```

## 📋 Requisitos

- **.NET 8.0 SDK** ou superior
- **Oracle Database** (com schema SkillSync criado)
- **Git** para clonar o repositório

## 🚀 Configuração

### 1. Clonar o Repositório

```bash
git clone https://github.com/gtheox/SkillSync-API.git
cd SkillSync-API/Dotnet
```

### 2. Restaurar Dependências

```bash
dotnet restore
```

### 3. Executar a API

```bash
# Usando script helper
./start-api.sh

# Ou diretamente
dotnet run --project SkillSync.API
```

A API estará disponível em:
- **Swagger UI**: `http://localhost:5004` (apenas em desenvolvimento)
- **API**: `http://localhost:5004/api/v1`
- **Health Check**: `http://localhost:5004/health`

## 📚 Uso da API

### Autenticação

A API utiliza JWT (JSON Web Tokens) para autenticação. Todos os endpoints protegidos requerem o header `Authorization: Bearer {token}`.

#### Registrar Usuário

```http
POST /api/v1/auth/register
Content-Type: application/json

{
  "nome": "João Silva",
  "email": "joao@example.com",
  "senha": "Senha123!",
  "role": "FREELANCER"
}
```

**Resposta (201 Created):**
```json
{
  "email": "joao@example.com",
  "role": "FREELANCER",
  "idUsuario": 1,
  "message": "Usuário registrado com sucesso. Faça login para obter o token."
}
```

#### Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "email": "joao@example.com",
  "senha": "Senha123!"
}
```

**Resposta (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "joao@example.com",
  "role": "FREELANCER",
  "idUsuario": 1
}
```

### Endpoints Principais

Todos os endpoints estão disponíveis em: `https://skillsync-api-t4l2.onrender.com/api/v1`

#### Autenticação

- `POST /api/v1/auth/register` - Registrar novo usuário
- `POST /api/v1/auth/login` - Login e obtenção de token JWT

#### Projetos

- `GET /api/v1/projetos` - Listar projetos (com paginação e HATEOAS)
- `GET /api/v1/projetos/{id}` - Buscar projeto por ID
- `POST /api/v1/projetos` - Criar novo projeto (requer autenticação)
- `PUT /api/v1/projetos/{id}` - Atualizar projeto (requer autenticação)
- `DELETE /api/v1/projetos/{id}` - Deletar projeto (requer autenticação)
- `POST /api/v1/projetos/{id}/gerar-matches` - Gerar matches usando IA (requer autenticação)

#### Perfis

- `GET /api/v1/perfis` - Listar perfis
- `GET /api/v1/perfis/{id}` - Buscar perfil por ID
- `POST /api/v1/perfis` - Criar novo perfil (requer autenticação)
- `PUT /api/v1/perfis/{id}` - Atualizar perfil (requer autenticação)
- `DELETE /api/v1/perfis/{id}` - Deletar perfil (requer autenticação)

#### Dicas

- `GET /api/v1/dicas` - Listar dicas geradas pela IA
- `GET /api/v1/dicas/{id}` - Buscar dica por ID

#### Health Check

- `GET /health` - Verificar saúde da API e banco de dados

### Versionamento

A API suporta versionamento através do prefixo `/api/v{version}/`:

- **v1.0**: Versão inicial da API
- **v2.0**: Versão com filtros avançados e ML.NET melhorado

### Documentação

A documentação interativa (Swagger UI) está disponível em:

- **Desenvolvimento**: `http://localhost:5004`
- **Produção**: `https://skillsync-api-t4l2.onrender.com`

O Swagger está habilitado por padrão. Para desabilitá-lo em produção, defina a variável de ambiente `EnableSwagger=false` no Render Dashboard.

### Exemplo de Uso

```bash
# 1. Registrar usuário
curl -X POST "https://skillsync-api-t4l2.onrender.com/api/v1/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva",
    "email": "joao@example.com",
    "senha": "Senha123!",
    "role": "CONTRATANTE"
  }'

# 2. Login
TOKEN=$(curl -X POST "https://skillsync-api-t4l2.onrender.com/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "joao@example.com",
    "senha": "Senha123!"
  }' | jq -r '.token')

# 3. Criar projeto
curl -X POST "https://skillsync-api-t4l2.onrender.com/api/v1/projetos" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "titulo": "Desenvolvimento de API REST",
    "descricao": "Preciso de um desenvolvedor para criar uma API REST",
    "orcamento": 5000.00,
    "habilidadesRequisitadas": [1, 2]
  }'

# 4. Gerar matches
curl -X POST "https://skillsync-api-t4l2.onrender.com/api/v1/projetos/1/gerar-matches" \
  -H "Authorization: Bearer $TOKEN"
```

## 🌐 API em Produção

A API está disponível em produção:

**URL Base**: https://skillsync-api-t4l2.onrender.com

### Endpoints Disponíveis

- `GET /health` - Health check da API e banco de dados
- `POST /api/v1/auth/register` - Registrar novo usuário
- `POST /api/v1/auth/login` - Login e obtenção de token JWT
- `GET /api/v1/projetos` - Listar projetos (com paginação)
- `GET /api/v1/projetos/{id}` - Buscar projeto por ID
- `POST /api/v1/projetos` - Criar projeto (requer autenticação)
- `PUT /api/v1/projetos/{id}` - Atualizar projeto (requer autenticação)
- `DELETE /api/v1/projetos/{id}` - Deletar projeto (requer autenticação)
- `POST /api/v1/projetos/{id}/gerar-matches` - Gerar matches usando IA (requer autenticação)
- `GET /api/v1/perfis` - Listar perfis
- `GET /api/v1/perfis/{id}` - Buscar perfil por ID
- `POST /api/v1/perfis` - Criar perfil (requer autenticação)
- `PUT /api/v1/perfis/{id}` - Atualizar perfil (requer autenticação)
- `DELETE /api/v1/perfis/{id}` - Deletar perfil (requer autenticação)
- `GET /api/v1/dicas` - Listar dicas geradas pela IA
- `GET /api/v1/dicas/{id}` - Buscar dica por ID

## 🧪 Testes

### Executar Testes

```bash
dotnet test
```

### Executar Testes com Cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Testes Disponíveis

Os testes de integração estão em `SkillSync.API.Tests` e incluem:

- Testes de autenticação (registro, login)
- Testes de controllers (projetos, perfis)
- Testes de health check

## 📦 Estrutura do Projeto

```
SkillSync.API/
├── Controllers/
│   ├── V1/
│   │   ├── AuthController.cs
│   │   ├── ProjetosController.cs
│   │   ├── PerfisController.cs
│   │   ├── DicasController.cs
│   │   └── MatchesController.cs
│   └── V2/
│       └── ProjetosV2Controller.cs
├── Data/
│   └── SkillSyncDbContext.cs
├── DTOs/
│   ├── Auth/
│   ├── Projeto/
│   ├── Perfil/
│   ├── Dica/
│   └── AI/
├── Helpers/
│   └── JwtHelper.cs
├── Models/
│   └── (Entidades do banco de dados)
├── Services/
│   ├── IAuthService.cs
│   ├── AuthService.cs
│   ├── IAIService.cs
│   ├── AIService.cs
│   ├── IMLService.cs
│   └── MLService.cs
├── ML/
│   └── (Modelos ML.NET)
├── Program.cs
└── appsettings.json

SkillSync.API.Tests/
├── AuthControllerTests.cs
├── ProjetosControllerTests.cs
└── HealthCheckTests.cs
```

## 🔧 Configurações

### Logging

O projeto utiliza Serilog para logging estruturado. Os logs são escritos em:
- **Console** (desenvolvimento)
- **Arquivo** (`logs/skillsync-*.log`)

### Health Checks

O endpoint `/health` verifica:
- Conexão com o banco de dados Oracle
- Status da API
- Informações detalhadas de cada check

### CORS

CORS está configurado para permitir todas as origens em desenvolvimento. Para produção, configure origens específicas no `Program.cs`.

## 📝 Notas Importantes

- A API utiliza procedures do Oracle para operações de escrita (INSERT, UPDATE, DELETE)
- O modelo ML.NET é treinado automaticamente quando há dados suficientes no banco
- A integração com a API de IA Python é assíncrona e utiliza HttpClient
- Todos os endpoints protegidos requerem autenticação JWT
- O Swagger está disponível apenas em desenvolvimento por padrão
- Cada usuário pode ter apenas um perfil (constraint `UK_GS_PERFIS_USUARIO`)

## 🔗 Links

- [Repositório GitHub](https://github.com/gtheox/SkillSync-API)
- [API de IA](https://skillsync-ai-api.onrender.com) - Microserviço de IA Generativa

## 📄 Licença

Este projeto é parte do trabalho acadêmico do curso de Advanced Business Development with .NET.
