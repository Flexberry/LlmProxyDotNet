# LLM Proxy

Альтернатива LiteLLM на .NET 10 + Next.js 16 с OpenAI-совместимым API.

## Обзор

LLM Proxy — это минималистичный proxy-сервер для работы с различными LLM провайдерами (Ollama, vLLM, OpenAI, OpenRouter, Z.ai) через единый OpenAI-совместимый API.

### Особенности

- ✅ **5 провайдеров**: Ollama, vLLM, OpenAI, OpenRouter, Z.ai
- ✅ **OpenAI-совместимый API**: `/v1/chat/completions`, `/v1/embeddings`, `/v1/models`
- ✅ **Docker Compose**: Полная поддержка контейнеризации
- ✅ **Аутентификация**: API ключи + Master Key для администрирования
- ✅ **Маршрутизация**: Выбор провайдера по префиксу модели (`ollama/`, `openai/`, и т.д.)
- ✅ **Streaming**: Server-Sent Events для потоковых ответов
- ✅ **Логирование**: Запись всех запросов в PostgreSQL
- ✅ **Frontend UI**: Next.js панель для управления API ключами

---

## Быстрый старт (Docker)

### 1. Предварительные требования

- Docker и Docker Compose
- API ключи от провайдеров (опционально для локальных моделей)

### 2. Настройка переменных окружения

Создайте файл `.env` в корне проекта:

```bash
# Обязательные
DATABASE_URL=Host=postgres;Port=5432;Database=litellm;Username=user;Password=password
LITELLM_MASTER_KEY=sk_master_dev_001

# API ключи провайдеров (заполните для облачных провайдеров)
OPENAI_API_KEY=sk-your-openai-key
OPENROUTER_API_KEY=sk-your-openrouter-key
ZAI_API_KEY=sk-your-zai-key

# Опциональные
DEFAULT_PROVIDER=openai
VLLM_MODEL=mistralai/Mistral-7B-Instruct-v0.3
```

### 3. Запуск всех сервисов

```bash
# Сборка и запуск
docker-compose up -d --build

# Проверка статуса
docker-compose ps

# Просмотр логов
docker-compose logs -f app
```

### 4. Доступ к сервисам

| Сервис | URL | Описание |
|--------|-----|----------|
| Backend API | http://localhost:4000 | OpenAI-совместимый API |
| Frontend UI | http://localhost:3000 | Панель управления |
| PostgreSQL | localhost:5432 | База данных |
| Redis | localhost:6379 | Кэш |
| Ollama | localhost:11434 | Локальный LLM |
| vLLM | localhost:8000 | OpenAI-совместимый API |

---

## Использование

### Создание API ключа

Через UI: http://localhost:3000/keys

Или через API:

```bash
curl http://localhost:4000/admin/keys \
  -H "X-Admin-Key: sk_master_dev_001" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test Key","permissions":["*"]}'
```

### Отправка запроса к LLM

```bash
# Ollama (локальная модель)
curl http://localhost:4000/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "ollama/llama3.2",
    "messages": [{"role": "user", "content": "Привет!"}]
  }'

# OpenAI
curl http://localhost:4000/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "openai/gpt-4o",
    "messages": [{"role": "user", "content": "Hello!"}]
  }'

# Streaming
curl http://localhost:4000/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "ollama/llama3.2",
    "messages": [{"role": "user", "content": "Считай до 5"}],
    "stream": true
  }'
```

### Получение списка моделей

```bash
curl http://localhost:4000/v1/models
```

---

## Поддерживаемые провайдеры

| Провайдер | Префикс | Base URL | Требуется API Key |
|-----------|---------|----------|-------------------|
| OpenAI | `openai/` | https://api.openai.com/v1 | ✅ |
| Ollama | `ollama/` | http://localhost:11434 | ❌ |
| vLLM | `vllm/` | http://localhost:8000 | ❌ |
| OpenRouter | `openrouter/` | https://openrouter.ai/api/v1 | ✅ |
| Z.ai | `zai/` | https://api.z.ai/v1 | ✅ |

---

## Запуск на других устройствах

### Вариант 1: Docker Compose (рекомендуется)

1. **Клонирование репозитория** на целевом устройстве:
   ```bash
   git clone <your-repo-url>
   cd <project-folder>
   ```

2. **Настройка `.env`**:
   ```bash
   # Укажите публичный IP или домен для Backend
   FRONTEND_URL=http://your-server-ip:3000
   
   # API ключи провайдеров
   OPENAI_API_KEY=sk-your-key
   LITELLM_MASTER_KEY=sk_master_secure_key
   ```

3. **Запуск**:
   ```bash
   docker-compose up -d --build
   ```

4. **Доступ извне**:
   - Откройте порты `4000` (API), `3000` (UI) в firewall
   - Убедитесь, что Docker слушает все интерфейсы (`0.0.0.0`)

### Вариант 2: Отдельные контейнеры

Если не хотите использовать Docker Compose:

```bash
# PostgreSQL
docker run -d --name postgres \
  -e POSTGRES_USER=user \
  -e POSTGRES_PASSWORD=password \
  -e POSTGRES_DB=litellm \
  -p 5432:5432 \
  postgres:16-alpine

# Redis
docker run -d --name redis \
  -p 6379:6379 \
  redis:7-alpine

# Backend
docker run -d --name app \
  -e DATABASE_URL=Host=host.docker.internal;Port=5432;Database=litellm;Username=user;Password=password \
  -e LITELLM_MASTER_KEY=sk_master_dev_001 \
  -e OPENAI_API_KEY=sk-your-key \
  -p 4000:4000 \
  --add-host=host.docker.internal:host-gateway \
  <backend-image>

# Frontend
docker run -d --name frontend \
  -e NEXT_PUBLIC_BACKEND_URL=http://your-server-ip:4000 \
  -p 3000:3000 \
  <frontend-image>
```

### Вариант 3: Локальный запуск (без Docker)

```bash
# Backend
cd src/LlmProxy.App
dotnet restore
dotnet ef database update
dotnet run --urls="http://0.0.0.0:4000"

# Frontend (отдельное окно)
cd frontend
npm install
npm run dev
```

---

## Архитектура

```
Клиент (OpenAI SDK)
    ↓
API Layer .NET (Аутентификация + Валидация)
    ↓
Router (Выбор модели + Балансировка)
    ↓
Provider Adapter (Ollama/vLLM/OpenAI/OpenRouter/Z.ai)
    ↓
Внешний LLM провайдер
```

---

## Структура проекта

```
.
├── src/
│   ├── LlmProxy.App/          # Backend ASP.NET Core
│   ├── LlmProxy.Core/         # Бизнес-логика и интерфейсы
│   └── LlmProxy.Infrastructure/ # БД, провайдеры, ORM
├── tests/
│   ├── LlmProxy.Tests.Unit/   # Unit тесты
│   └── LlmProxy.Tests.Integration/ # Интеграционные тесты
├── frontend/                   # Next.js UI
├── docker/                     # Docker конфигурации
├── docker-compose.yml          # Оркестрация контейнеров
├── .env                        # Переменные окружения
└── README.md                   # Эта документация
```

---

## Тестирование

```bash
# Unit тесты
cd tests/LlmProxy.Tests.Unit
dotnet test

# Интеграционные тесты (требуется Docker)
cd tests/LlmProxy.Tests.Integration
dotnet test
```

---

## Настройки fallback

При ошибке провайдера система автоматически попробует переключиться на следующий доступный провайдер:

- **Максимум попыток**: 2 (настраивается через `FallbackSettings.MaxRetries`)
- **Игнорируемые ошибки**: HTTP 5xx (серверные ошибки)
- **Streaming**: Fallback отключен по умолчанию для потоковых ответов

---

## Будущие улучшения (v2)

- [ ] Rate Limiting на ключ и провайдера
- [ ] Кэширование ответов в Redis
- [ ] Webhook события
- [ ] Интеграция с Prometheus/OpenTelemetry
- [ ] JWT авторизация для frontend
- [ ] Управление бюджетами
- [ ] Team/Org RBAC

---

## Лицензия

Проект распространяется под лицензией MIT. См. файл [LICENSE](LICENSE).

---

## Ссылки

- [SETUP.md](SETUP.md) — Подробная инструкция по локальному запуску
- [Описание.txt](Описание.txt) — Полные требования проекта