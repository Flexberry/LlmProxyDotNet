import { z } from 'zod';
import type { ApiKey, CreateApiKeyRequest, ChatCompletionRequest } from '@/lib/types';

describe('Type Definitions', () => {
  it('ApiKey type has required fields', () => {
    // TypeScript type checking happens at compile time,
    // but we can verify the structure through usage
    const key: ApiKey = {
      id: 'test-id',
      keyHash: 'abc123',
      permissions: '*',
      isActive: true,
      createdAt: '2024-01-01',
    };
    
    expect(key.id).toBe('test-id');
    expect(key.permissions).toBe('*');
  });

  it('CreateApiKeyRequest accepts optional fields', () => {
    const request: CreateApiKeyRequest = {
      name: 'My Key',
      permissions: ['model1', 'model2'],
      expiresAt: '2024-12-31',
    };
    
    expect(request.name).toBe('My Key');
    expect(request.permissions).toEqual(['model1', 'model2']);
  });

  it('ChatCompletionRequest matches OpenAI spec', () => {
    const request: ChatCompletionRequest = {
      model: 'openai/gpt-4',
      messages: [
        { role: 'system', content: 'You are helpful' },
        { role: 'user', content: 'Hello' },
      ],
      stream: true,
      temperature: 0.7,
      max_tokens: 100,
    };
    
    expect(request.model).toBe('openai/gpt-4');
    expect(request.messages).toHaveLength(2);
    expect(request.stream).toBe(true);
  });
});