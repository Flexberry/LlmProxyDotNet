import { HTMLAttributes, forwardRef } from 'react';

interface AlertProps extends HTMLAttributes<HTMLDivElement> {
  /** Визуальный вариант Alert */
  variant?: 'default' | 'destructive';
}

/**
 * Компонент Alert для отображения сообщений
 * @param props - Свойства Alert
 * @param props.className - Дополнительные CSS классы
 * @param props.variant - Визуальный вариант (по умолчанию: 'default')
 */
export const Alert = forwardRef<HTMLDivElement, AlertProps>(
  ({ className, variant = 'default', ...props }, ref) => (
    <div
      ref={ref}
      role="alert"
      className={`relative w-full rounded-lg border p-4 [&>svg~*]:pl-7 [&>svg+div]:translate-y-[-3px] [&>svg]:absolute [&>svg]:left-4 [&>svg]:top-4 [&>svg]:text-foreground ${
        variant === 'destructive'
          ? 'border-destructive/50 text-destructive dark:border-destructive [&>svg]:text-destructive'
          : 'bg-background text-foreground'
      } ${className}`}
      {...props}
    />
  )
);
Alert.displayName = 'Alert';

/**
 * Компонент описания Alert
 * @param props - HTML paragraph атрибуты
 * @param props.className - Дополнительные CSS классы
 */
export const AlertDescription = forwardRef<HTMLParagraphElement, HTMLAttributes<HTMLParagraphElement>>(
  ({ className, ...props }, ref) => (
    <p
      ref={ref}
      className={`text-sm [&_p]:leading-relaxed ${className}`}
      {...props}
    />
  )
);
AlertDescription.displayName = 'AlertDescription';