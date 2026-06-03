import { render, screen, waitFor } from '@testing-library/react';
import DashboardPage from '@/app/page';
import * as api from '@/lib/api';

jest.mock('@/lib/api', () => ({
  getStats: jest.fn(),
}));

describe('DashboardPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('displays stats after successful fetch', async () => {
    const mockStats = {
      totalRequests: 150,
      successCount: 140,
      errorCount: 10,
      avgLatencyMs: 245.5,
      totalPromptTokens: 5000,
      totalCompletionTokens: 8000,
      requestsByModel: { 'openai/gpt-4o': 100, 'ollama/llama3': 50 },
      requestsByProvider: { openai: 100, ollama: 50 },
    };
    (api.getStats as jest.Mock).mockResolvedValue(mockStats);

    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('150')).toBeInTheDocument();
      expect(screen.getByText(/93%|140/i)).toBeInTheDocument();
      expect(screen.getByText(/245\.5|246/i)).toBeInTheDocument();
    });
  });

  it('displays top models section', async () => {
    const mockStats = {
      totalRequests: 10,
      successCount: 10,
      errorCount: 0,
      avgLatencyMs: 100,
      totalPromptTokens: 100,
      totalCompletionTokens: 200,
      requestsByModel: { 'openai/gpt-4o': 7, 'ollama/llama3': 3 },
      requestsByProvider: {},
    };
    (api.getStats as jest.Mock).mockResolvedValue(mockStats);

    render(<DashboardPage />);

    await waitFor(() => {
      expect(screen.getByText('openai/gpt-4o')).toBeInTheDocument();
      expect(screen.getByText('7 запросов')).toBeInTheDocument();
    });
  });

  it('handles stats fetch error gracefully', async () => {
    (api.getStats as jest.Mock).mockRejectedValue(new Error('API error'));

    render(<DashboardPage />);

    await waitFor(() => {
      const zeros = screen.getAllByText('0');
      expect(zeros.length).toBeGreaterThan(0);
    });
  });
});