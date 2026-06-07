/**
 * API ключ для аутентификации клиента
 */
export interface ApiKey {
  /** Уникальный идентификатор ключа */
  id: string;
  /** SHA256 хеш ключа */
  keyHash: string;
  /** Плоский ключ (только для новых ключей, не сохраняется в БД) */
  key?: string;
  /** Человеко-читаемое имя ключа */
  name?: string;
  /** Разрешённые модели (JSON строка или "*" для всех) */
  permissions: string;
  /** Дата истечения ключа */
  expiresAt?: string;
  /** Статус активности ключа */
  isActive: boolean;
  /** Дата создания ключа */
  createdAt: string;
  /** ID команды (v2) */
  teamId?: string;
}

/**
 * Запрос на создание нового API ключа
 */
export interface CreateApiKeyRequest {
  /** Имя ключа */
  name?: string;
  /** Разрешённые модели (["*"] или ["ollama/llama3", "openai/gpt-4"]) */
  permissions?: string[];
  /** Дата истечения */
  expiresAt?: string;
  /** Конфигурация rate limit (v2) */
  rateLimitConfig?: RateLimitConfig;
}

/**
 * Ответ на создание нового API ключа
 */
export interface CreateApiKeyResponse {
  /** Плоский ключ (показывается только один раз!) */
  key: string;
  /** Созданный объект API ключа */
  apiKey: ApiKey;
}

/**
 * Информация о модели
 */
export interface Model {
  /** Уникальный идентификатор модели */
  id: string;
  /** Тип объекта (всегда "model") */
  object: string;
  /** Временная метка создания */
  created?: number;
  /** Владелец модели */
  owned_by: string;
}

/**
 * Ответ на запрос списка моделей
 */
export interface ModelsListResponse {
  data: Model[];
}

/**
 * Статистика использования LLM Proxy
 */
export interface LogStats {
  /** Общее количество запросов */
  totalRequests: number;
  /** Количество успешных запросов */
  successCount: number;
  /** Количество неудачных запросов */
  errorCount: number;
  /** Средняя задержка в миллисекундах */
  avgLatencyMs: number;
  /** Запросы, сгруппированные по модели */
  requestsByModel: Record<string, number>;
  /** Запросы, сгруппированные по провайдеру */
  requestsByProvider: Record<string, number>;
}

// v2 Types - Rate Limiting
/**
 * Конфигурация rate limit для API ключа
 */
export interface RateLimitConfig {
  /** Максимум запросов в минуту */
  requestsPerMinute: number;
  /** Максимум токенов в минуту */
  tokensPerMinute: number;
  /** Максимум запросов в день */
  requestsPerDay: number;
  /** Максимум ежедневных расходов в долларах (опционально) */
  maxDailyCost?: number;
}

/**
 * Текущий статус rate limit для API ключа
 */
export interface RateLimitStatus {
  /** Запросов сделано за эту минуту */
  requestsThisMinute: number;
  /** Запросов сделано сегодня */
  requestsThisDay: number;
  /** Токенов использовано за эту минуту */
  tokensThisMinute: number;
  /** В данный момент ограничен ли ключ rate limit */
  isRateLimited: boolean;
  /** Время сброса лимитов (опционально) */
  resetAt?: string;
}

// v2 Types - Budget Management
/**
 * Конфигурация бюджета для сущности (ApiKey или Team)
 */
export interface Budget {
  /** Идентификатор бюджета */
  id: string;
  /** Идентификатор сущности (ID ApiKey или ID Team) */
  entityId: string;
  /** Тип сущности */
  entityType: 'ApiKey' | 'Team';
  /** Максимальная сумма бюджета */
  budgetAmount: number;
  /** Текущие расходы */
  currentSpending: number;
  /** Действие при превышении бюджета */
  limitAction: 'warn' | 'block';
  /** Дата начала периода бюджета */
  periodStart?: string;
  /** Дата конца периода бюджета */
  periodEnd?: string;
  /** Временная метка создания */
  createdAt: string;
  /** Временная метка последнего обновления */
  updatedAt: string;
}

/**
 * Результат проверки бюджета
 */
export interface BudgetCheckResult {
  /** Максимальная сумма бюджета */
  budgetAmount: number;
  /** Текущие расходы */
  currentSpending: number;
  /** Оставшийся бюджет */
  remainingBudget: number;
  /** Следует ли блокировать запросы */
  shouldBlock: boolean;
  /** Процент использованного бюджета */
  percentageUsed: number;
}

/**
 * Запрос на установку/обновление бюджета
 */
export interface SetBudgetRequest {
  /** Сумма бюджета */
  budgetAmount: number;
  /** Действие при превышении */
  limitAction: 'warn' | 'block';
  /** Дата конца периода (опционально) */
  periodEnd?: string;
}

/**
 * Запрос на обновление расходов
 */
export interface UpdateSpendingRequest {
  /** Расходы для добавления */
  cost: number;
}

// v2 Types - Team/Org RBAC
/**
 * Роль участника команды
 */
export type TeamRole = 'Owner' | 'Admin' | 'Member' | 'Viewer';

/**
 * Информация о команде
 */
export interface Team {
  /** Идентификатор команды */
  id: string;
  /** Название команды */
  name: string;
  /** Описание команды */
  description?: string;
  /** ID владельца команды */
  ownerId: string;
  /** Временная метка создания */
  createdAt: string;
  /** Временная метка последнего обновления */
  updatedAt: string;
}

/**
 * Информация об участнике команды
 */
export interface TeamMember {
  /** Идентификатор участника */
  id: string;
  /** Идентификатор команды */
  teamId: string;
  /** Идентификатор пользователя */
  userId: string;
  /** Роль участника */
  role: TeamRole;
  /** Разрешённые модели (опционально) */
  allowedModels?: string[];
  /** Временная метка создания */
  createdAt: string;
}

/**
 * Запрос на создание команды
 */
export interface CreateTeamRequest {
  /** Название команды */
  name: string;
  /** Описание команды (опционально) */
  description?: string;
}

/**
 * Запрос на добавление участника команды
 */
export interface AddTeamMemberRequest {
  /** ID пользователя для добавления */
  userId: string;
  /** Роль участника */
  role: TeamRole;
  /** Разрешённые модели (опционально) */
  allowedModels?: string[];
}

// v2 Chat types
/**
 * Сообщение чата
 */
export interface ChatMessage {
  /** Роль отправителя */
  role: 'system' | 'user' | 'assistant' | 'tool';
  /** Содержание сообщения */
  content: string;
  /** Опциональное имя отправителя */
  name?: string;
}

/**
 * Запрос на завершение чата
 */
export interface ChatCompletionRequest {
  /** Название модели */
  model: string;
  /** Сообщения чата */
  messages: ChatMessage[];
  /** Включить потоковый ответ */
  stream?: boolean;
  /** Температура генерации (0-2) */
  temperature?: number;
  /** Максимальное количество токенов */
  max_tokens?: number;
  /** Стоп-последовательности генерации */
  stop?: string[];
  /** Дополнительные свойства */
  [key: string]: unknown;
}