import { cn } from '@/lib/utils';

/**
 * Компонент-заглушка Skeleton для состояния загрузки
 * @param props - HTML div атрибуты
 * @param props.className - Дополнительные CSS классы
 */
function Skeleton({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return (
    <div
      className={cn('animate-pulse rounded-md bg-muted', className)}
      {...props}
    />
  );
}

export { Skeleton };