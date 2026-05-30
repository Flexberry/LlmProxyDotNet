// frontend/app/api/proxy/route.ts
// Server-side proxy для обхода CORS и защиты Master Key

import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:4000';
const MASTER_KEY = process.env.LITELLM_MASTER_KEY;

export async function POST(request: NextRequest) {
  try {
    const { path, method = 'POST', body, headers: clientHeaders } = await request.json();
    
    const headers = new Headers();
    headers.set('Content-Type', 'application/json');
    
    // Добавляем Master Key для админ-запросов
    if (MASTER_KEY) {
      headers.set('X-Admin-Key', MASTER_KEY);
    }
    
    // Зарезервированные заголовки, которые клиент не может переопределить
    const reservedHeaders = new Set(['x-admin-key', 'x-master-key']);
    
    // Пробрасываем пользовательские заголовки (например, Authorization)
    if (clientHeaders) {
      Object.entries(clientHeaders).forEach(([key, value]) => {
        if (typeof value === 'string' && !reservedHeaders.has(key.toLowerCase())) {
          headers.set(key, value);
        }
      });
    }

    const response = await fetch(`${BACKEND_URL}${path}`, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    const data = await response.json();
    
    if (!response.ok) {
      return NextResponse.json(data, { status: response.status });
    }

    return NextResponse.json(data);
  } catch (error) {
    console.error('Proxy error:', error);
    return NextResponse.json(
      { error: 'Internal proxy error' }, 
      { status: 500 }
    );
  }
}

// Для streaming нужен отдельный endpoint с поддержкой ReadableStream
export async function POST_STREAM(request: NextRequest) {
  try {
    const { path, body, headers: clientHeaders } = await request.json();
    
    const headers = new Headers();
    headers.set('Content-Type', 'application/json');
    if (MASTER_KEY) headers.set('X-Admin-Key', MASTER_KEY);
    
    // Зарезервированные заголовки, которые клиент не может переопределить
    const reservedHeaders = new Set(['x-admin-key', 'x-master-key']);
    
    if (clientHeaders) {
      Object.entries(clientHeaders).forEach(([key, value]) => {
        if (typeof value === 'string' && !reservedHeaders.has(key.toLowerCase())) {
          headers.set(key, value);
        }
      });
    }

    const response = await fetch(`${BACKEND_URL}${path}`, {
      method: 'POST',
      headers,
      body: JSON.stringify({ ...body, stream: true }),
    });

    if (!response.ok || !response.body) {
      throw new Error('Failed to connect to backend');
    }

    // Пробрасываем SSE-поток клиенту
    return new NextResponse(response.body, {
      headers: {
        'Content-Type': 'text/event-stream',
        'Cache-Control': 'no-cache',
        'Connection': 'keep-alive',
      },
    });
  } catch (error) {
    console.error('Stream proxy error:', error);
    return NextResponse.json(
      { error: 'Internal proxy error' }, 
      { status: 500 }
    );
  }
}