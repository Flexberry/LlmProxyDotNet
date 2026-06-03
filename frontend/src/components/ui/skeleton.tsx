import { cn } from '@/lib/utils';

/**
 * Skeleton loading placeholder component
 * @param props - HTML div attributes
 * @param props.className - Additional CSS classes
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