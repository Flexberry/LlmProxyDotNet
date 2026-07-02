import 'whatwg-fetch';
import '@testing-library/jest-dom';

// Polyfill для TextEncoder/TextDecoder (нужен для тестов streaming API)
import { TextEncoder, TextDecoder } from 'util';
if (typeof global.TextEncoder === 'undefined') {
  global.TextEncoder = TextEncoder;
}
if (typeof global.TextDecoder === 'undefined') {
  global.TextDecoder = TextDecoder;
}

// Only setup MSW if we're in a Node environment (not browser)
if (typeof window === 'undefined') {
  // Dynamic import for MSW to avoid module resolution issues
  import('./__tests__/mocks/server').then(({ server }) => {
    beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
    afterEach(() => server.resetHandlers());
    afterAll(() => server.close());
  });
}

// Мокаем next/router для тестов компонентов
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    prefetch: jest.fn(),
    back: jest.fn(),
  }),
  usePathname: () => '/',
  useSearchParams: () => new URLSearchParams(),
}));