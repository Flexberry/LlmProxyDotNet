'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog';
import { RateLimitConfigForm } from '@/app/keys/components/RateLimitConfigForm';
import { BudgetConfigForm } from '@/app/keys/components/BudgetConfigForm';
import { listApiKeys, getRateLimitStatus, getBudget, setBudget, type ApiKey, type RateLimitStatus, type Budget as BudgetType } from '@/lib/api';
import { Shield, AlertCircle, TrendingUp, RefreshCw } from 'lucide-react';

export default function RateLimitsPage() {
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedKey, setSelectedKey] = useState<ApiKey | null>(null);
  const [rateLimitStatus, setRateLimitStatus] = useState<RateLimitStatus | null>(null);
  const [budget, setBudgetData] = useState<BudgetType | null>(null);
  const [rateLimitDialogOpen, setRateLimitDialogOpen] = useState(false);
  const [budgetDialogOpen, setBudgetDialogOpen] = useState(false);
  const [refreshing, setRefreshing] = useState(false);

  useEffect(() => {
    loadKeys();
  }, []);

  const loadKeys = async () => {
    try {
      const data = await listApiKeys();
      setKeys(data);
    } catch (e) {
      console.error('Failed to load keys:', e);
    } finally {
      setLoading(false);
    }
  };

  const handleSelectKey = async (key: ApiKey) => {
    setSelectedKey(key);
    setRateLimitDialogOpen(true);
    setRefreshing(true);
    
    try {
      const status = await getRateLimitStatus(key.keyHash);
      setRateLimitStatus(status);
    } catch (e) {
      console.error('Failed to load rate limit status:', e);
    } finally {
      setRefreshing(false);
    }
  };

  const handleRateLimitConfigChange = async (config: {
    requestsPerMinute: number;
    tokensPerMinute: number;
    requestsPerDay: number;
    enabled: boolean;
  }) => {
    if (!selectedKey) return;
    
    // Note: This would require a backend endpoint to update rate limit config
    console.log('Rate limit config updated:', config);
    // TODO: Implement update rate limit config API call
  };

  const handleBudgetConfigChange = async (config: {
    budgetAmount: number;
    limitAction: 'warn' | 'block';
    enabled: boolean;
  }) => {
    if (!selectedKey) return;

    try {
      if (config.enabled && config.budgetAmount > 0) {
        await setBudget('ApiKey', selectedKey.id, {
          budgetAmount: config.budgetAmount,
          limitAction: config.limitAction,
        });
        // Reload budget
        const budgetData = await getBudget('ApiKey', selectedKey.id);
        setBudgetData(budgetData);
      }
    } catch (e) {
      console.error('Failed to update budget:', e);
    }
  };

  const handleRefresh = async () => {
    if (!selectedKey) return;
    setRefreshing(true);
    
    try {
      const [status, budgetData] = await Promise.all([
        getRateLimitStatus(selectedKey.keyHash),
        getBudget('ApiKey', selectedKey.id),
      ]);
      setRateLimitStatus(status);
      setBudgetData(budgetData);
    } catch (e) {
      console.error('Failed to refresh:', e);
    } finally {
      setRefreshing(false);
    }
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <div>
        <h1 className="text-2xl font-bold">Rate Limiting & Budget (v2)</h1>
        <p className="text-sm text-muted-foreground">
          Управление ограничениями и бюджетами для API ключей
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>API Ключи</CardTitle>
          <CardDescription>
            Выберите ключ для настройки rate limiting и budget
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="space-y-3">
              {[1, 2, 3].map((i) => (
                <div key={i} className="flex items-center justify-between p-4 border rounded-lg">
                  <Skeleton className="h-4 w-48" />
                  <Skeleton className="h-8 w-16" />
                </div>
              ))}
            </div>
          ) : keys.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              <p>Нет API ключей</p>
              <p className="text-sm">Создайте ключ сначала</p>
            </div>
          ) : (
            <div className="space-y-3">
              {keys.map((key) => (
                <div
                  key={key.id}
                  className="flex items-center justify-between p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                >
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <Shield className="h-4 w-4" />
                      <span className="font-medium">{key.name || 'Без названия'}</span>
                      <Badge variant={key.isActive ? 'default' : 'destructive'}>
                        {key.isActive ? 'Активен' : 'Отозван'}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted-foreground font-mono">
                      {key.keyHash.slice(0, 16)}...
                    </p>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleSelectKey(key)}
                  >
                    Настроить
                  </Button>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={rateLimitDialogOpen} onOpenChange={setRateLimitDialogOpen}>
        <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>
              Настройка для: {selectedKey?.name || 'API Key'}
            </DialogTitle>
          </DialogHeader>

          {selectedKey && (
            <div className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="font-mono text-xs bg-muted px-2 py-1 rounded">
                    {selectedKey.keyHash.slice(0, 16)}...
                  </span>
                </div>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleRefresh}
                  disabled={refreshing}
                >
                  <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
                </Button>
              </div>

              {/* Rate Limiting Section */}
              <div>
                <h3 className="text-lg font-semibold mb-3">Rate Limiting</h3>
                
                {rateLimitStatus && (
                  <Alert className="mb-4">
                    <TrendingUp className="h-4 w-4" />
                    <AlertDescription>
                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div>
                          <span className="text-muted-foreground">Запросов/мин:</span>{' '}
                          <span className="font-semibold">{rateLimitStatus.requestsThisMinute}</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Запросов/день:</span>{' '}
                          <span className="font-semibold">{rateLimitStatus.requestsThisDay}</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Токенов/мин:</span>{' '}
                          <span className="font-semibold">{rateLimitStatus.tokensThisMinute.toLocaleString()}</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Статус:</span>{' '}
                          <Badge variant={rateLimitStatus.isRateLimited ? 'destructive' : 'default'}>
                            {rateLimitStatus.isRateLimited ? 'Limited' : 'OK'}
                          </Badge>
                        </div>
                      </div>
                    </AlertDescription>
                  </Alert>
                )}

                <RateLimitConfigForm
                  onChange={handleRateLimitConfigChange}
                />
              </div>

              {/* Budget Section */}
              <div>
                <h3 className="text-lg font-semibold mb-3">Budget Management</h3>
                
                {budget && (
                  <Alert className="mb-4">
                    <AlertCircle className="h-4 w-4" />
                    <AlertDescription>
                      <div className="grid grid-cols-2 gap-4 text-sm">
                        <div>
                          <span className="text-muted-foreground">Бюджет:</span>{' '}
                          <span className="font-semibold">${budget.budgetAmount.toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Расход:</span>{' '}
                          <span className="font-semibold">${budget.currentSpending.toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Осталось:</span>{' '}
                          <span className="font-semibold">${(budget.budgetAmount - budget.currentSpending).toFixed(2)}</span>
                        </div>
                        <div>
                          <span className="text-muted-foreground">Использовано:</span>{' '}
                          <span className="font-semibold">{((budget.currentSpending / budget.budgetAmount) * 100).toFixed(1)}%</span>
                        </div>
                      </div>
                    </AlertDescription>
                  </Alert>
                )}

                <BudgetConfigForm
                  onChange={handleBudgetConfigChange}
                />
              </div>
            </div>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
