'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { AlertCircle } from 'lucide-react';

export default function SettingsPage() {
  return (
    <div className="space-y-6 animate-fade-in">
      <div>
        <h1 className="text-2xl font-bold">Настройки</h1>
        <p className="text-sm text-muted-foreground">
          Глобальные настройки прокси-сервера
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-amber-500" />
            Функция в разработке
          </CardTitle>
          <CardDescription>
            Расширенные настройки будут доступны в версии 2.0
          </CardDescription>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          <p>В текущей версии (v1) поддерживаются только базовые функции:</p>
          <ul className="list-disc list-inside mt-2 space-y-1">
            <li>Управление API ключами</li>
            <li>Маршрутизация запросов</li>
            <li>Просмотр статистики</li>
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}