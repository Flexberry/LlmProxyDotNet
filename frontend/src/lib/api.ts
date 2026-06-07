import type { 
  ApiKey, 
  CreateApiKeyRequest, 
  CreateApiKeyResponse, 
  ModelsListResponse,
  LogStats,
  ChatCompletionRequest,
  RateLimitStatus,
  Budget,
  BudgetCheckResult,
  SetBudgetRequest,
  UpdateSpendingRequest,
  Team,
  TeamMember,
  TeamRole,
  CreateTeamRequest,
  AddTeamMemberRequest
} from './types';

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:4000';

/**
 * Выполняет HTTP запрос к backend API
 * @template T - Ожидаемый тип ответа
 * @param endpoint - Путь API endpoint
 * @param options - Опции fetch
 * @returns Promise с данными ответа
 * @throws Error при HTTP ошибках
 */
export async function fetchBackend<T>(
  endpoint: string, 
  options: RequestInit = {}
): Promise<T> {
  const headers = new Headers(options.headers);
  
  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  } else if (!headers.has('Content-Type') && options.method !== 'GET') {
    headers.set('Content-Type', 'application/json');
  }

  try {
    const response = await fetch(`${BACKEND_URL}${endpoint}`, {
      ...options,
      headers,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.error || `HTTP ${response.status}: ${response.statusText}`);
    }

    const contentLength = response.headers?.get('content-length');
    if (response.status === 204 || contentLength === '0') {
      return {} as T;
    }

    return response.json();
  } catch (error) {
    console.error(`API Error [${endpoint}]:`, error);
    throw error;
  }
}

// Серверные API функции для административных операций
// Эти функции вызывают server-side API route (/api/admin), который сам добавляет ADMIN_SECRET
export async function fetchAdmin<T>(
  endpoint: string,
  method: 'GET' | 'POST' | 'DELETE' = 'GET',
  body?: unknown
): Promise<T> {
  try {
    // Для клиентского кода не добавляем Authorization header — это делает server-side API route
    const headers: HeadersInit = { 
      'Content-Type': 'application/json',
    };
    
    // Для GET и DELETE используем query params, для POST — JSON body
    if (method === 'GET' || method === 'DELETE') {
      const params = new URLSearchParams({ endpoint });
      const response = await fetch(`/api/admin?${params}`, {
        method,
        headers,
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        throw new Error(error.error || `HTTP ${response.status}`);
      }

      // DELETE может возвращать пустой ответ (204)
      if (method === 'DELETE') {
        const contentLength = response.headers?.get("content-length");
        if (response.status === 204 || contentLength === "0") {
          return {} as T;
        }
      }

      return response.json();
    } else {
      // POST с JSON body
      const response = await fetch(`/api/admin`, {
        method,
        headers,
        body: JSON.stringify({ endpoint, body }),
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        throw new Error(error.error || `HTTP ${response.status}`);
      }

      return response.json();
    }
  } catch (error) {
    console.error(`Admin API Error [${endpoint}]:`, error);
    throw error;
  }
}

/**
 * Получает все API ключи
 * @returns Promise с массивом API ключей
 */
export async function listApiKeys(): Promise<ApiKey[]> {
  return fetchAdmin<ApiKey[]>('/admin/keys', 'GET');
}

/**
 * Создаёт новый API ключ
 * @param data - Данные для создания ключа
 * @returns Promise с созданным ключом (с plaintext)
 */
export async function createApiKey(data: CreateApiKeyRequest): Promise<CreateApiKeyResponse> {
  const payload = {
    ...data,
    expiresAt: data.expiresAt ? new Date(data.expiresAt).toISOString() : undefined,
  };
  return fetchAdmin<CreateApiKeyResponse>('/admin/keys', 'POST', payload);
}

/**
 * Отозывает API ключ по ID
 * @param keyId - Уникальный идентификатор ключа
 * @returns Promise, разрешающийся после отзыва ключа
 */
export async function revokeApiKey(keyId: string): Promise<void> {
  return fetchAdmin<void>(`/admin/keys/${keyId}`, 'DELETE');
}

/**
 * Получает список доступных моделей
 * @returns Promise с ответом списка моделей
 */
export async function listModels(): Promise<ModelsListResponse> {
  return fetchBackend<ModelsListResponse>('/v1/models');
}

/**
 * Получает статистику использования за период
 * @param from - Дата начала периода
 * @param to - Дата конца периода
 * @returns Promise со статистикой
 */
export async function getStats(from: Date, to: Date): Promise<LogStats> {
  const params = new URLSearchParams({ 
    endpoint: '/admin/stats',
    from: from.toISOString(), 
    to: to.toISOString() 
  });
  return fetchAdmin<LogStats>(`/api/admin?${params}`, 'GET');
}

/**
 * v2: API для Rate Limiting
 */

/**
 * Получает статус rate limit для API ключа
 * @param apiKeyHash - Хеш API ключа
 * @returns Текущий статус rate limit
 */
export async function getRateLimitStatus(apiKeyHash: string): Promise<RateLimitStatus> {
  return fetchAdmin<RateLimitStatus>(`/admin/ratelimits/${apiKeyHash}`, 'GET');
}

/**
 * v2: API для Budget Management
 */

/**
 * Получает бюджет для сущности
 * @param entityType - Тип сущности ('ApiKey' или 'Team')
 * @param entityId - Идентификатор сущности
 * @returns Информация о бюджете
 */
export async function getBudget(entityType: 'ApiKey' | 'Team', entityId: string): Promise<Budget | null> {
  try {
    return await fetchAdmin<Budget>(`/admin/budgets/${entityType}/${entityId}`, 'GET');
  } catch (error) {
    // Возвращаем null, если бюджет не существует
    return null;
  }
}

/**
 * Устанавливает или обновляет бюджет для сущности
 * @param entityType - Тип сущности ('ApiKey' или 'Team')
 * @param entityId - Идентификатор сущности
 * @param request - Настройки бюджета
 * @returns Обновлённый бюджет
 */
export async function setBudget(entityType: 'ApiKey' | 'Team', entityId: string, request: SetBudgetRequest): Promise<Budget> {
  return fetchAdmin<Budget>(`/admin/budgets/${entityType}/${entityId}`, 'POST', request);
}

/**
 * Проверяет статус бюджета для сущности
 * @param entityType - Тип сущности ('ApiKey' или 'Team')
 * @param entityId - Идентификатор сущности
 * @returns Результат проверки бюджета
 */
export async function checkBudget(entityType: 'ApiKey' | 'Team', entityId: string): Promise<BudgetCheckResult> {
  return fetchAdmin<BudgetCheckResult>(`/admin/budgets/${entityType}/${entityId}/check`, 'GET');
}

/**
 * Обновляет расходы для сущности
 * @param entityType - Тип сущности ('ApiKey' или 'Team')
 * @param entityId - Идентификатор сущности
 * @param request - Запрос на обновление расходов
 * @returns Обновлённый результат проверки бюджета
 */
export async function updateSpending(entityType: 'ApiKey' | 'Team', entityId: string, request: UpdateSpendingRequest): Promise<BudgetCheckResult> {
  return fetchAdmin<BudgetCheckResult>(`/admin/budgets/${entityType}/${entityId}/spending`, 'POST', request);
}

/**
 * v2: API для Team/Org RBAC
 */

/**
 * Создаёт новую команду
 * @param request - Запрос на создание команды
 * @returns Созданная команда
 */
export async function createTeam(request: CreateTeamRequest): Promise<Team> {
  return fetchAdmin<Team>('/admin/teams', 'POST', request);
}

/**
 * Получает команду по ID
 * @param teamId - Идентификатор команды
 * @returns Информация о команде
 */
export async function getTeam(teamId: string): Promise<Team> {
  return fetchAdmin<Team>(`/admin/teams/${teamId}`, 'GET');
}

/**
 * Получает все команды для текущего пользователя
 * @returns Массив команд
 */
export async function getUserTeams(): Promise<Team[]> {
  return fetchAdmin<Team[]>('/admin/teams', 'GET');
}

/**
 * Добавляет участника в команду
 * @param teamId - Идентификатор команды
 * @param request - Запрос на добавление участника
 * @returns Добавленный участник команды
 */
export async function addTeamMember(teamId: string, request: AddTeamMemberRequest): Promise<TeamMember> {
  return fetchAdmin<TeamMember>(`/admin/teams/${teamId}/members`, 'POST', request);
}

/**
 * Удаляет участника из команды
 * @param teamId - Идентификатор команды
 * @param userId - Идентификатор пользователя
 */
export async function removeTeamMember(teamId: string, userId: string): Promise<void> {
  return fetchAdmin<void>(`/admin/teams/${teamId}/members/${userId}`, 'DELETE');
}

/**
 * Получает роль пользователя в команде
 * @param teamId - Идентификатор команды
 * @param userId - Идентификатор пользователя
 * @returns Информация об участнике команды
 */
export async function getUserRole(teamId: string, userId: string): Promise<TeamMember> {
  return fetchAdmin<TeamMember>(`/admin/teams/${teamId}/members/${userId}/role`, 'GET');
}

/**
 * Удаляет команду
 * @param teamId - Идентификатор команды
 */
export async function deleteTeam(teamId: string): Promise<void> {
  return fetchAdmin<void>(`/admin/teams/${teamId}`, 'DELETE');
}

/**
 * Создаёт завершение чата через LLM провайдера
 * @param request - Запрос на завершение чата
 * @param userApiKey - API ключ пользователя
 * @param onChunk - Callback для каждого фрагмента потока
 * @returns Promise с ответом или void для потоковых запросов
 */
export async function createChatCompletion(
  request: ChatCompletionRequest,
  userApiKey: string,
  onChunk?: (chunk: any) => void
): Promise<any> {
  if (request.stream) {
    if (!onChunk) {
      throw new Error('stream:true требует callback onChunk');
    }
    return streamChatCompletion(request, userApiKey, onChunk);
  }

  const response = await fetch(`${BACKEND_URL}/v1/chat/completions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${userApiKey}`,
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.error || `HTTP ${response.status}`);
  }

  return response.json();
}

/**
 * Создаёт потоковое завершение чата
 * @param request - Запрос на завершение чата
 * @param userApiKey - API ключ пользователя
 * @param onChunk - Callback для каждого фрагмента потока
 * @returns Promise, разрешающийся после завершения потоковой передачи
 */
async function streamChatCompletion(
  request: ChatCompletionRequest,
  userApiKey: string,
  onChunk: (chunk: unknown) => void
): Promise<void> {
  const response = await fetch(`${BACKEND_URL}/api/proxy`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({ 
      path: '/v1/chat/completions',
      method: 'POST',
      body: { ...request, stream: true },
      headers: {
        'Authorization': `Bearer ${userApiKey}`
      }
    }),
  });

  if (!response.ok || !response.body) {
    throw new Error('Не удалось запустить потоковую передачу');
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;

    buffer += decoder.decode(value, { stream: true });
    
    const lines = buffer.split('\n');
    buffer = lines.pop() || ''; 

    for (const line of lines) {
      if (line.startsWith('data: ')) {
        const data = line.slice(6).trim();
        if (data === '[DONE]') continue;
        if (data) {
          try {
            const chunk = JSON.parse(data);
            onChunk(chunk);
          } catch (e) {
            console.warn('Не удалось обработать фрагмент:', data);
          }
        }
      }
    }
  }
}

export type { ApiKey, CreateApiKeyRequest, CreateApiKeyResponse, LogStats, ChatCompletionRequest, ModelsListResponse, RateLimitConfig, RateLimitStatus, Budget, BudgetCheckResult, SetBudgetRequest, UpdateSpendingRequest, Team, TeamMember, TeamRole, CreateTeamRequest, AddTeamMemberRequest } from './types';