import { cn, formatBytes, formatDate } from '@/lib/utils';

describe('Utils', () => {
  describe('cn', () => {
    it('merges class names correctly', () => {
      expect(cn('foo', 'bar')).toBe('foo bar');
    });

    it('handles conditional classes', () => {
      expect(cn('base', true && 'active', false && 'hidden')).toBe('base active');
    });

    it('merges tailwind classes with conflicts', () => {
      expect(cn('px-2 py-1', 'px-4')).toBe('py-1 px-4');
    });

    it('handles empty inputs', () => {
      expect(cn()).toBe('');
      expect(cn('', null, undefined)).toBe('');
    });

    it('handles array inputs', () => {
      expect(cn(['foo', 'bar'])).toBe('foo bar');
    });
  });

  describe('formatBytes', () => {
    it('formats 0 bytes', () => {
      expect(formatBytes(0)).toBe('0 B');
    });

    it('formats bytes', () => {
      expect(formatBytes(512)).toBe('512 B');
    });

    it('formats kilobytes', () => {
      expect(formatBytes(1024)).toBe('1 KB');
      expect(formatBytes(1536)).toBe('1.5 KB');
    });

    it('formats megabytes', () => {
      expect(formatBytes(1024 * 1024)).toBe('1 MB');
      expect(formatBytes(1024 * 1024 * 2.5)).toBe('2.5 MB');
    });

    it('formats gigabytes', () => {
      expect(formatBytes(1024 * 1024 * 1024)).toBe('1 GB');
    });

    it('respects custom decimals', () => {
      expect(formatBytes(1024, 0)).toBe('1 KB');
      expect(formatBytes(1500, 3)).toBe('1.465 KB');
    });
  });

  describe('formatDate', () => {
    it('formats ISO string', () => {
      const result = formatDate('2024-03-15');
      expect(result).toContain('2024');
      expect(result).toContain('15');
    });

    it('formats Date object', () => {
      const date = new Date('2024-06-20');
      const result = formatDate(date);
      expect(result).toContain('2024');
      expect(result).toContain('20');
    });

    it('uses ru-RU locale', () => {
      const result = formatDate('2024-01-01');
      // В русской локали дата содержит точки или пробелы
      expect(result).toMatch(/\d{1,2}\s/);
    });
  });
});
