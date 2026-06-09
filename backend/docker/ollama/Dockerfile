# Dockerfile для Ollama с автоматической загрузкой моделей
FROM ollama/ollama:latest

# Переменная окружения для настройки моделей
ENV OLLAMA_MODELS="llama3.2:latest mistral:latest"

# Директория для моделей
VOLUME /root/.ollama

EXPOSE 11434

# Копируем скрипт инициализации
COPY init-ollama.sh /init-ollama.sh
RUN chmod +x /init-ollama.sh

# Запускаем скрипт инициализации
ENTRYPOINT ["/init-ollama.sh"]

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:11434/api/tags || exit 1
