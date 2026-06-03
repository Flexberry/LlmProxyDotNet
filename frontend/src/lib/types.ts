/**
 * API key for client authentication
 */
export interface ApiKey {
  /** Unique identifier for the key */
  id: string;
  /** SHA256 hash of the key */
  keyHash: string;
  /** Plain text key (only for newly created keys, not stored in DB) */
  key?: string;
  /** Human-readable name for the key */
  name?: string;
  /** Allowed models (JSON string or "*" for all) */
  permissions: string;
  /** Expiration date for the key */
  expiresAt?: string;
  /** Active status of the key */
  isActive: boolean;
  /** Creation date of the key */
  createdAt: string;
}

/**
 * Request to create a new API key
 */
export interface CreateApiKeyRequest {
  /** Name for the key */
  name?: string;
  /** Allowed models (["*"] or ["ollama/llama3", "openai/gpt-4"]) */
  permissions?: string[];
  /** Expiration date */
  expiresAt?: string;
}

/**
 * Response for new API key creation
 */
export interface CreateApiKeyResponse {
  /** Plain text key (shown only once!) */
  key: string;
  /** Created API key object */
  apiKey: ApiKey;
}

/**
 * Model information
 */
export interface ModelInfo {
  /** Unique model identifier */
  id: string;
  /** Object type (always "model") */
  object: string;
  /** Creation timestamp */
  created: number;
  /** Model owner */
  owned_by: string;
}

/**
 * Response for models list request
 */
export interface ModelsListResponse {
  /** Object type (always "list") */
  object: string;
  /** List of models */
  data: ModelInfo[];
}

/**
 * LLM Proxy usage statistics
 */
export interface LogStats {
  /** Total number of requests */
  totalRequests: number;
  /** Number of successful requests */
  successCount: number;
  /** Number of failed requests */
  errorCount: number;
  /** Average latency in milliseconds */
  avgLatencyMs: number;
  /** Total prompt tokens used */
  totalPromptTokens: number;
  /** Total completion tokens used */
  totalCompletionTokens: number;
  /** Requests grouped by model */
  requestsByModel: Record<string, number>;
  /** Requests grouped by provider */
  requestsByProvider: Record<string, number>;
}

/**
 * Chat message
 */
export interface ChatMessage {
  /** Sender role */
  role: 'system' | 'user' | 'assistant' | 'tool';
  /** Message content */
  content: string;
  /** Optional sender name */
  name?: string;
}

/**
 * Chat completion request
 */
export interface ChatCompletionRequest {
  /** Model name */
  model: string;
  /** Chat messages */
  messages: ChatMessage[];
  /** Enable streaming response */
  stream?: boolean;
  /** Generation temperature (0-2) */
  temperature?: number;
  /** Maximum token count */
  max_tokens?: number;
  /** Generation stop sequences */
  stop?: string[];
}