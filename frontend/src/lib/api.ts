import type { 
  ApiKey, 
  CreateApiKeyRequest, 
  CreateApiKeyResponse, 
  ModelsListResponse,
  LogStats,
  ChatCompletionRequest
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

/**
 * Makes an administrative request via server-side API route
 * @template T - Expected response type
 * @param endpoint - Administrative endpoint path
 * @param method - HTTP method (GET, POST, DELETE)
 * @param body - Request body
 * @returns Promise resolving to the response data
 */
export async function fetchAdmin<T>(
  endpoint: string,
  method: 'GET' | 'POST' | 'DELETE' = 'GET',
  body?: unknown
): Promise<T> {
  try {
    const response = await fetch('/api/admin', {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ endpoint, body }),
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.error || `HTTP ${response.status}`);
    }

    return response.json();
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
  const params = new URLSearchParams({
    from: from.toISOString(),
    to: to.toISOString(),
  });
  return fetchBackend<LogStats>(`/admin/stats?${params}`);
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
  onChunk?: (chunk: unknown) => void
): Promise<unknown> {
  if (request.stream) {
    if (onChunk) {
      return streamChatCompletion(request, userApiKey, onChunk);
    } else {
      // If stream=true without onChunk, use server-side handler
      return fetchAdmin<unknown>('/proxy/chat', 'POST', {
        path: '/v1/chat/completions',
        body: { ...request, stream: true },
        headers: { Authorization: `Bearer ${userApiKey}` },
      });
    }
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

export type { ApiKey, CreateApiKeyRequest, CreateApiKeyResponse, LogStats, ChatCompletionRequest, ModelsListResponse } from './types';