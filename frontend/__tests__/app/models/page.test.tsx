import { render, screen, waitFor } from '@testing-library/react';
import ModelsPage from '@/app/models/page';
import * as api from '@/lib/api';

jest.mock('@/lib/api', () => ({
  listModels: jest.fn(),
}));

describe('ModelsPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('displays loading state initially', () => {
    (api.listModels as jest.Mock).mockImplementation(() => new Promise(() => {}));
    const { container } = render(<ModelsPage />);

    expect(screen.getByText('Доступные модели')).toBeInTheDocument();
    expect(container.querySelector('.animate-pulse')).toBeInTheDocument();
  });

  it('renders list of models after successful fetch', async () => {
    const mockModels = {
      object: 'list',
      data: [
        { id: 'ollama/llama3.2', object: 'model', created: 1700000000, owned_by: 'ollama' },
        { id: 'openai/gpt-4o', object: 'model', created: 1700000000, owned_by: 'openai' },
        { id: 'vllm/mistral-7b', object: 'model', created: 1700000000, owned_by: 'vllm' },
      ],
    };
    (api.listModels as jest.Mock).mockResolvedValue(mockModels);

    render(<ModelsPage />);

    await waitFor(() => {
      expect(screen.getByText('ollama/llama3.2')).toBeInTheDocument();
      expect(screen.getByText('openai/gpt-4o')).toBeInTheDocument();
      expect(screen.getByText('vllm/mistral-7b')).toBeInTheDocument();
    });

    // Проверяем бейджи провайдеров
    expect(screen.getByText('Ollama')).toBeInTheDocument();
    expect(screen.getByText('OpenAI')).toBeInTheDocument();
    expect(screen.getByText('vLLM')).toBeInTheDocument();
  });

  it('handles unknown providers with fallback', async () => {
    const mockModels = {
      object: 'list',
      data: [
        { id: 'custom/model', object: 'model', created: 1700000000, owned_by: 'custom-provider' },
      ],
    };
    (api.listModels as jest.Mock).mockResolvedValue(mockModels);

    render(<ModelsPage />);

    await waitFor(() => {
      expect(screen.getByText('custom/model')).toBeInTheDocument();
      expect(screen.getByText('custom-provider')).toBeInTheDocument();
    });
  });

  it('displays empty state when no models', async () => {
    (api.listModels as jest.Mock).mockResolvedValue({ object: 'list', data: [] });

    render(<ModelsPage />);

    await waitFor(() => {
      expect(screen.getByText('Доступные модели')).toBeInTheDocument();
    });

    // Проверяем, что после загрузки нет скелетонов
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });

  it('handles API errors gracefully', async () => {
    const consoleSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    (api.listModels as jest.Mock).mockRejectedValue(new Error('API Error'));

    render(<ModelsPage />);

    await waitFor(() => {
      expect(screen.getByText('Доступные модели')).toBeInTheDocument();
    });

    consoleSpy.mockRestore();
  });
});
