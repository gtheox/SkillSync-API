#!/bin/bash

# Script para iniciar a SkillSync API

echo "🚀 Iniciando SkillSync API..."

# Verificar se já está rodando
EXISTING=$(ps aux | grep -E "SkillSync.API|dotnet.*run.*SkillSync" | grep -v grep | awk '{print $2}')

if [ ! -z "$EXISTING" ]; then
    echo "⚠️  A API já está rodando (PID: $EXISTING)"
    echo "Execute ./stop-api.sh para encerrar antes de iniciar novamente"
    exit 1
fi

# Verificar se a porta 5004 está em uso por um processo SkillSync
PORT_5004=$(lsof -i :5004 2>/dev/null | grep -i "SkillSync")

if [ ! -z "$PORT_5004" ]; then
    echo "⚠️  Porta 5004 está em uso por um processo SkillSync:"
    echo "$PORT_5004"
    echo "Execute ./stop-api.sh para liberar a porta"
    exit 1
fi

# Navegar para o diretório do projeto
cd "$(dirname "$0")"

# Verificar se o build está atualizado
echo "📦 Verificando build..."
if ! dotnet build --verbosity quiet --no-incremental > /dev/null 2>&1; then
    echo "❌ Erro no build. Execute: dotnet build para ver os detalhes"
    exit 1
fi

# Iniciar a API em modo Development para habilitar Swagger
echo "🚀 Iniciando API na porta 5004 (modo Development)..."
echo "📚 Swagger disponível em: http://localhost:5004"
echo "🏥 Health Check disponível em: http://localhost:5004/health"
echo ""
echo "Pressione Ctrl+C para parar a API"
echo ""

# Definir explicitamente o ambiente como Development para habilitar Swagger
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --project SkillSync.API

