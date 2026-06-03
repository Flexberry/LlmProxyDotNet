import { forwardRef, LabelHTMLAttributes } from 'react';

/**
 * Label form field component
 * @param props - Label HTML attributes
 * @param props.className - Additional CSS classes
 */
export const Label = forwardRef<HTMLLabelElement, LabelHTMLAttributes<HTMLLabelElement>>(
  ({ className, ...props }, ref) => (
    <label ref={ref} className={`text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70 ${className}`} {...props} />
  )
);
Label.displayName = 'Label';