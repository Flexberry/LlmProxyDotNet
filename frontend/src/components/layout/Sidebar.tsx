'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { LayoutDashboard, Key, Blocks, Settings } from 'lucide-react';
import { cn } from '@/lib/utils';

/**
 * Navigation menu items configuration
 */
const navigation = [
  { name: 'Дашборд', href: '/', icon: LayoutDashboard },
  { name: 'API Ключи', href: '/keys', icon: Key },
  { name: 'Модели', href: '/models', icon: Blocks },
  { name: 'Настройки', href: '/settings', icon: Settings },
];

/**
 * Sidebar navigation component
 * @returns Sidebar JSX element
 */
export function Sidebar() {
  const pathname = usePathname();

  return (
    <aside className="hidden md:flex w-64 flex-col border-r bg-card">
      <div className="flex h-16 items-center gap-2 px-6 border-b">
        <span className="font-semibold text-lg">LLM Proxy</span>
      </div>
      
      <nav className="flex-1 space-y-1 px-3 py-4">
        {navigation.map((item) => {
          const isActive = item.href === '/' 
            ? pathname === '/' 
            : pathname === item.href || pathname.startsWith(item.href + '/');
          return (
            <Link
              key={item.name}
              href={item.href}
              className={cn(
                'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )}
            >
              <item.icon className="h-4 w-4" />
              {item.name}
            </Link>
          );
        })}
      </nav>

      <div className="p-4 border-t">
        <div className="text-xs text-muted-foreground">
          <p>v1.0.0</p>
          <p className="mt-1">.NET 10 + Next.js 16</p>
        </div>
      </div>
    </aside>
  );
}