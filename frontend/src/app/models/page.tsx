'use client';

import { useEffect, useState } from 'react';
import { listModels, type ModelsListResponse } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Skeleton } from '@/components/ui/skeleton';
import { Bot, Server } from 'lucide-react';

export default function ModelsPage() {
  const [models, setModels] = useState<ModelsListResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    listModels()
      .then(setModels)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, []);

  const getProviderBadge = (ownedBy: string) => {
    const variants: Record<string, { variant: string; label: string }> = {
      'ollama': { variant: 'secondary', label: 'Ollama' },
      'vllm': { variant: 'secondary', label: 'vLLM' },
      'openai': { variant: 'default', label: 'OpenAI' },
      'openrouter': { variant: 'outline', label: 'OpenRouter' },
      'zai': { variant: 'outline', label: 'Z.ai' },
    };
    const config = variants[ownedBy.toLowerCase()] || { variant: 'outline', label: ownedBy };
    return <Badge variant={config.variant as any}>{config.label}</Badge>;
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <div>
        <h1 className="text-2xl font-bold">Доступные модели</h1>
        <p className="text-sm text-muted-foreground">
          Модели, доступные через настроенных провайдеров
        </p>
      </div>

      {loading ? (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <Card key={i}>
              <CardHeader className="pb-2">
                <Skeleton className="h-4 w-3/4" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-3 w-1/2" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {models?.data.map((model) => (
            <Card key={model.id} className="hover:shadow-md transition-shadow">
              <CardHeader className="pb-2">
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-2">
                    <Bot className="h-4 w-4 text-muted-foreground" />
                    <CardTitle className="text-base font-mono truncate">
                      {model.id}
                    </CardTitle>
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex items-center justify-between">
                  {getProviderBadge(model.owned_by)}
                  <span className="text-xs text-muted-foreground">
                    ID: {model.id.split('/').pop()}
                  </span>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}