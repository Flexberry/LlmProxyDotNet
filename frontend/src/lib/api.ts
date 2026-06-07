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
 * Makes an HTTP request to the backend API
 * @template T - Expected response type
 * @param endpoint - API endpoint path
 * @param options - Fetch options
 * @returns Promise resolving to the response data
 * @throws Error on HTTP errors
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
 * Retrieves all API keys
 * @returns Promise resolving to array of API keys
 */
export async function listApiKeys(): Promise<ApiKey[]> {
  return fetchAdmin<ApiKey[]>('/admin/keys', 'GET');
}

/**
 * Creates a new API key
 * @param data - Data for key creation
 * @returns Promise resolving to the created key with plaintext
 */
export async function createApiKey(data: CreateApiKeyRequest): Promise<CreateApiKeyResponse> {
  const payload = {
    ...data,
    expiresAt: data.expiresAt ? new Date(data.expiresAt).toISOString() : undefined,
  };
  return fetchAdmin<CreateApiKeyResponse>('/admin/keys', 'POST', payload);
}

/**
 * Revokes an API key by ID
 * @param keyId - Unique key identifier
 * @returns Promise resolving when key is revoked
 */
export async function revokeApiKey(keyId: string): Promise<void> {
  return fetchAdmin<void>(`/admin/keys/${keyId}`, 'DELETE');
}

/**
 * Retrieves list of available models
 * @returns Promise resolving to models list response
 */
export async function listModels(): Promise<ModelsListResponse> {
  return fetchBackend<ModelsListResponse>('/v1/models');
}

/**
 * Retrieves usage statistics for a period
 * @param from - Start date of the period
 * @param to - End date of the period
 * @returns Promise resolving to statistics
 */
export async function getStats(from: Date, to: Date): Promise<LogStats> {
  // Pass endpoint without query params, and let fetchAdmin handle query params
  return fetchAdmin<LogStats>('/admin/stats', 'GET', { from: from.toISOString(), to: to.toISOString() });
}

/**
 * v2: Rate Limiting APIs
 */

/**
 * Get rate limit status for an API key
 * @param apiKeyHash - Hash of the API key
 * @returns Current rate limit status
 */
export async function getRateLimitStatus(apiKeyHash: string): Promise<RateLimitStatus> {
  return fetchAdmin<RateLimitStatus>(`/admin/ratelimits/${apiKeyHash}`, 'GET');
}

/**
 * v2: Budget Management APIs
 */

/**
 * Get budget for an entity
 * @param entityType - Type of entity ('ApiKey' or 'Team')
 * @param entityId - Entity identifier
 * @returns Budget information
 */
export async function getBudget(entityType: 'ApiKey' | 'Team', entityId: string): Promise<Budget | null> {
  try {
    return await fetchAdmin<Budget>(`/admin/budgets/${entityType}/${entityId}`, 'GET');
  } catch (error) {
    // Return null if budget doesn't exist
    return null;
  }
}

/**
 * Set or update budget for an entity
 * @param entityType - Type of entity ('ApiKey' or 'Team')
 * @param entityId - Entity identifier
 * @param request - Budget settings
 * @returns Updated budget
 */
export async function setBudget(entityType: 'ApiKey' | 'Team', entityId: string, request: SetBudgetRequest): Promise<Budget> {
  return fetchAdmin<Budget>(`/admin/budgets/${entityType}/${entityId}`, 'POST', request);
}

/**
 * Check budget status for an entity
 * @param entityType - Type of entity ('ApiKey' or 'Team')
 * @param entityId - Entity identifier
 * @returns Budget check result
 */
export async function checkBudget(entityType: 'ApiKey' | 'Team', entityId: string): Promise<BudgetCheckResult> {
  return fetchAdmin<BudgetCheckResult>(`/admin/budgets/${entityType}/${entityId}/check`, 'GET');
}

/**
 * Update spending for an entity
 * @param entityType - Type of entity ('ApiKey' or 'Team')
 * @param entityId - Entity identifier
 * @param request - Spending update request
 * @returns Updated budget check result
 */
export async function updateSpending(entityType: 'ApiKey' | 'Team', entityId: string, request: UpdateSpendingRequest): Promise<BudgetCheckResult> {
  return fetchAdmin<BudgetCheckResult>(`/admin/budgets/${entityType}/${entityId}/spending`, 'POST', request);
}

/**
 * v2: Team/Org RBAC APIs
 */

/**
 * Create a new team
 * @param request - Team creation request
 * @returns Created team
 */
export async function createTeam(request: CreateTeamRequest): Promise<Team> {
  return fetchAdmin<Team>('/admin/teams', 'POST', request);
}

/**
 * Get a team by ID
 * @param teamId - Team identifier
 * @returns Team information
 */
export async function getTeam(teamId: string): Promise<Team> {
  return fetchAdmin<Team>(`/admin/teams/${teamId}`, 'GET');
}

/**
 * Get all teams for the current user
 * @returns Array of teams
 */
export async function getUserTeams(): Promise<Team[]> {
  return fetchAdmin<Team[]>('/admin/teams', 'GET');
}

/**
 * Add a member to a team
 * @param teamId - Team identifier
 * @param request - Member addition request
 * @returns Added team member
 */
export async function addTeamMember(teamId: string, request: AddTeamMemberRequest): Promise<TeamMember> {
  return fetchAdmin<TeamMember>(`/admin/teams/${teamId}/members`, 'POST', request);
}

/**
 * Remove a member from a team
 * @param teamId - Team identifier
 * @param userId - User identifier
 */
export async function removeTeamMember(teamId: string, userId: string): Promise<void> {
  return fetchAdmin<void>(`/admin/teams/${teamId}/members/${userId}`, 'DELETE');
}

/**
 * Get user's role in a team
 * @param teamId - Team identifier
 * @param userId - User identifier
 * @returns Team member information
 */
export async function getUserRole(teamId: string, userId: string): Promise<TeamMember> {
  return fetchAdmin<TeamMember>(`/admin/teams/${teamId}/members/${userId}/role`, 'GET');
}

/**
 * Delete a team
 * @param teamId - Team identifier
 */
export async function deleteTeam(teamId: string): Promise<void> {
  return fetchAdmin<void>(`/admin/teams/${teamId}`, 'DELETE');
}

/**
 * Creates a chat completion through LLM provider
 * @param request - Chat completion request
 * @param userApiKey - User's API key
 * @param onChunk - Callback for each stream chunk
 * @returns Promise resolving to response or void for streaming
 */
export async function createChatCompletion(
  request: ChatCompletionRequest,
  userApiKey: string,
  onChunk?: (chunk: any) => void
): Promise<any> {
  if (request.stream) {
    if (!onChunk) {
      throw new Error('stream:true requires onChunk callback');
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
 * Creates a streaming chat completion
 * @param request - Chat completion request
 * @param userApiKey - User's API key
 * @param onChunk - Callback for each stream chunk
 * @returns Promise resolving when streaming is complete
 */
async function streamChatCompletion(
  request: ChatCompletionRequest,
  userApiKey: string,
  onChunk: (chunk: unknown) => void
): Promise<void> {
  const response = await fetch(`${BACKEND_URL}/v1/chat/completions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${userApiKey}`,
    },
    body: JSON.stringify({ ...request, stream: true }),
  });

  if (!response.ok || !response.body) {
    throw new Error('Failed to start streaming');
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
            console.warn('Failed to parse chunk:', data);
          }
        }
      }
    }
  }
}

export type { ApiKey, CreateApiKeyRequest, CreateApiKeyResponse, LogStats, ChatCompletionRequest, ModelsListResponse, RateLimitConfig, RateLimitStatus, Budget, BudgetCheckResult, SetBudgetRequest, UpdateSpendingRequest, Team, TeamMember, TeamRole, CreateTeamRequest, AddTeamMemberRequest } from './types';