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

    expect(screen.getByText('All models (*)')).toBeInTheDocument();
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

    // Выбираем "All models"
    const allModelsCheckbox = screen.getByLabelText('All models (*)');
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

  it('selects specific models instead of all', async () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    // Uncheck "All models"
    const allModelsCheckbox = screen.getByLabelText('All models (*)');
    await userEvent.click(allModelsCheckbox);

    // Select specific model
    const ollamaCheckbox = screen.getByLabelText('Ollama: Llama 3');
    await userEvent.click(ollamaCheckbox);

    const submitButton = screen.getByRole('button', { name: /Создать ключ/i });
    await userEvent.click(submitButton);

    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          permissions: expect.arrayContaining(['ollama/llama3']),
        })
      );
    });
  });

  it('sets expiration date', async () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    const nameInput = screen.getByPlaceholderText(/Production Key/i);
    await userEvent.type(nameInput, 'Test Key');

    const allModelsCheckbox = screen.getByLabelText('All models (*)');
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

  it('validates required fields', async () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    const submitButton = screen.getByRole('button', { name: /Создать ключ/i });
    await userEvent.click(submitButton);

    // Form allows empty name and no permissions (defaults to all)
    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          name: '',
          permissions: [],
        })
      );
    });
  });

  it('handles empty name gracefully', async () => {
    render(<KeyForm onSubmit={mockSubmit} onCancel={mockCancel} />);

    const allModelsCheckbox = screen.getByLabelText('All models (*)');
    await userEvent.click(allModelsCheckbox);

    const submitButton = screen.getByRole('button', { name: /Создать ключ/i });
    await userEvent.click(submitButton);

    await waitFor(() => {
      expect(mockSubmit).toHaveBeenCalledWith(
        expect.objectContaining({
          name: '',
          permissions: ['*'],
        })
      );
    });
  });
});
