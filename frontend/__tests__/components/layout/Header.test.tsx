import { render, screen } from '@testing-library/react';
import { Header } from '@/components/layout/Header';

describe('Header', () => {
  it('renders header element', () => {
    render(<Header />);

    const header = screen.getByRole('banner');
    expect(header).toBeInTheDocument();
  });

  it('renders empty header without buttons', () => {
    const { container } = render(<Header />);

    const buttons = container.querySelectorAll('button');
    expect(buttons.length).toBe(0);
  });

  it('has correct styling classes', () => {
    const { container } = render(<Header />);

    const header = container.querySelector('header');
    expect(header).toHaveClass('sticky', 'top-0');
  });

  it('has correct z-index', () => {
    const { container } = render(<Header />);

    const header = container.querySelector('header');
    expect(header).toHaveClass('z-30');
  });

  it('has border-bottom', () => {
    const { container } = render(<Header />);

    const header = container.querySelector('header');
    expect(header).toHaveClass('border-b');
  });

  it('has correct height', () => {
    const { container } = render(<Header />);

    const header = container.querySelector('header');
    expect(header).toHaveClass('h-16');
  });
});
