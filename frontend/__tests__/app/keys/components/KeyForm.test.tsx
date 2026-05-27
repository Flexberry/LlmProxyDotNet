import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { KeyForm } from '@/app/keys/components/KeyForm';

describe('KeyForm', () => {
  const mockSubmit = jest.fn();
  const mockCancel = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders form fields', () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    expect(screen.getByPlaceholderText(/Production Key/i)).toBeInTheDocument();
    expect(screen.getByText(/Доступ к моделям/i)).toBeInTheDocument();
    expect(screen.getByText(/Истекает/i)).toBeInTheDocument();
  });

  it('renders model options', () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    expect(screen.getByText('Все модели (*)')).toBeInTheDocument();
    expect(screen.getByText('Ollama: Llama 3')).toBeInTheDocument();
    expect(screen.getByText('OpenAI: GPT-4o')).toBeInTheDocument();
  });

  it('calls onCancel when cancel button clicked', async () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    const cancelButton = screen.getByRole('button', { name: /Отмена/i });
    await userEvent.click(cancelButton);

    expect(mockCancel).toHaveBeenCalledTimes(1);
  });

  it('submits form with name and permissions', async () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    const nameInput = screen.getByPlaceholderText(/Production Key/i);
    await userEvent.type(nameInput, 'Test Key');

    // Выбираем "Все модели"
    const allModelsCheckbox = screen.getByLabelText('Все модели (*)');
    await userEvent.click(allModelsCheckbox);

    const submitButton = screen.getByRole('button', { name: /Создать ключ/i });
    await userEvent.click(submitButton);

    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          name: 'Test Key',
          permissions: ['*'],
        })
      );
    });
  });

  it('disables buttons during submission', async () => {
    mockSubmit.mockImplementation(() => new Promise(() => {})); // Never resolves

    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    const submitButton = screen.getByRole('button', { name: /Создать ключ/i });
    await userEvent.click(submitButton);

    await waitFor(() => {
      expect(submitButton).toBeDisabled();
      expect(screen.getByRole('button', { name: /Отмена/i })).toBeDisabled();
    });

    expect(screen.getByText(/Создание/i)).toBeInTheDocument();
  });
});
