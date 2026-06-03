import { render, screen } from '@testing-library/react';
import RootLayout from '@/app/layout';

jest.mock('next/font/google', () => ({
  Inter: () => ({ className: 'inter-mock' }),
}));

jest.mock('@/components/layout/Sidebar', () => ({
  Sidebar: () => <aside data-testid="sidebar">Sidebar</aside>,
}));

jest.mock('@/components/layout/Header', () => ({
  Header: () => <header data-testid="header">Header</header>,
}));

describe('RootLayout', () => {
  it('renders children within layout structure', () => {
    render(
      <RootLayout>
        <div data-testid="child-content">Test Content</div>
      </RootLayout>
    );

    expect(screen.getByTestId('sidebar')).toBeInTheDocument();
    expect(screen.getByTestId('header')).toBeInTheDocument();
    expect(screen.getByTestId('child-content')).toBeInTheDocument();
  });
});

