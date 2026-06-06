# LLM Proxy

Альтернатива LiteLLM на .NET 10 + Next.js 16 с OpenAI-совместимым API.

## Структура проекта

```
.
├── backend/          # Backend на .NET (ASP.NET Core)
│   ├── src/          # Исходный код backend
│   ├── tests/        # Тесты backend
│   ├── docker/       # Docker конфигурации
│   └── docker-compose.yml
├── frontend/         # Frontend на Next.js
│   ├── src/          # Исходный код frontend
│   ├── __tests__/    # Тесты frontend
│   └── Dockerfile
└── README.md         # Этот файл
```

## Быстрый старт

```bash
# Запуск через Docker Compose (из папки backend/)
cd backend
docker-compose up -d --build

# Frontend будет доступен на http://localhost:3000
# Backend API на http://localhost:4000
```

## Документация

- [Backend README](backend/README.md) — Подробная документация по backend
- [Frontend README](frontend/README.md) — Документация по frontend

## Разработка

### Backend

```bash
cd backend
dotnet restore
dotnet run --project src/LlmProxy.App
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Тестирование

### Backend тесты

```bash
cd backend
dotnet test
```

### Frontend тесты

```bash
cd frontend
npm test
```
