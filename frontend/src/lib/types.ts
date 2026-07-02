// frontend/lib/types.ts

export interface ApiKey {
  id: string;
  keyHash: string;
  key?: string; // Plaintext ключ (только для newly created keys, не сохраняется в БД)
  name?: string;
  permissions: string; // JSON string или "*"
  expiresAt?: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateApiKeyRequest {
  name?: string;
  permissions?: string[]; // ["*"] или ["ollama/llama3", "openai/gpt-4"]
  expiresAt?: string;
}

export interface CreateApiKeyResponse {
  key: string; // Plaintext ключ (показывается только один раз!)
  apiKey: ApiKey;
}

export interface ModelInfo {
  id: string;
  object: string;
  created: number;
  owned_by: string;
}

export interface ModelsListResponse {
  object: string;
  data: ModelInfo[];
}

export interface LogStats {
  totalRequests: number;
  successCount: number;
  errorCount: number;
  avgLatencyMs: number;
  totalPromptTokens: number;
  totalCompletionTokens: number;
  requestsByModel: Record<string, number>;
  requestsByProvider: Record<string, number>;
}

export interface ChatMessage {
  role: 'system' | 'user' | 'assistant' | 'tool';
  content: string;
  name?: string;
}

export interface ChatCompletionRequest {
  model: string;
  messages: ChatMessage[];
  stream?: boolean;
  temperature?: number;
  max_tokens?: number;
  stop?: string[];
}