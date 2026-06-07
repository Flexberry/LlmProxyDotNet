import { forwardRef, LabelHTMLAttributes } from 'react';

/**
 * Компонент поля ввода Label
 * @param props - HTML атрибуты Label
 * @param props.className - Дополнительные CSS классы
 */
export const Label = forwardRef<HTMLLabelElement, LabelHTMLAttributes<HTMLLabelElement>>(
  ({ className, ...props }, ref) => (
    <label ref={ref} className={`text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 ${className}`} {...props} />
  )
);
Label.displayName = 'Label';