import { render, screen } from '@testing-library/react';
import { Sidebar } from '@/components/layout/Sidebar';

jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
}));

import { usePathname } from 'next/navigation';

describe('Sidebar', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders sidebar with logo', () => {
    (usePathname as jest.Mock).mockReturnValue('/');
    render(<Sidebar />);

    expect(screen.getByText('LLM Proxy')).toBeInTheDocument();
    expect(screen.getByText('v2.0.0')).toBeInTheDocument();
  });

  it('renders navigation links', () => {
    (usePathname as jest.Mock).mockReturnValue('/');
    render(<Sidebar />);

    expect(screen.getByText('Дашборд')).toBeInTheDocument();
    expect(screen.getByText('API Ключи')).toBeInTheDocument();
    expect(screen.getByText('Модели')).toBeInTheDocument();
    expect(screen.getByText('Настройки')).toBeInTheDocument();
  });

  it('highlights active link', () => {
    (usePathname as jest.Mock).mockReturnValue('/keys');
    const { container } = render(<Sidebar />);

    const links = container.querySelectorAll('a');
    const keysLink = Array.from(links).find(a => a.textContent?.includes('API Ключи'));

    expect(keysLink).toHaveClass('bg-primary');
  });

  it('does not highlight inactive links', () => {
    (usePathname as jest.Mock).mockReturnValue('/keys');
    const { container } = render(<Sidebar />);

    const links = container.querySelectorAll('a');
    const dashboardLink = Array.from(links).find(a => a.textContent?.includes('Дашборд'));

    expect(dashboardLink).not.toHaveClass('bg-primary');
    expect(dashboardLink).toHaveClass('text-muted-foreground');
  });

  it('displays tech stack info', () => {
    (usePathname as jest.Mock).mockReturnValue('/');
    render(<Sidebar />);

    expect(screen.getByText('.NET 10 + Next.js 16')).toBeInTheDocument();
  });

  it('handles root path correctly', () => {
    (usePathname as jest.Mock).mockReturnValue('/');
    const { container } = render(<Sidebar />);

    const links = container.querySelectorAll('a');
    const dashboardLink = Array.from(links).find(a => a.textContent?.includes('Дашборд'));

    expect(dashboardLink).toHaveClass('bg-primary');
  });

  it('handles nested routes', () => {
    (usePathname as jest.Mock).mockReturnValue('/keys/new');
    const { container } = render(<Sidebar />);

    const links = container.querySelectorAll('a');
    const keysLink = Array.from(links).find(a => a.textContent?.includes('API Ключи'));

    expect(keysLink).toHaveClass('bg-primary');
  });

  it('renders all navigation items', () => {
    (usePathname as jest.Mock).mockReturnValue('/');
    render(<Sidebar />);

    expect(screen.getByText('Дашборд')).toBeInTheDocument();
    expect(screen.getByText('API Ключи')).toBeInTheDocument();
    expect(screen.getByText('Модели')).toBeInTheDocument();
    expect(screen.getByText('Rate Limit & Budget')).toBeInTheDocument();
    expect(screen.getByText('Команды')).toBeInTheDocument();
    expect(screen.getByText('Настройки')).toBeInTheDocument();
  });
});
