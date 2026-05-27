import { render, screen } from '@testing-library/react';
import SettingsPage from '@/app/settings/page';

describe('SettingsPage', () => {
  it('renders settings heading', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Настройки')).toBeInTheDocument();
    expect(screen.getByText('Глобальные настройки прокси-сервера')).toBeInTheDocument();
  });

  it('displays development notice', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Функция в разработке')).toBeInTheDocument();
    expect(screen.getByText('Расширенные настройки будут доступны в версии 2.0')).toBeInTheDocument();
  });

  it('lists current features', () => {
    render(<SettingsPage />);

    expect(screen.getByText('Управление API ключами')).toBeInTheDocument();
    expect(screen.getByText('Маршрутизация запросов')).toBeInTheDocument();
    expect(screen.getByText('Просмотр статистики')).toBeInTheDocument();
  });

  it('has correct structure with cards', () => {
    const { container } = render(<SettingsPage />);

    // Проверяем наличие Card компонентов
    expect(container.querySelector('[class*="card"]')).toBeInTheDocument();
  });
});
