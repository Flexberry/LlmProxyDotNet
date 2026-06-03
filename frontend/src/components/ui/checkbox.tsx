import { forwardRef, InputHTMLAttributes } from 'react';

/**
 * Checkbox form field component
 * @param props - Checkbox HTML attributes (excluding type)
 * @param props.className - Additional CSS classes
 */
export const Checkbox = forwardRef<HTMLInputElement, Omit<InputHTMLAttributes<HTMLInputElement>, "type">>(
  ({ className, ...props }, ref) => (
    <input type="checkbox" ref={ref} className={`h-4 w-4 shrink-0 rounded-sm border border-primary ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 ${className}`} {...props} />
  )
);
Checkbox.displayName = 'Checkbox';