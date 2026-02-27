agro-identity-api

API de Identidade e Autenticação do projeto AgroSolutions IoT (FIAP Tech Challenge – Fase 5).

Responsável por:
- Cadastro/autenticação de usuários
- Emissão de JWT
- Endpoints protegidos para validação/identidade do usuário

STACK
- .NET (Minimal API / ASP.NET)
- Azure SQL (schema identity, quando usando DB único)
- JWT Bearer Authentication
- Swagger/OpenAPI

PRINCIPAIS ENDPOINTS
- GET /health
- POST /auth/login
- (opcional) POST /auth/register
- (opcional) GET /me

CONFIGURAÇÃO (ENV VARS)
- ConnectionStrings__Default
- Jwt__Issuer
- Jwt__Audience
- Jwt__Key

RODAR LOCALMENTE
dotnet restore
dotnet run

SWAGGER
/ swagger

RODAR VIA DOCKER
docker build -t agro-identity-api .
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__Default="..." \
  -e Jwt__Issuer="AgroSolutions.Identity" \
  -e Jwt__Audience="AgroSolutions" \
  -e Jwt__Key="..." \
  agro-identity-api

TESTE RÁPIDO (TOKEN VIA CLI)
curl -s -X POST "http://localhost:8080/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"kevin@teste.com","password":"Senha123!"}'

OBSERVAÇÕES PARA AKS/INGRESS
Quando publicado atrás do Ingress, costuma ser acessado via:
- /identity/...