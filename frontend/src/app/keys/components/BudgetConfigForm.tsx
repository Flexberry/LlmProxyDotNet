'use client';

import { useState } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { AlertCircle } from 'lucide-react';

interface BudgetConfigFormProps {
  initialBudget?: {
    budgetAmount: number;
    limitAction: 'warn' | 'block';
  };
  onChange: (config: {
    budgetAmount: number;
    limitAction: 'warn' | 'block';
    enabled: boolean;
  }) => void;
}

export function BudgetConfigForm({ initialBudget, onChange }: BudgetConfigFormProps) {
  const [enabled, setEnabled] = useState(!!initialBudget);
  const [budgetAmount, setBudgetAmount] = useState(initialBudget?.budgetAmount?.toString() || '');
  const [limitAction, setLimitAction] = useState<'warn' | 'block'>(initialBudget?.limitAction || 'warn');

  const handleEnabledChange = (checked: boolean) => {
    setEnabled(checked);
    onChange({
      budgetAmount: parseFloat(budgetAmount) || 0,
      limitAction,
      enabled: checked,
    });
  };

  const handleBudgetAmountChange = (value: string) => {
    setBudgetAmount(value);
    const numValue = parseFloat(value) || 0;
    if (enabled) {
      onChange({
        budgetAmount: numValue,
        limitAction,
        enabled: true,
      });
    }
  };

  const handleLimitActionChange = (action: 'warn' | 'block') => {
    setLimitAction(action);
    if (enabled) {
      onChange({
        budgetAmount: parseFloat(budgetAmount) || 0,
        limitAction: action,
        enabled: true,
      });
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-base">
          <AlertCircle className="h-4 w-4" />
          Budget Management (v2)
        </CardTitle>
        <CardDescription>
          Настройка бюджета и ограничений по расходам
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="flex items-center justify-between">
          <Label>Включить Budget Management</Label>
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
              <Label htmlFor="budgetAmount">Бюджет ($)</Label>
              <Input
                id="budgetAmount"
                type="number"
                step="0.01"
                min="0"
                value={budgetAmount}
                onChange={(e) => handleBudgetAmountChange(e.target.value)}
                placeholder="0.00"
              />
              <p className="text-xs text-muted-foreground">
                Максимальная сумма расходов за период
              </p>
            </div>

            <div className="space-y-2">
              <Label>Действие при превышении бюджета</Label>
              <div className="flex gap-4">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="budgetAction"
                    value="warn"
                    checked={limitAction === 'warn'}
                    onChange={() => handleLimitActionChange('warn')}
                  />
                  <span className="text-sm">Только предупреждение</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="budgetAction"
                    value="block"
                    checked={limitAction === 'block'}
                    onChange={() => handleLimitActionChange('block')}
                  />
                  <span className="text-sm">Блокировать запросы</span>
                </label>
              </div>
            </div>

            <div className="bg-muted p-3 rounded-md text-xs text-muted-foreground">
              <p className="font-semibold mb-1">Как это работает:</p>
              <ul className="list-disc list-inside space-y-1">
                <li>Система отслеживает расходы по всем запросам</li>
                <li>При достижении {limitAction === 'warn' ? 'предупреждения' : 'лимита'} будет выполнено соответствующее действие</li>
                <li>Расходы рассчитываются на основе количества токенов и тарифов провайдера</li>
              </ul>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
