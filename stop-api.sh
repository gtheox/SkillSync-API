#!/bin/bash

# Script para encerrar processos da SkillSync API

echo "🛑 Encerrando processos da SkillSync API..."

# Encontrar e encerrar processos
PIDS=$(ps aux | grep -E "SkillSync.API|dotnet.*run.*SkillSync" | grep -v grep | awk '{print $2}')

if [ -z "$PIDS" ]; then
    echo "✅ Nenhum processo da SkillSync API encontrado rodando"
    exit 0
fi

echo "Processos encontrados: $PIDS"

# Encerrar processos
for PID in $PIDS; do
    echo "Encerrando processo $PID..."
    kill -9 $PID 2>/dev/null
done

sleep 2

# Verificar se foram encerrados
REMAINING=$(ps aux | grep -E "SkillSync.API|dotnet.*run.*SkillSync" | grep -v grep | awk '{print $2}')

if [ -z "$REMAINING" ]; then
    echo "✅ Todos os processos foram encerrados com sucesso"
    echo "✅ Portas liberadas - pronto para rodar novamente"
else
    echo "⚠️  Ainda há processos rodando: $REMAINING"
    echo "Tente executar: kill -9 $REMAINING"
fi

# Verificar porta 5004
echo ""
echo "Verificando porta 5004..."
lsof -i :5004 2>/dev/null | grep -i "SkillSync" || echo "✅ Porta 5004 livre"

