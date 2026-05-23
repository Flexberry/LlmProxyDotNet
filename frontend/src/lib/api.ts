// frontend/lib/api.ts
import { 
  ApiKey, 
  CreateApiKeyRequest, 
  CreateApiKeyResponse, 
  ModelsListResponse,
  LogStats,
  ChatCompletionRequest
} from './types';

const BACKEND_URL = process.env.NEXT_PUBLIC_BACKEND_URL || 'http://localhost:4000';
const MASTER_KEY = process.env.NEXT_PUBLIC_LITELLM_MASTER_KEY; 

async function fetchBackend<T>(
  endpoint: string, 
  options: RequestInit = {}
): Promise<T> {
  const headers = new Headers(options.headers);
  
  // Теперь MASTER_KEY будет определен в браузере
  if (MASTER_KEY) {
    headers.set('X-Admin-Key', MASTER_KEY);
  }
  
  // Добавляем Content-Type только если есть тело запроса или это не GET
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

      // Обработка пустых ответов (например, 204 No Content при удалении)
      if (response.status === 204 || response.headers.get("content-length") === "0") {
          return {} as T;
      }

      return response.json();
  } catch (error) {
      console.error(`API Error [${endpoint}]:`, error);
      throw error;
  }
}
// === API Keys ===

export async function listApiKeys(): Promise<ApiKey[]> {
  return fetchBackend<ApiKey[]>('/admin/keys');
}

export async function createApiKey(data: CreateApiKeyRequest): Promise<CreateApiKeyResponse> {
  return fetchBackend<CreateApiKeyResponse>('/admin/keys', {
    method: 'POST',
    body: JSON.stringify(data),
  });
}

export async function revokeApiKey(keyId: string): Promise<void> {
  return fetchBackend<void>(`/admin/keys/${keyId}`, {
    method: 'DELETE',
  });
}

// === Models ===

export async function listModels(): Promise<ModelsListResponse> {
  return fetchBackend<ModelsListResponse>('/v1/models');
}

// === Stats ===

export async function getStats(from: Date, to: Date): Promise<LogStats> {
  const params = new URLSearchParams({
    from: from.toISOString(),
    to: to.toISOString(),
  });
  return fetchBackend<LogStats>(`/admin/stats?${params}`);
}

// === Chat Completion (через прокси для CORS) ===

export async function createChatCompletion(
  request: ChatCompletionRequest,
  userApiKey: string,
  onChunk?: (chunk: any) => void
): Promise<any> {
  if (request.stream && onChunk) {
    // Streaming через SSE
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
    
    // Парсинг SSE: data: {...}\n\n
    const lines = buffer.split('\n');
    buffer = lines.pop() || ''; // Сохраняем неполную строку

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