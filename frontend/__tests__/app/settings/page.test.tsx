import { render, screen } from '@testing-library/react';
import SettingsPage from '@/app/settings/page';

describe('SettingsPage', () => {
  it('renders settings heading', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Настройки')).toBeInTheDocument();
    expect(screen.getByText('Информация о системе и функционале')).toBeInTheDocument();
  });

  it('displays feature cards', () => {
    render(<SettingsPage />);

    expect(screen.getByText('OpenAI-compatible API')).toBeInTheDocument();
    expect(screen.getByText('5 LLM Providers')).toBeInTheDocument();
    expect(screen.getByText('API Key Authentication')).toBeInTheDocument();
    // Проверяем что есть хотя бы один badge v2
    const v2Badges = screen.getAllByText('v2');
    expect(v2Badges.length).toBeGreaterThan(0);
  });

  it('shows system information', () => {
    render(<SettingsPage />);

    expect(screen.getByText('О системе')).toBeInTheDocument();
    expect(screen.getByText('v2.0.0')).toBeInTheDocument();
    expect(screen.getByText('.NET 10 + Next.js 16')).toBeInTheDocument();
    expect(screen.getByText('PostgreSQL')).toBeInTheDocument();
    expect(screen.getByText('Redis')).toBeInTheDocument();
  });

  it('displays upcoming features', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Функции v2 (в разработке)')).toBeInTheDocument();
    // Проверяем наличие текста в ul/li элементах
    const upcomingFeatures = screen.getByText(/Rate Limiting/);
    expect(upcomingFeatures).toBeInTheDocument();
  });

  it('has correct structure with cards', () => {
    const { container } = render(<SettingsPage />);

    // Проверяем наличие нескольких Card компонентов
    const cards = container.querySelectorAll('[class*="card"]');
    expect(cards.length).toBeGreaterThan(2);
  });
});
