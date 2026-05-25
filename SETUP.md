# Инструкция по запуску LLM Proxy

## Быстрый старт

### 1. Предварительные требования

- .NET 10 SDK
- Docker и Docker Compose
- Node.js 20+ (для frontend)
- PostgreSQL 16+ (или используйте Docker)
- Redis 7+ (или используйте Docker)

### 2. Настройка переменных окружения

Создайте файл `.env` в корне проекта:

```bash
# База данных
DATABASE_URL=Host=localhost;Port=5432;Database=litellm;Username=user;Password=password

# Мастер ключ (для админ-панели)
LITELLM_MASTER_KEY=sk_master_dev_001

# API ключи провайдеров (замените на свои)
OPENAI_API_KEY=sk-your-openai-key-here
OPENROUTER_API_KEY=sk-your-openrouter-key-here
ZAI_API_KEY=sk-your-zai-key-here

# Redis
REDIS_CONNECTION=localhost:6379

# Конфигурация
DEFAULT_PROVIDER=openai
```

Для Windows PowerShell используйте `Set-EnvironmentVariable`:

```powershell
$env:DATABASE_URL = "Host=localhost;Port=5432;Database=litellm;Username=user;Password=password"
$env:LITELLM_MASTER_KEY = "sk_master_dev_001"
$env:OPENAI_API_KEY = "sk-your-openai-key-here"
$env:REDIS_CONNECTION = "localhost:6379"
```

### 3. Запуск зависимостей через Docker

```powershell
# В корне проекта
docker-compose up -d postgres redis
```

Проверьте, что контейнеры запущены:

```powershell
docker ps | Select-String "postgres|redis"
```

### 4. Настройка локальных LLM провайдеров (опционально)

#### Ollama (рекомендуется для тестов)

1. Скачайте с https://ollama.ai
2. Установите модель:
   ```powershell
   ollama pull llama3
   ```
3. Запустите:
   ```powershell
   ollama serve
   ```

#### vLLM (опционально)

Используйте Docker Compose:

```powershell
docker-compose up -d vllm
```

### 5. Применение миграций БД

```powershell
cd src/LlmProxy.App
dotnet ef database update
cd ../..
```

### 6. Запуск Backend

```powershell
cd src/LlmProxy.App
dotnet run --urls="http://localhost:4000"
```

Backend запустится на `http://localhost:4000`

### 7. Запуск Frontend (в новом окне PowerShell)

```powershell
cd frontend
npm install  # Первый запуск
npm run dev
```

Frontend запустится на `http://localhost:3000`

---

## Тестирование

### Unit тесты

```powershell
cd tests/LlmProxy.Tests.Unit
dotnet test
```

Ожидается: **36 тестов, все пройдены**

### Интеграционные тесты

```powershell
cd tests/LlmProxy.Tests.Integration
dotnet test
```

**Примечание**: Интеграционные тесты требуют работающего Docker. Если тесты не проходят, убедитесь, что:
1. Docker Desktop запущен
2. Контейнеры PostgreSQL и Redis не конфликтуют с локальными инстансами

### Ручное тестирование через curl

Создайте API ключ через админ-панель или используйте тестовый ключ:

```powershell
# Проверка endpoint моделей (без auth)
curl http://localhost:4000/v1/models

# Чат с Ollama (требуется запущенный Ollama)
curl http://localhost:4000/v1/chat/completions `
  -H "Authorization: Bearer YOUR_API_KEY" `
  -H "Content-Type: application/json" `
  -d "{\"model\":\"ollama/llama3\",\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}"

# Чат с OpenAI
curl http://localhost:4000/v1/chat/completions `
  -H "Authorization: Bearer YOUR_API_KEY" `
  -H "Content-Type: application/json" `
  -d "{\"model\":\"openai/gpt-4o\",\"messages\":[{\"role\":\"user\",\"content\":\"Hello\"}]}"
```

---

## Решение проблем

### Ошибка "Provider error: NotFound"

**Причина**: Провайдер недоступен (не запущен или неверный API ключ)

**Решение**:
1. Проверьте, что API ключи провайдеров настроены в `.env`
2. Убедитесь, что локальные провайдеры (Ollama/vLLM) запущены
3. Проверьте логи:
   ```powershell
   docker-compose logs app
   ```

### Ошибка подключения к PostgreSQL

**Причина**: БД недоступна или неверные учётные данные

**Решение**:
```powershell
# Перезапустите контейнер
docker-compose restart postgres

# Проверьте подключение
docker-compose exec postgres psql -U user -d litellm -c "SELECT 1"
```

### Ошибка "Redis connection failed"

**Решение**:
```powershell
docker-compose restart redis
```

### Конфликт портов

Если порт 4000, 5432 или 6379 занят:

1. Измените порт в `docker-compose.yml`
2. Или остановите конфликтующее приложение

---

## Администрирование

### Создание API ключа

Через UI: `http://localhost:3000/keys`

Или через API:

```powershell
curl http://localhost:4000/admin/keys `
  -H "X-Admin-Key: sk_master_dev_001" `
  -H "Content-Type: application/json" `
  -d "{\"name\":\"Test Key\",\"permissions\":[\"*\"]}"
```

### Просмотр логов

```powershell
docker-compose logs -f app
```

### Остановка всех сервисов

```powershell
docker-compose down
```

---

## Структура проекта

```
.
├── src/
│   ├── LlmProxy.App/          # Backend ASP.NET Core
│   ├── LlmProxy.Core/         # Бизнес-логика
│   └── LlmProxy.Infrastructure/ # БД, провайдеры, ORM
├── tests/
│   ├── LlmProxy.Tests.Unit/   # Unit тесты
│   └── LlmProxy.Tests.Integration/ # Интеграционные тесты
├── frontend/                   # Next.js UI
├── docker-compose.yml          # Контейнеризация
└── SETUP.md                    # Эта инструкции
```

---

## Поддерживаемые провайдеры

| Провайдер | Префикс модели | Конфигурация |
|-----------|---------------|--------------|
| OpenAI | `openai/` | `OPENAI_API_KEY` |
| Ollama | `ollama/` | `PROVIDER__OLLAMA__BASE_URL` |
| vLLM | `vllm/` | `PROVIDER__VLLM__BASE_URL` |
| OpenRouter | `openrouter/` | `OPENROUTER_API_KEY` |
| Z.ai | `zai/` | `ZAI_API_KEY` |

Пример запроса:
```json
{
  "model": "ollama/llama3",
  "messages": [{"role": "user", "content": "Hello"}]
}
```

---

## Следующие шаги

- [ ] Добавить Rate Limiting
- [ ] Реализовать кэширование ответов
- [ ] Добавить вебхуки для событий
- [ ] Интеграция с Prometheus/OpenTelemetry
