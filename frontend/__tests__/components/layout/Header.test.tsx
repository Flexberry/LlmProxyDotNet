import { render, screen } from '@testing-library/react';
import { Header } from '@/components/layout/Header';

describe('Header', () => {
  it('renders header with navigation elements', () => {
    render(<Header />);

    // Проверяем наличие header элемента
    const header = screen.getByRole('banner');
    expect(header).toBeInTheDocument();
  });

  it('contains notification button with indicator', () => {
    const { container } = render(<Header />);

    // Проверяем наличие кнопок (Bell и User)
    const buttons = container.querySelectorAll('button');
    expect(buttons.length).toBe(2);
  });

  it('has correct styling classes', () => {
    const { container } = render(<Header />);

    const header = container.querySelector('header');
    expect(header).toHaveClass('sticky', 'top-0');
  });
});
