# E2E Tests

End-to-end тесты для frontend приложения с использованием Playwright.

## Установка

```bash
cd frontend
npm install -D @playwright/test
npx playwright install
```

## Запуск тестов

```bash
# Запуск всех E2E тестов
npm run test:e2e

# Запуск в headed режиме (с браузером)
npm run test:e2e:headed

# Запуск для конкретного браузера
npm run test:e2e -- --project=chromium

# Запуск с генерацией trace для отладки
npm run test:e2e -- --trace on

# Запуск конкретного файла теста
npx playwright test e2e/api-keys.spec.ts

# Запуск конкретного теста по имени
npx playwright test -g "should create a new API key"
```

## Структура тестов

```
e2e/
├── playwright.config.ts    # Playwright конфигурация
├── api-keys.spec.ts        # Тесты для управления API ключами
├── chat-completion.spec.ts # Тесты для отправки запросов к LLM
└── budget-management.spec.ts # Тесты для управления бюджетами
```

## Конфигурация

Файл `e2e/playwright.config.ts` содержит конфигурацию:

- **Три браузера**: Chromium, Firefox, WebKit
- **Base URL**: `http://localhost:3000` (настраивается через `NEXT_PUBLIC_BASE_URL`)
- **Web server**: Автоматически запускает `npm run dev` перед тестами
- **Reporter**: HTML отчет в `playwright-report`

## Переменные окружения

```bash
# Базовый URL для тестов (по умолчанию http://localhost:3000)
NEXT_PUBLIC_BASE_URL=http://localhost:3000

# Режим CI (включает retries и изменяет workers)
CI=true
```

## Лучшие практики

### 1. Используйте role-based селекторы

```typescript
// Хорошо
await page.getByRole('button', { name: /create/i }).click();

// Избегайте
await page.click('.btn-create');
```

### 2. Добавляйте timeouts для асинхронных операций

```typescript
await expect(page.getByText(/success/i)).toBeVisible({ timeout: 10000 });
```

### 3. Используйте before/after hooks для подготовки

```typescript
test.beforeEach(async ({ page }) => {
  await page.goto('/keys');
});
```

### 4. Тестируйте критические пользовательские сценарии

- Создание API ключа
- Отправка запроса к LLM
- Управление бюджетами
- RBAC и permissions

## CI/CD интеграция

### GitHub Actions

```yaml
name: E2E Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with:
          node-version: '20'
      - run: npm ci
      - run: npx playwright install --with-deps
      - run: npm run test:e2e
      - uses: actions/upload-artifact@v3
        if: always()
        with:
          name: playwright-report
          path: playwright-report/
```

## Generation mode

Для генерации тестов на основе вашего UI:

```bash
npx playwright codegen http://localhost:3000/keys
```

Это откроет браузер с инструментом записи тестов.

## Troubleshooting

### Тесты падают на "Element not visible"

- Убедитесь, что backend запущен
- Проверьте, что API ключи созданы
- Используйте `page.pause()` для отладки

### Браузер не запускается

```bash
npx playwright install --with-deps
```

### Тесты слишком медленные

- Используйте `test.describe.configure({ timeout: 30000 })` для целых групп
- Уменьшите количество параллельных workers в CI
- Используйте `--retries 1` вместо большего количества

## См. также

- [Playwright Documentation](https://playwright.dev)
- [Testing Library](https://testing-library.com)
- [Jest Tests](../__tests__/README.md)
