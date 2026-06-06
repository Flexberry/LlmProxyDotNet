// frontend/src/lib/api.ts
import { 
  ApiKey, 
  CreateApiKeyRequest, 
  CreateApiKeyResponse, 
  ModelsListResponse,
  LogStats,
  ChatCompletionRequest
} from './types';

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:4000';

// Убираем MASTER_KEY из клиентского кода - теперь используется server-side API route

export async function fetchBackend<T>(
  endpoint: string, 
  options: RequestInit = {}
): Promise<T> {
  const headers = new Headers(options.headers);
  
  // Убираем client-side добавление X-Admin-Key
  // Административные запросы должны идти через server-side API route
  
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

      // ИСПРАВЛЕНИЕ: Безопасная проверка headers
      const contentLength = response.headers?.get("content-length");
      if (response.status === 204 || contentLength === "0") {
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
  body?: any
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

export async function listApiKeys(): Promise<ApiKey[]> {
  // Используем server-side API route для админ-операций
  return fetchAdmin<ApiKey[]>('/admin/keys', 'GET');
}

export async function createApiKey(data: CreateApiKeyRequest): Promise<CreateApiKeyResponse> {
  // Преобразуем expiresAt в ISO формат с timezone, если он указан
  const payload = {
    ...data,
    expiresAt: data.expiresAt ? new Date(data.expiresAt).toISOString() : undefined,
  };
  // Используем server-side API route для админ-операций
  return fetchAdmin<CreateApiKeyResponse>('/admin/keys', 'POST', payload);
}

export async function revokeApiKey(keyId: string): Promise<void> {
  // Используем server-side API route для админ-операций
  return fetchAdmin<void>(`/admin/keys/${keyId}`, 'DELETE');
}

export async function listModels(): Promise<ModelsListResponse> {
  return fetchBackend<ModelsListResponse>('/v1/models');
}

export async function getStats(from: Date, to: Date): Promise<LogStats> {
  const params = new URLSearchParams({
    from: from.toISOString(),
    to: to.toISOString(),
  });
  return fetchBackend<LogStats>(`/admin/stats?${params}`);
}

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
      'Authorization': `Bearer ${userApiKey}`,
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.error || `HTTP ${response.status}`);
  }

  return response.json();
}

async function streamChatCompletion(
  request: ChatCompletionRequest,
  userApiKey: string,
  onChunk: (chunk: any) => void
): Promise<void> {
  const response = await fetch(`${BACKEND_URL}/v1/chat/completions`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${userApiKey}`,
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

export type { ApiKey, CreateApiKeyRequest, CreateApiKeyResponse, LogStats, ChatCompletionRequest, ModelsListResponse } from './types';