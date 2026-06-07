import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

/**
 * Объединяет и сливает Tailwind CSS классы
 * @param inputs - Значения CSS классов для объединения
 * @returns Объединённая строка CSS классов
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

/**
 * Форматирует байты в человеко-читаемый формат
 * @param bytes - Количество байт для форматирования
 * @param decimals - Количество знаков после запятой (по умолчанию: 2)
 * @returns Отформатированная строка байт (например, "1.5 KB")
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
 * Форматирует дату в локализованную строку
 * @param date - Строка даты или объект Date для форматирования
 * @returns Отформатированная строка даты (например, "15 янв. 2025")
 */
export function formatDate(date: string | Date) {
  return new Date(date).toLocaleDateString('ru-RU', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}