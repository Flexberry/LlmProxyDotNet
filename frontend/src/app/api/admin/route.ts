// frontend/src/app/api/admin/route.ts
// Server-side API route для административных операций с Master Key

import { NextRequest, NextResponse } from 'next/server';

const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:4000';
const MASTER_KEY = process.env.LITELLM_MASTER_KEY;
const ADMIN_SECRET = process.env.ADMIN_SECRET;

// Проверка авторизации администратора
function verifyAdminAuth(request: NextRequest): boolean {
  if (!ADMIN_SECRET) {
    // Если ADMIN_SECRET не настроен, разрешаем только в development
    return process.env.NODE_ENV === 'development';
  }
  
  const authHeader = request.headers.get('Authorization');
  if (authHeader && authHeader.startsWith('Bearer ')) {
    const token = authHeader.slice(7);
    return token === ADMIN_SECRET;
  }
  
  return false;
}

// Проверка разрешённых endpoint'ов
const ALLOWED_ENDPOINTS = [
  '/admin/keys',
  '/admin/keys/',
  '/admin/stats',
  '/v1/models',
];

function isEndpointAllowed(endpoint: string): boolean {
  return ALLOWED_ENDPOINTS.some(allowed => endpoint.startsWith(allowed));
}

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  const endpoint = searchParams.get('endpoint');
  
  if (!endpoint) {
    return NextResponse.json({ error: 'Missing endpoint parameter' }, { status: 400 });
  }

  // Проверка авторизации
  if (!verifyAdminAuth(request)) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  // Проверка разрешённого endpoint'а
  if (!isEndpointAllowed(endpoint)) {
    return NextResponse.json({ error: 'Forbidden: endpoint not allowed' }, { status: 403 });
  }

  const headers = new Headers();
  headers.set('Content-Type', 'application/json');
  
  if (MASTER_KEY) {
    headers.set('X-Admin-Key', MASTER_KEY);
  }

  try {
    const response = await fetch(`${BACKEND_URL}${endpoint}`, {
      method: 'GET',
      headers,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      return NextResponse.json(error, { status: response.status });
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('Admin API GET error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}

export async function POST(request: NextRequest) {
  try {
    const { endpoint, body } = await request.json();
    
    if (!endpoint) {
      return NextResponse.json({ error: 'Missing endpoint parameter' }, { status: 400 });
    }

    // Проверка авторизации
    if (!verifyAdminAuth(request)) {
      return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }

    // Проверка разрешённого endpoint'а
    if (!isEndpointAllowed(endpoint)) {
      return NextResponse.json({ error: 'Forbidden: endpoint not allowed' }, { status: 403 });
    }

    const headers = new Headers();
    headers.set('Content-Type', 'application/json');
    
    if (MASTER_KEY) {
      headers.set('X-Admin-Key', MASTER_KEY);
    }

    const response = await fetch(`${BACKEND_URL}${endpoint}`, {
      method: 'POST',
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      return NextResponse.json(error, { status: response.status });
    }

    const data = await response.json();
    return NextResponse.json(data);
  } catch (error) {
    console.error('Admin API POST error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}

export async function DELETE(request: NextRequest) {
  // Поддерживаем endpoint как в query params, так и в body
  const { searchParams } = new URL(request.url);
  let endpoint = searchParams.get('endpoint');
  let body: any = null;
  
  // Если endpoint не в query params, пробуем получить из body
  if (!endpoint) {
    try {
      const jsonBody = await request.json();
      endpoint = jsonBody.endpoint;
      body = jsonBody.body;
    } catch {
      return NextResponse.json({ error: 'Missing endpoint parameter' }, { status: 400 });
    }
  }

  if (!endpoint) {
    return NextResponse.json({ error: 'Missing endpoint parameter' }, { status: 400 });
  }

  // Проверка авторизации
  if (!verifyAdminAuth(request)) {
    return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
  }

  // Проверка разрешённого endpoint'а
  if (!isEndpointAllowed(endpoint)) {
    return NextResponse.json({ error: 'Forbidden: endpoint not allowed' }, { status: 403 });
  }

  const headers = new Headers();
  headers.set('Content-Type', 'application/json');
  
  if (MASTER_KEY) {
    headers.set('X-Admin-Key', MASTER_KEY);
  }

  try {
    const response = await fetch(`${BACKEND_URL}${endpoint}`, {
      method: 'DELETE',
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      return NextResponse.json(error, { status: response.status });
    }

    const data = await response.json();
    return NextResponse.json(data ?? { success: true });
  } catch (error) {
    console.error('Admin API DELETE error:', error);
    return NextResponse.json({ error: 'Internal server error' }, { status: 500 });
  }
}
