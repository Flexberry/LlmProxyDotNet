'use client';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import * as z from 'zod';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Checkbox } from '@/components/ui/checkbox';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { X } from 'lucide-react';

/**
 * Form schema for API key creation
 */
const formSchema = z.object({
  name: z.string().optional(),
  permissions: z.array(z.string()).optional(),
  expiresAt: z.string().optional(),
}).refine(data => !data.permissions || data.permissions.length === 0 || data.permissions.length > 0, {
  message: 'Select at least one model or leave empty for all',
  path: ['permissions'],
});

/**
 * Available models for selection
 */
const MODELS = [
  { value: '*', label: 'All models (*)' },
  { value: 'ollama/llama3', label: 'Ollama: Llama 3' },
  { value: 'ollama/mistral', label: 'Ollama: Mistral' },
  { value: 'openai/gpt-4o', label: 'OpenAI: GPT-4o' },
  { value: 'openai/gpt-4o-mini', label: 'OpenAI: GPT-4o Mini' },
  { value: 'vllm/mistral-7b', label: 'vLLM: Mistral 7B' },
  { value: 'openrouter/meta-llama/llama-3-70b', label: 'OpenRouter: Llama 3 70B' },
  { value: 'zai/z-ai-chat', label: 'Z.ai: Chat' },
];

/**
 * API key creation form component
 * @param props - Component properties
 * @param props.onSubmit - Callback when form is submitted
 * @param props.onCancel - Callback when form is cancelled
 */
export function KeyForm({ onSubmit, onCancel }: { onSubmit: (d: z.infer<typeof formSchema>) => Promise<void>; onCancel: () => void }) {
  const [loading, setLoading] = useState(false);
  const { register, handleSubmit, watch, setValue, formState: { errors } } = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: { permissions: [] }
  });

  const selected = watch('permissions') || [];
  const toggle = (val: string) => {
    const next = val === '*' ? (selected.includes('*') ? [] : ['*']) : 
      selected.filter(p => p !== '*').includes(val) ? selected.filter(p => p !== val) : [...selected.filter(p => p !== '*'), val];
    setValue('permissions', next);
  };

  return (
    <form onSubmit={handleSubmit(async (data) => { setLoading(true); await onSubmit(data); setLoading(false); })} className="space-y-4">
      <div className="space-y-2">
        <Label>Название</Label>
        <Input placeholder="Production Key" {...register('name')} />
      </div>
      <div className="space-y-2">
        <Label>Доступ к моделям</Label>
        <div className="grid grid-cols-2 gap-2 max-h-40 overflow-y-auto p-2 border rounded">
          {MODELS.map(m => (
            <label key={m.value} className="flex items-center gap-2 cursor-pointer">
              <Checkbox checked={selected.includes(m.value)} onChange={() => toggle(m.value)} />
              <span className="text-sm">{m.label}</span>
            </label>
          ))}
        </div>
        <div className="flex flex-wrap gap-1 mt-1">
          {selected.map(s => <Badge key={s} variant="secondary" className="gap-1">{s}<X className="w-3 h-3 cursor-pointer" onClick={() => toggle(s)}/></Badge>)}
        </div>
        {errors.permissions && <p className="text-red-500 text-xs">{errors.permissions.message}</p>}
      </div>
      <div className="space-y-2">
        <Label>Истекает (опционально)</Label>
        <Input type="datetime-local" {...register('expiresAt')} />
      </div>
      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel} disabled={loading}>Отмена</Button>
        <Button type="submit" disabled={loading}>{loading ? 'Создание...' : 'Создать ключ'}</Button>
      </div>
    </form>
  );
}