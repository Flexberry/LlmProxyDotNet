'use client';

import { useState, useEffect } from 'react';
import { listApiKeys, createApiKey, revokeApiKey, type ApiKey } from '@/lib/api';
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Skeleton } from '@/components/ui/skeleton';
import { KeyForm } from './components/KeyForm';
import { Plus, Copy, Trash2, Calendar, Shield } from 'lucide-react';
import { cn } from '@/lib/utils';

const STORAGE_KEY = 'llm-proxy-api-keys';

export default function ApiKeysPage() {
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState(false);
  const [newKey, setNewKey] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  // Загрузка сохраненных ключей из localStorage
  const getStoredKeys = (): Record<string, string> => {
    if (typeof window === 'undefined') return {};
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored ? JSON.parse(stored) : {};
    } catch {
      return {};
    }
  };

  // Сохранение ключа в localStorage
  const storeKey = (id: string, key: string) => {
    if (typeof window === 'undefined') return;
    try {
      const stored = getStoredKeys();
      stored[id] = key;
      localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
    } catch (err) {
      console.error('Failed to store key:', err);
    }
  };

  const loadKeys = async () => {
    try {
      const data = await listApiKeys();
      const storedKeys = getStoredKeys();
      
      // Добавляем оригинальные ключи из localStorage
      const keysWithPlain = data.map(key => ({
        ...key,
        key: storedKeys[key.id] || undefined
      }));
      
      setKeys(keysWithPlain);
    } catch (e) {
      console.error('Failed to load keys:', e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadKeys(); }, []);

  const handleCreate = async (data: any) => {
    const permissions = data.permissions && data.permissions.length > 0 
      ? data.permissions 
      : ['*'];
    
    const response = await createApiKey({
      name: data.name,
      permissions,
      expiresAt: data.expiresAt ? new Date(data.expiresAt).toISOString() : undefined,
    });
    
    // Сохраняем оригинальный ключ в localStorage
    storeKey(response.apiKey.id, response.key);
    
    setNewKey(response.key);
    setOpen(false);
    await loadKeys();
  };

  const handleRevoke = async (keyId: string) => {
    if (!confirm('Отозвать этот ключ? Это действие нельзя отменить.')) return;
    
    // Удаляем из localStorage
    if (typeof window !== 'undefined') {
      try {
        const stored = getStoredKeys();
        delete stored[keyId];
        localStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
      } catch (err) {
        console.error('Failed to remove key from storage:', err);
      }
    }
    
    await revokeApiKey(keyId);
    await loadKeys();
  };

  const copyKey = async (key: string) => {
    await navigator.clipboard.writeText(key);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const getKeyToCopy = (key: ApiKey) => {
    // Если есть оригинальный ключ (только что создан) - используем его
    // Иначе - показываем предупреждение, что ключ недоступен
    return key.key || key.keyHash;
  };

  return (
    <div className="space-y-6 animate-fade-in">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">API Ключи</h1>
          <p className="text-sm text-muted-foreground">
            Управление ключами доступа к LLM провайдерам
          </p>
        </div>
        
        {/* Исправлено: onOpenChange передается в Dialog */}
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger asChild>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Создать ключ
            </Button>
          </DialogTrigger>
          <DialogContent className="sm:max-w-md">
            <DialogHeader>
              <DialogTitle>Новый API ключ</DialogTitle>
            </DialogHeader>
            <KeyForm onSubmit={handleCreate} onCancel={() => setOpen(false)} />
          </DialogContent>
        </Dialog>
      </div>

      {newKey && (
        <Alert className="bg-green-50 border-green-200 text-green-900">
          <AlertDescription className="flex items-center justify-between">
            <div>
              <strong>Ключ создан:</strong>{' '}
              <code className="font-mono text-sm">{newKey}</code>
            </div>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => copyKey(newKey)}
              className={cn('ml-2', copied && 'text-green-600')}
            >
              {copied ? 'Скопировано!' : <><Copy className="h-4 w-4" /> Копировать</>}
            </Button>
          </AlertDescription>
          <p className="text-xs mt-2 text-green-700">
            Сохраните этот ключ в безопасном месте
          </p>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Ваши ключи ({keys.length})</CardTitle>
          <CardDescription>
            Активные ключи для доступа к LLM провайдерам
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="space-y-3">
              {[1, 2, 3].map((i) => (
                <div key={i} className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="space-y-2 flex-1">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-3 w-48" />
                  </div>
                  <Skeleton className="h-8 w-16" />
                </div>
              ))}
            </div>
          ) : keys.length === 0 ? (
            <div className="text-center py-8 text-muted-foreground">
              <Shield className="mx-auto h-12 w-12 mb-4 opacity-50" />
              <p>Нет активных ключей</p>
              <p className="text-sm">Создайте первый ключ для начала работы</p>
            </div>
          ) : (
            <div className="space-y-3">
              {keys.map((key) => (
                <div
                  key={key.id}
                  className="flex items-start justify-between gap-4 p-4 border rounded-lg hover:bg-muted/50 transition-colors"
                >
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <span className="font-medium truncate">
                        {key.name || 'Без названия'}
                      </span>
                      {!key.isActive && (
                        <Badge variant="destructive" className="text-xs">
                          Отозван
                        </Badge>
                      )}
                    </div>
                    
                    <div className="text-xs text-muted-foreground space-y-1">
                      <p className="flex items-center gap-1">
                        <Shield className="h-3 w-3" />
                        {key.permissions === '*' 
                          ? 'Все модели' 
                          : key.permissions.split(',').slice(0, 3).join(', ') + (key.permissions.split(',').length > 3 ? '...' : '')}
                      </p>
                      {key.expiresAt && (
                        <p className="flex items-center gap-1">
                          <Calendar className="h-3 w-3" />
                          Истекает: {new Date(key.expiresAt).toLocaleDateString('ru-RU')}
                        </p>
                      )}
                      <p className="text-[10px] font-mono opacity-70">
                        {(key.key || key.keyHash).slice(0, 16)}...
                      </p>
                    </div>
                  </div>
                  
                  <div className="flex items-center gap-1">
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => copyKey(key.key || key.keyHash)}
                      title="Скопировать ключ"
                    >
                      <Copy className="h-4 w-4" />
                    </Button>
                    {key.isActive && (
                      <Button
                        variant="ghost"
                        size="icon"
                        onClick={() => handleRevoke(key.id)}
                        className="text-destructive hover:text-destructive hover:bg-destructive/10"
                        title="Отозвать ключ"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}