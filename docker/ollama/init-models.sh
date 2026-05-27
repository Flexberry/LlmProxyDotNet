#!/bin/bash
# /docker-entrypoint.d/01-pull-models.sh

set -e

echo "=== Ollama Model Initialization ==="

# Файл маркер, чтобы избежать повторной загрузки
MARKER_FILE="/root/.ollama/models_loaded"

if [ -f "$MARKER_FILE" ]; then
    echo "Models already loaded, skipping..."
    exit 0
fi

# Список моделей для загрузки (настраивается через переменные окружения)
MODELS_TO_PULL="${OLLAMA_MODELS:-llama3.2:latest mistral:latest}"

for model in $MODELS_TO_PULL; do
    echo "Pulling model: $model"
    ollama pull "$model" || {
        echo "Warning: Failed to pull $model, continuing..."
    }
done

# Создаем маркер
touch "$MARKER_FILE"

echo "=== Model initialization complete ==="
ollama list
