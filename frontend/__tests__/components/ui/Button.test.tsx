import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Button } from '@/components/ui/button';

describe('Button Component', () => {
  it('renders with default styles', () => {
    render(<Button>Click me</Button>);
    const button = screen.getByRole('button', { name: /click me/i });
    
    expect(button).toBeInTheDocument();
    expect(button).toHaveClass('bg-primary', 'text-primary-foreground');
  });

  it('supports variant prop', () => {
    render(<Button variant="destructive">Delete</Button>);
    const button = screen.getByRole('button', { name: /delete/i });
    
    expect(button).toHaveClass('bg-destructive', 'text-destructive-foreground');
  });

  it('supports size prop', () => {
    render(<Button size="icon">X</Button>);
    const button = screen.getByRole('button');
    
    expect(button).toHaveClass('h-9', 'w-9');
  });

  it('calls onClick handler when clicked', async () => {
    const user = userEvent.setup();
    const handleClick = jest.fn();
    
    render(<Button onClick={handleClick}>Test</Button>);
    const button = screen.getByRole('button', { name: /test/i });
    
    await user.click(button);
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it('is disabled when disabled prop is true', () => {
    render(<Button disabled>Disabled</Button>);
    const button = screen.getByRole('button', { name: /disabled/i });
    
    expect(button).toBeDisabled();
    expect(button).toHaveClass('disabled:pointer-events-none', 'disabled:opacity-50');
  });

  it('supports outline variant', () => {
    render(<Button variant="outline">Outline</Button>);
    const button = screen.getByRole('button', { name: /outline/i });
    
    expect(button).toHaveClass('border', 'border-input');
  });

  it('supports secondary variant', () => {
    render(<Button variant="secondary">Secondary</Button>);
    const button = screen.getByRole('button', { name: /secondary/i });
    
    expect(button).toHaveClass('bg-secondary', 'text-secondary-foreground');
  });

  it('supports ghost variant', () => {
    render(<Button variant="ghost">Ghost</Button>);
    const button = screen.getByRole('button', { name: /ghost/i });
    
    expect(button).not.toHaveClass('bg-primary');
  });

  it('supports link variant', () => {
    render(<Button variant="link">Link</Button>);
    const button = screen.getByRole('button', { name: /link/i });
    
    expect(button).toHaveClass('text-primary', 'underline-offset-4');
  });

  it('merges custom className correctly', () => {
    render(<Button className="custom-class">Test</Button>);
    const button = screen.getByRole('button', { name: /test/i });
    
    expect(button).toHaveClass('custom-class');
    expect(button).toHaveClass('inline-flex');
  });

  it('renders with sm size', () => {
    render(<Button size="sm">Small</Button>);
    const button = screen.getByRole('button', { name: /small/i });
    
    expect(button).toHaveClass('h-8', 'text-xs');
  });

  it('renders with lg size', () => {
    render(<Button size="lg">Large</Button>);
    const button = screen.getByRole('button', { name: /large/i });
    
    expect(button).toHaveClass('h-10', 'px-8');
  });
});