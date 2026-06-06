'use client';

import { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Sliders } from 'lucide-react';

interface RateLimitConfigFormProps {
  initialConfig?: {
    requestsPerMinute: number;
    tokensPerMinute: number;
    requestsPerDay: number;
  };
  onChange: (config: {
    requestsPerMinute: number;
    tokensPerMinute: number;
    requestsPerDay: number;
    enabled: boolean;
  }) => void;
}

export function RateLimitConfigForm({ initialConfig, onChange }: RateLimitConfigFormProps) {
  const [enabled, setEnabled] = useState(!!initialConfig);
  const [requestsPerMinute, setRequestsPerMinute] = useState(initialConfig?.requestsPerMinute || 100);
  const [tokensPerMinute, setTokensPerMinute] = useState(initialConfig?.tokensPerMinute || 100000);
  const [requestsPerDay, setRequestsPerDay] = useState(initialConfig?.requestsPerDay || 10000);

  const handleEnabledChange = (checked: boolean) => {
    setEnabled(checked);
    onChange({
      requestsPerMinute,
      tokensPerMinute,
      requestsPerDay,
      enabled: checked,
    });
  };

  const handleRequestsPerMinuteChange = (value: number) => {
    setRequestsPerMinute(value);
    if (enabled) {
      onChange({
        requestsPerMinute: value,
        tokensPerMinute,
        requestsPerDay,
        enabled: true,
      });
    }
  };

  const handleTokensPerMinuteChange = (value: number) => {
    setTokensPerMinute(value);
    if (enabled) {
      onChange({
        requestsPerMinute,
        tokensPerMinute: value,
        requestsPerDay,
        enabled: true,
      });
    }
  };

  const handleRequestsPerDayChange = (value: number) => {
    setRequestsPerDay(value);
    if (enabled) {
      onChange({
        requestsPerMinute,
        tokensPerMinute,
        requestsPerDay: value,
        enabled: true,
      });
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <Sliders className="h-4 w-4" />
          Rate Limiting (v2)
        </CardTitle>
        <CardDescription>
          Настройка ограничений на количество запросов
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center justify-between">
          <Label>Включить Rate Limiting</Label>
          <input
            type="checkbox"
            checked={enabled}
            onChange={(e) => handleEnabledChange(e.target.checked)}
            className="h-4 w-4"
          />
        </div>

        {enabled && (
          <div className="space-y-4 pt-2">
            <div className="space-y-2">
              <Label>Запросов в минуту: {requestsPerMinute}</Label>
              <input
                type="range"
                min="1"
                max="1000"
                value={requestsPerMinute}
                onChange={(e) => handleRequestsPerMinuteChange(parseInt(e.target.value))}
                className="w-full"
              />
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>1</span>
                <span>1000</span>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Токенов в минуту: {tokensPerMinute.toLocaleString()}</Label>
              <input
                type="range"
                min="100"
                max="1000000"
                step="1000"
                value={tokensPerMinute}
                onChange={(e) => handleTokensPerMinuteChange(parseInt(e.target.value))}
                className="w-full"
              />
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>100</span>
                <span>1M</span>
              </div>
            </div>

            <div className="space-y-2">
              <Label>Запросов в день: {requestsPerDay.toLocaleString()}</Label>
              <input
                type="range"
                min="10"
                max="100000"
                value={requestsPerDay}
                onChange={(e) => handleRequestsPerDayChange(parseInt(e.target.value))}
                className="w-full"
              />
              <div className="flex justify-between text-xs text-muted-foreground">
                <span>10</span>
                <span>100K</span>
              </div>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
