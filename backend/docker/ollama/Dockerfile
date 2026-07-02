# Dockerfile для Ollama с автоматической загрузкой моделей
FROM ollama/ollama:latest

# Переменная окружения для настройки моделей
ENV OLLAMA_MODELS="llama3:latest mistral:latest"

# Директория для моделей
VOLUME /root/.ollama

EXPOSE 11434

# Копируем скрипт и инициализации
COPY init-ollama.sh /tmp/init-ollama.sh
RUN sed -i 's/\r$//' /tmp/init-ollama.sh && \
    chmod +x /tmp/init-ollama.sh && \
    cp /tmp/init-ollama.sh /init-ollama.sh

# Запускаем скрипт инициализации
ENTRYPOINT ["/init-ollama.sh"]

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:11434/api/tags || exit 1
