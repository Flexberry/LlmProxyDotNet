# Dockerfile для Ollama с автоматической загрузкой моделей
FROM ollama/ollama:latest

# Копируем скрипт инициализации
COPY init-models.sh /init-models.sh
RUN chmod +x /init-models.sh

# Переменная окружения для настройки моделей
ENV OLLAMA_MODELS="llama3.2:latest mistral:latest"

# Директория для моделей
VOLUME /root/.ollama

EXPOSE 11434

# Используем наш скрипт как точку входа
ENTRYPOINT ["/init-models.sh"]
