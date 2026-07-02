// frontend/__tests__/app/keys/page.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import ApiKeysPage from '@/app/keys/page';
import * as api from '@/lib/api';

jest.mock('@/lib/api', () => ({
  listApiKeys: jest.fn(),
  createApiKey: jest.fn(),
  revokeApiKey: jest.fn(),
}));

describe('ApiKeysPage', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('displays loading state initially', () => {
    (api.listApiKeys as jest.Mock).mockImplementation(() => new Promise(() => {}));
    const { container } = render(<ApiKeysPage />);
    
    // Проверяем наличие заголовка и анимации Skeleton (класс animate-pulse)
    expect(screen.getByText('API Ключи')).toBeInTheDocument();
    expect(container.querySelector('.animate-pulse')).toBeInTheDocument();
  });

  it('renders list of keys after successful fetch', async () => {
    const mockKeys = [
      { id: '1', name: 'Prod Key', keyHash: 'abc123', isActive: true, permissions: '*', createdAt: '2024-01-01' },
      { id: '2', name: 'Dev Key', keyHash: 'def456', isActive: false, permissions: 'ollama/*', createdAt: '2024-01-02' },
    ];
    (api.listApiKeys as jest.Mock).mockResolvedValue(mockKeys);

    render(<ApiKeysPage />);

    await waitFor(() => {
      expect(screen.getByText('Prod Key')).toBeInTheDocument();
      expect(screen.getByText('Dev Key')).toBeInTheDocument();
      expect(screen.getByText(/Отозван|Revoked/)).toBeInTheDocument();
    });
  });

  it('calls revokeApiKey when revoke button clicked', async () => {
    const user = userEvent.setup();
    const mockKeys = [{ id: 'key-123', name: 'Test', keyHash: 'abc', isActive: true, permissions: '*', createdAt: '2024-01-01' }];
    (api.listApiKeys as jest.Mock).mockResolvedValue(mockKeys);
    (api.revokeApiKey as jest.Mock).mockResolvedValue(undefined);

    window.confirm = jest.fn().mockReturnValue(true);

    render(<ApiKeysPage />);

    await waitFor(() => {
      const revokeBtn = screen.getByRole('button', { name: /отозвать|revoke|delete/i });
      expect(revokeBtn).toBeInTheDocument();
    });

    // Проверяем, что listApiKeys был вызван при монтировании
    expect(api.listApiKeys).toHaveBeenCalled();
    const initialCallCount = (api.listApiKeys as jest.Mock).mock.calls.length;

    const revokeBtn = screen.getByRole('button', { name: /отозвать|revoke|delete/i });
    await user.click(revokeBtn);

    expect(api.revokeApiKey).toHaveBeenCalledWith('key-123');
    
    // Проверяем, что listApiKeys был вызван ещё раз после revoke
    await waitFor(() => {
      expect((api.listApiKeys as jest.Mock).mock.calls.length).toBeGreaterThan(initialCallCount);
    });
  });

  it('displays empty state when no keys', async () => {
    (api.listApiKeys as jest.Mock).mockResolvedValue([]);

    render(<ApiKeysPage />);

    await waitFor(() => {
      expect(screen.getByText('Нет активных ключей')).toBeInTheDocument();
      expect(screen.getByText('Создайте первый ключ для начала работы')).toBeInTheDocument();
    });
  });
});