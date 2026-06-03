'use client';

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { getStats, type LogStats } from '@/lib/api';
import { Bot, CheckCircle, Clock, AlertCircle } from 'lucide-react';

export default function DashboardPage() {
  const [stats, setStats] = useState<LogStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const to = new Date();
        const from = new Date(to.getTime() - 7 * 24 * 60 * 60 * 1000);
        setStats(await getStats(from, to));
      } catch (e) {
        console.error('Failed to load stats:', e);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const cards = [
    {
      title: 'Всего запросов',
      value: stats?.totalRequests ?? 0,
      icon: Bot,
      color: 'text-blue-600',
    },
    {
      title: 'Успешные',
      value: stats?.successCount ?? 0,
      icon: CheckCircle,
      color: 'text-green-600',
    },
    {
      title: 'Средняя задержка',
      value: `${stats?.avgLatencyMs ?? 0} мс`,
      icon: Clock,
      color: 'text-amber-600',
    },
    {
      title: 'Ошибки',
      value: stats?.errorCount ?? 0,
      icon: AlertCircle,
      color: 'text-red-600',
    },
  ];

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Дашборд</h1>
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {cards.map((_, i) => (
            <Card key={i}>
              <CardHeader className="pb-2"><Skeleton className="h-4 w-24" /></CardHeader>
              <CardContent><Skeleton className="h-8 w-16" /></CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Дашборд</h1>
        <span className="text-sm text-muted-foreground">
          Последняя неделя
        </span>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {cards.map((card) => (
          <Card key={card.title}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium text-muted-foreground">
                {card.title}
              </CardTitle>
              <card.icon className={`h-4 w-4 ${card.color}`} />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{card.value}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      {stats?.requestsByModel && Object.keys(stats.requestsByModel).length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Популярные модели</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {Object.entries(stats.requestsByModel)
                .sort(([, a], [, b]) => (b as number) - (a as number))
                .slice(0, 5)
                .map(([model, count]) => (
                  <div key={model} className="flex items-center justify-between">
                    <span className="font-mono text-sm">{model}</span>
                    <span className="text-sm text-muted-foreground">
                      {count as number} запросов
                    </span>
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}

      {stats?.requestsByProvider && Object.keys(stats.requestsByProvider).length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Использование провайдеров</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {Object.entries(stats.requestsByProvider)
                .sort(([, a], [, b]) => (b as number) - (a as number))
                .map(([provider, count]) => (
                  <div key={provider} className="flex items-center justify-between">
                    <span className="text-sm capitalize">{provider}</span>
                    <span className="text-sm text-muted-foreground">
                      {count as number} запросов
                    </span>
                  </div>
                ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}