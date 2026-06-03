import { HTMLAttributes, forwardRef } from 'react';

interface AlertProps extends HTMLAttributes<HTMLDivElement> {
  /** Alert visual variant */
  variant?: 'default' | 'destructive';
}

/**
 * Alert component for displaying messages
 * @param props - Alert properties
 * @param props.className - Additional CSS classes
 * @param props.variant - Visual variant (default: 'default')
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
 * Alert description component
 * @param props - HTML paragraph attributes
 * @param props.className - Additional CSS classes
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