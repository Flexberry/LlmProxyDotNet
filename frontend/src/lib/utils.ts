import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Combines and merges Tailwind CSS classes
 * @param inputs - CSS class values to combine
 * @returns Merged CSS class string
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Formats bytes into human-readable format
 * @param bytes - Number of bytes to format
 * @param decimals - Number of decimal places (default: 2)
 * @returns Formatted byte string (e.g., "1.5 KB")
 */
export function formatBytes(bytes: number, decimals = 2) {
  if (bytes <= 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  const clampedI = Math.max(0, Math.min(i, sizes.length - 1));
  return `${parseFloat((bytes / Math.pow(k, clampedI)).toFixed(decimals))} ${sizes[clampedI]}`;
}

/**
 * Formats a date into localized string
 * @param date - Date string or Date object to format
 * @returns Formatted date string (e.g., "Jan 15, 2025")
 */
export function formatDate(date: string | Date) {
  return new Date(date).toLocaleDateString('ru-RU', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}