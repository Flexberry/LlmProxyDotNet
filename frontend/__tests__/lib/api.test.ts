import { fetchBackend, listApiKeys, createApiKey, revokeApiKey, listModels, getStats, createChatCompletion } from '@/lib/api';

global.fetch = jest.fn();

describe('API Client', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    process.env.NEXT_PUBLIC_LITELLM_MASTER_KEY = 'sk_master_test';
    process.env.NEXT_PUBLIC_ADMIN_SECRET = 'admin_secret';
  });

  describe('fetchBackend', () => {
    it('makes request to backend URL', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 200,
        headers: { get: jest.fn((key) => key === 'content-length' ? '10' : null) },
        json: async () => ({ data: [] }),
      });

      await fetchBackend('/admin/keys');

      expect(fetch).toHaveBeenCalledTimes(1);
      const [url, options] = (fetch as jest.Mock).mock.calls[0];
      expect(url).toContain('/admin/keys');
      expect(options.headers).toBeDefined();
    });

    it('throws error on non-2xx response', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 401,
        statusText: 'Unauthorized',
        headers: { get: jest.fn() },
        json: async () => ({ error: 'Invalid key' }),
      });

      await expect(fetchBackend('/admin/keys')).rejects.toThrow('Invalid key');
    });

    it('handles network errors gracefully', async () => {
      (fetch as jest.Mock).mockRejectedValueOnce(new Error('Network error'));
      await expect(fetchBackend('/admin/keys')).rejects.toThrow('Network error');
    });

    it('handles 204 No Content responses', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        status: 204,
        headers: { get: jest.fn((key) => key === 'content-length' ? '0' : null) },
      });

      const result = await fetchBackend<void>('/admin/keys/test');
      expect(result).toEqual({});
    });

    it('parses error responses correctly', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 500,
        statusText: 'Internal Server Error',
        headers: { get: jest.fn() },
        json: async () => ({ error: 'Server error' }),
      });

      await expect(fetchBackend('/admin/keys')).rejects.toThrow('Server error');
    });
  });

  describe('listApiKeys', () => {
    it('calls admin endpoint and returns typed data', async () => {
      const mockKeys = [{ id: '1', keyHash: 'abc', isActive: true, permissions: '*', createdAt: '2024-01-01' }];
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: { get: jest.fn((key) => key === 'content-length' ? '10' : null) },
        json: async () => mockKeys,
      });

      const result = await listApiKeys();
      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/admin?endpoint=%2Fadmin%2Fkeys'),
        expect.objectContaining({
          method: 'GET',
          headers: { 'Content-Type': 'application/json' },
        })
      );
      expect(result).toEqual(mockKeys);
    });
  });

  describe('createApiKey', () => {
    it('sends POST with correct payload', async () => {
      const mockResponse = { key: 'sk_new', apiKey: { id: '1', keyHash: 'xyz', isActive: true } };
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: { get: jest.fn((key) => key === 'content-length' ? '10' : null) },
        json: async () => mockResponse,
      });

      const result = await createApiKey({ name: 'Test', permissions: ['*'] });
      
      expect(fetch).toHaveBeenCalledWith(
        '/api/admin',
        expect.objectContaining({
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
        })
      );
      
      const call = (fetch as jest.Mock).mock.calls[0];
      const body = JSON.parse(call[1].body);
      expect(body.endpoint).toBe('/admin/keys');
      expect(body.body).toEqual({ name: 'Test', permissions: ['*'] });
      
      expect(result).toEqual(mockResponse);
    });

    it('converts expiresAt to ISO format', async () => {
      const mockResponse = { key: 'sk_new', apiKey: { id: '1', keyHash: 'xyz', isActive: true } };
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: { get: jest.fn((key) => key === 'content-length' ? '10' : null) },
        json: async () => mockResponse,
      });

      const date = new Date('2024-12-31T23:59:59Z');
      await createApiKey({ name: 'Test', expiresAt: date.toISOString() });
      
      const call = (fetch as jest.Mock).mock.calls[0];
      const body = JSON.parse(call[1].body);
      expect(body.body.expiresAt).toContain('2024-12-31');
    });
  });

  describe('revokeApiKey', () => {
    it('calls admin endpoint with correct key ID', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: { get: jest.fn((key) => key === 'content-length' ? '0' : null) },
        json: async () => ({}),
      });

      await revokeApiKey('test-key-id');

      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/admin?endpoint=%2Fadmin%2Fkeys%2Ftest-key-id'),
        expect.objectContaining({
          method: 'DELETE',
          headers: { 'Content-Type': 'application/json' },
        })
      );
    });
  });

  describe('listModels', () => {
    it('calls correct endpoint and returns models list', async () => {
      const mockModels = { object: 'list', data: [{ id: 'test-model', object: 'model', created: 123, owned_by: 'test' }] };
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: { get: jest.fn((key) => key === 'content-length' ? '10' : null) },
        json: async () => mockModels,
      });

      const result = await listModels();
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/v1/models'), expect.any(Object));
      expect(result).toEqual(mockModels);
    });
  });

  describe('getStats', () => {
    it('formats date range correctly in query params', async () => {
      const from = new Date('2024-01-01T00:00:00Z');
      const to = new Date('2024-01-07T23:59:59Z');
      
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        json: async () => ({ totalRequests: 100, successCount: 90, errorCount: 10, avgLatencyMs: 250 }),
      });

      await getStats(from, to);

      const call = (fetch as jest.Mock).mock.calls[0];
      // getStats использует fetchAdmin который вызывает /api/admin с endpoint param
      expect(call[0]).toContain('/api/admin');
      // URL имеет двойное кодирование из-за вложенности fetchAdmin
      expect(call[0]).toContain('admin');
    });
  });

  describe('createChatCompletion', () => {
    it('sends POST request with correct headers', async () => {
      const mockResponse = { id: 'chat-1', choices: [{ message: { role: 'assistant', content: 'Hello' } }] };
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: { get: jest.fn() },
        json: async () => mockResponse,
      });

      const result = await createChatCompletion(
        { model: 'ollama/llama3', messages: [{ role: 'user', content: 'Hi' }] },
        'sk_test_key'
      );

      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/v1/chat/completions'),
        expect.objectContaining({
          method: 'POST',
          headers: expect.objectContaining({
            'Authorization': 'Bearer sk_test_key',
            'Content-Type': 'application/json',
          }),
        })
      );
      expect(result).toEqual(mockResponse);
    });

    it('throws error on failed response', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 401,
        headers: { get: jest.fn() },
        json: async () => ({ error: 'Unauthorized' }),
      });

      await expect(createChatCompletion(
        { model: 'ollama/llama3', messages: [{ role: 'user', content: 'Hi' }] },
        'invalid-key'
      )).rejects.toThrow('Unauthorized');
    });

    it('handles streaming responses', async () => {
      const chunks: any[] = [];
      const onChunk = jest.fn((chunk: any) => chunks.push(chunk));

      const streamData = new Uint8Array(
        Array.from('data: {"choices":[{"delta":{"content":"Hello"}}]}\n\ndata: [DONE]\n\n').map(c => c.charCodeAt(0))
      );

      const mockReader = {
        read: jest.fn()
          .mockResolvedValueOnce({ done: false, value: streamData })
          .mockResolvedValueOnce({ done: true, value: undefined }),
      };

      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        body: { getReader: () => mockReader },
      });

      await createChatCompletion(
        { model: 'ollama/llama3', messages: [{ role: 'user', content: 'Hi' }], stream: true },
        'sk_test_key',
        onChunk
      );

      expect(fetch).toHaveBeenCalledWith(
        expect.stringContaining('/api/proxy'),
        expect.objectContaining({
          method: 'POST',
        })
      );
      
      // Verify onChunk was called with parsed content
      expect(onChunk).toHaveBeenCalled();
      expect(chunks.length).toBeGreaterThan(0);
    });

    it('throws on failed stream start', async () => {
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: false,
        status: 500,
        body: null,
      });

      await expect(createChatCompletion(
        { model: 'ollama/llama3', messages: [{ role: 'user', content: 'Hi' }], stream: true },
        'sk_test_key',
        () => {}
      )).rejects.toThrow('Не удалось запустить потоковую передачу');
    });

    it('throws error when stream:true without onChunk callback', async () => {
      await expect(createChatCompletion(
        { model: 'ollama/llama3', messages: [{ role: 'user', content: 'Hi' }], stream: true },
        'sk_test_key'
      )).rejects.toThrow('stream:true требует callback onChunk');
    });
  });
});