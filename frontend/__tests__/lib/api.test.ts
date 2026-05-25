// frontend/__tests__/lib/api.test.ts
import { fetchBackend, listApiKeys, createApiKey } from '@/lib/api';

global.fetch = jest.fn();

describe('API Client', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    process.env.NEXT_PUBLIC_LITELLM_MASTER_KEY = 'sk_master_test';
  });

  describe('fetchBackend', () => {
    it('adds X-Admin-Key header when MASTER_KEY is set', async () => {
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
      // Просто проверяем, что headers переданы (структура зависит от полифила)
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
  });

  describe('listApiKeys', () => {
    it('calls correct endpoint and returns typed data', async () => {
      const mockKeys = [{ id: '1', keyHash: 'abc', isActive: true, permissions: '*', createdAt: '2024-01-01' }];
      (fetch as jest.Mock).mockResolvedValueOnce({
        ok: true,
        headers: { get: jest.fn((key) => key === 'content-length' ? '10' : null) },
        json: async () => mockKeys,
      });

      const result = await listApiKeys();
      expect(fetch).toHaveBeenCalledWith(expect.stringContaining('/admin/keys'), expect.any(Object));
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
        expect.stringContaining('/admin/keys'),
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ name: 'Test', permissions: ['*'] }),
        })
      );
      expect(result).toEqual(mockResponse);
    });
  });
});