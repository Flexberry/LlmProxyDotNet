
'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { AlertCircle, CheckCircle2, Server, Shield, Bot, Activity } from 'lucide-react';

export default function SettingsPage() {
  const features = [
    { name: 'OpenAI-compatible API', description: 'Полная поддержка /v1/chat/completions и /v1/embeddings', icon: CheckCircle2, status: 'active' },
    { name: '5 LLM Providers', description: 'Ollama, vLLM, OpenAI, OpenRouter, Z.ai', icon: Server, status: 'active' },
    { name: 'API Key Authentication', description: 'Управление ключами с правами доступа', icon: Shield, status: 'active' },
    { name: 'Model Routing', description: 'Интеллектуальная маршрутизация между провайдерами', icon: Activity, status: 'active' },
    { name: 'Streaming Support', description: 'Server-Sent Events для потоковых ответов', icon: Bot, status: 'active' },
    { name: 'Request Logging', description: 'Логирование запросов с сохранением в БД', icon: CheckCircle2, status: 'active' },
  ];

  return (
    <div className="space-y-6 animate-fade-in">
      <div>
        <h1 className="text-2xl font-bold">Настройки</h1>
        <p className="text-sm text-muted-foreground">
          Информация о системе и функционале
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {features.map((feature) => (
          <Card key={feature.name}>
            <CardHeader className="pb-3">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2">
                  <feature.icon className="h-4 w-4 text-primary" />
                  <CardTitle className="text-sm font-medium">{feature.name}</CardTitle>
                </div>
                <Badge variant="secondary" className="text-xs">v2</Badge>
              </div>
              <CardDescription className="text-xs mt-1">
                {feature.description}
              </CardDescription>
            </CardHeader>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>О системе</CardTitle>
          <CardDescription>
            Технические детали
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="text-muted-foreground">Версия</p>
              <p className="font-semibold">v2.0.0</p>
            </div>
            <div>
              <p className="text-muted-foreground">Стек</p>
              <p className="font-semibold">.NET 10 + Next.js 16</p>
            </div>
            <div>
              <p className="text-muted-foreground">База данных</p>
              <p className="font-semibold">PostgreSQL</p>
            </div>
            <div>
              <p className="text-muted-foreground">Кэш</p>
              <p className="font-semibold">Redis</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-amber-500" />
            Функции v2 (в разработке)
          </CardTitle>
          <CardDescription>
            Планируемые улучшения
          </CardDescription>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          <ul className="list-disc list-inside space-y-1">
            <li>Rate Limiting - Ограничения на ключ и провайдера</li>
            <li>Управление бюджетами - Отслеживание расходов</li>
            <li>Team/Org RBAC - Многопользовательский контроль</li>
            <li>Интеграции логирования - Langfuse, OpenTelemetry</li>
            <li>Кэширование - Redis кэш для ответов</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
