# Dockerfile для Ollama с автоматической загрузкой моделей
# Используем конкретную версию для детерминированной сборки
FROM ollama/ollama:0.1.44

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
