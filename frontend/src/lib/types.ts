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
  /** Team ID (v2) */
  teamId?: string;
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
  /** Rate limit configuration (v2) */
  rateLimitConfig?: RateLimitConfig;
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
export interface Model {
  /** Unique model identifier */
  id: string;
  /** Object type (always "model") */
  object: string;
  /** Creation timestamp */
  created?: number;
  /** Model owner */
  owned_by: string;
}

/**
 * Response for models list request
 */
export interface ModelsListResponse {
  data: Model[];
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
  /** Requests grouped by model */
  requestsByModel: Record<string, number>;
  /** Requests grouped by provider */
  requestsByProvider: Record<string, number>;
}

// v2 Types - Rate Limiting
/**
 * Rate limit configuration for an API key
 */
export interface RateLimitConfig {
  /** Maximum requests per minute */
  requestsPerMinute: number;
  /** Maximum tokens per minute */
  tokensPerMinute: number;
  /** Maximum requests per day */
  requestsPerDay: number;
  /** Maximum daily cost in dollars (optional) */
  maxDailyCost?: number;
}

/**
 * Current rate limit status for an API key
 */
export interface RateLimitStatus {
  /** Requests made this minute */
  requestsThisMinute: number;
  /** Requests made today */
  requestsThisDay: number;
  /** Tokens used this minute */
  tokensThisMinute: number;
  /** Whether the key is currently rate limited */
  isRateLimited: boolean;
  /** When the limits reset (optional) */
  resetAt?: string;
}

// v2 Types - Budget Management
/**
 * Budget configuration for an entity (ApiKey or Team)
 */
export interface Budget {
  /** Budget identifier */
  id: string;
  /** Entity identifier (ApiKey ID or Team ID) */
  entityId: string;
  /** Entity type */
  entityType: 'ApiKey' | 'Team';
  /** Maximum budget amount */
  budgetAmount: number;
  /** Current spending */
  currentSpending: number;
  /** Action when budget is exceeded */
  limitAction: 'warn' | 'block';
  /** Budget period start date */
  periodStart?: string;
  /** Budget period end date */
  periodEnd?: string;
  /** Creation timestamp */
  createdAt: string;
  /** Last update timestamp */
  updatedAt: string;
}

/**
 * Budget check result
 */
export interface BudgetCheckResult {
  /** Maximum budget amount */
  budgetAmount: number;
  /** Current spending */
  currentSpending: number;
  /** Remaining budget */
  remainingBudget: number;
  /** Whether requests should be blocked */
  shouldBlock: boolean;
  /** Percentage of budget used */
  percentageUsed: number;
}

/**
 * Request to set/update a budget
 */
export interface SetBudgetRequest {
  /** Budget amount */
  budgetAmount: number;
  /** Action when exceeded */
  limitAction: 'warn' | 'block';
  /** Period end date (optional) */
  periodEnd?: string;
}

/**
 * Request to update spending
 */
export interface UpdateSpendingRequest {
  /** Cost to add */
  cost: number;
}

// v2 Types - Team/Org RBAC
/**
 * Team member role
 */
export type TeamRole = 'Owner' | 'Admin' | 'Member' | 'Viewer';

/**
 * Team information
 */
export interface Team {
  /** Team identifier */
  id: string;
  /** Team name */
  name: string;
  /** Team description */
  description?: string;
  /** Owner user ID */
  ownerId: string;
  /** Creation timestamp */
  createdAt: string;
  /** Last update timestamp */
  updatedAt: string;
}

/**
 * Team member information
 */
export interface TeamMember {
  /** Member identifier */
  id: string;
  /** Team identifier */
  teamId: string;
  /** User identifier */
  userId: string;
  /** Member role */
  role: TeamRole;
  /** Allowed models (optional) */
  allowedModels?: string[];
  /** Creation timestamp */
  createdAt: string;
}

/**
 * Request to create a team
 */
export interface CreateTeamRequest {
  /** Team name */
  name: string;
  /** Team description (optional) */
  description?: string;
}

/**
 * Request to add a team member
 */
export interface AddTeamMemberRequest {
  /** User ID to add */
  userId: string;
  /** Role for the member */
  role: TeamRole;
  /** Allowed models (optional) */
  allowedModels?: string[];
}

// v2 Chat types
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
  /** Additional properties */
  [key: string]: unknown;
}