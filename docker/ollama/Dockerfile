# Dockerfile для Ollama с автоматической загрузкой моделей
# Путь: docker/ollama/Dockerfile

FROM ollama/ollama:latest

# Копируем скрипт инициализации
COPY init-models.sh /docker-entrypoint.d/01-pull-models.sh

# Делаем скрипт исполняемым
RUN chmod +x /docker-entrypoint.d/01-pull-models.sh

# Переменная окружения для настройки моделей
# Пример: OLLAMA_MODELS="llama3.2:latest mistral:latest llama3:latest"
ENV OLLAMA_MODELS="llama3.2:latest mistral:latest"

# Директория для моделей (для кэширования в Docker)
VOLUME /root/.ollama/models

EXPOSE 11434
