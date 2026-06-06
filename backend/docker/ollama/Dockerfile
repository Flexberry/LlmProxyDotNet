# Dockerfile для Ollama с автоматической загрузкой моделей
FROM ollama/ollama:latest

# Копируем скрипт инициализации
COPY init-models.sh /usr/local/bin/init-models.sh
RUN chmod +x /usr/local/bin/init-models.sh

# Переменная окружения для настройки моделей
ENV OLLAMA_MODELS="llama3.2:latest mistral:latest"

# Директория для моделей
VOLUME /root/.ollama

EXPOSE 11434

# Используем наш скрипт как точку входа
ENTRYPOINT ["/usr/local/bin/init-models.sh"]
