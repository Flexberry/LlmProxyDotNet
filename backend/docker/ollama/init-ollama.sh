#!/bin/bash
# Скрипт инициализации Ollama с автоматической загрузкой моделей

set -e

echo "=== Starting Ollama Server ==="

# Запускаем ollama serve в фоне
ollama serve &
OLLAMA_PID=$!

# Ждём готовности сервера
echo "Waiting for Ollama to be ready..."
for i in $(seq 1 30); do
    if kill -0 $OLLAMA_PID 2>/dev/null; then
        if curl -s http://localhost:11434/api/tags >/dev/null 2>&1; then
            echo "Ollama is ready!"
            break
        fi
    else
        echo "ERROR: ollama serve died"
        exit 1
    fi
    sleep 1
done

# Файл-маркер, чтобы избежать повторной загрузки
MARKER_FILE="/root/.ollama/models_loaded"

if [ ! -f "$MARKER_FILE" ]; then
    MODELS_TO_PULL="${OLLAMA_MODELS:-llama3:latest}"
    echo "=== Pulling models: $MODELS_TO_PULL ==="
    for model in $MODELS_TO_PULL; do
        echo "Pulling: $model"
        ollama pull "$model" || echo "Warning: Failed to pull $model (will retry next start)"
    done
    touch "$MARKER_FILE"
    echo "=== Models loaded ==="
    ollama list
else
    echo "Models already loaded, skipping..."
fi

# Держим контейнер живым, ждём завершения ollama serve
echo "=== Ollama running ==="
wait $OLLAMA_PID
