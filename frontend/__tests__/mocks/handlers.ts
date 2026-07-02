import { http, HttpResponse, PathParams } from 'msw';

export const handlers = [
  // Admin endpoints
  http.get('/admin/keys', () => {
    return HttpResponse.json([
      {
        id: 'key-1',
        keyHash: 'abc123',
        name: 'Test Key',
        permissions: '*',
        isActive: true,
        createdAt: '2024-01-01T00:00:00Z',
      },
    ]);
  }),

  http.post('/admin/keys', async ({ request }) => {
    // Явное приведение типа к Record<string, unknown> или конкретному интерфейсу
    const body = await request.json() as Record<string, unknown>;
    
    return HttpResponse.json({
      key: 'sk_new_' + Math.random().toString(36).slice(2),
      apiKey: {
        id: 'key-new',
        keyHash: 'newhash',
        name: body.name || 'Unnamed',
        permissions: body.permissions || '*',
        isActive: true,
        createdAt: new Date().toISOString(),
      },
    }, { status: 201 });
  }),

  http.delete('/admin/keys/:id', ({ params }) => {
    return new HttpResponse(null, { status: 204 });
  }),

  // Stats endpoint
  http.get('/admin/stats', () => {
    return HttpResponse.json({
      totalRequests: 150,
      successCount: 140,
      errorCount: 10,
      avgLatencyMs: 245.5,
      totalPromptTokens: 5000,
      totalCompletionTokens: 8000,
      requestsByModel: { 'openai/gpt-4o': 100, 'ollama/llama3': 50 },
      requestsByProvider: { openai: 100, ollama: 50 },
    });
  }),

  // OpenAI-compatible endpoints
  http.post('/v1/chat/completions', async ({ request }) => {
    const body = await request.json() as { stream?: boolean };
    
    if (body.stream) {
      const encoder = new TextEncoder();
      const stream = new ReadableStream({
        start(controller) {
          const chunks = [
            'data: {"id":"test","object":"chat.completion.chunk","choices":[{"index":0,"delta":{"role":"assistant"}}]}\n\n',
            'data: {"id":"test","object":"chat.completion.chunk","choices":[{"index":0,"delta":{"content":"Hello"}}]}\n\n',
            'data: {"id":"test","object":"chat.completion.chunk","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}\n\n',
            'data: [DONE]\n\n',
          ];
          chunks.forEach(chunk => controller.enqueue(encoder.encode(chunk)));
          controller.close();
        },
      });
      return new HttpResponse(stream, {
        headers: { 'Content-Type': 'text/event-stream' },
      });
    }
    
    return HttpResponse.json({
      id: 'chatcmpl-test',
      object: 'chat.completion',
      created: Date.now(),
      model: 'openai/gpt-4o',
      choices: [{
        index: 0,
        message: { role: 'assistant', content: 'Hello from mock!' },
        finish_reason: 'stop',
      }],
      usage: { prompt_tokens: 10, completion_tokens: 5, total_tokens: 15 },
    });
  }),

  http.get('/v1/models', () => {
    return HttpResponse.json({
      object: 'list',
      data: [
        { id: 'openai/gpt-4o', object: 'model', created: Date.now(), owned_by: 'openai' },
        { id: 'ollama/llama3', object: 'model', created: Date.now(), owned_by: 'ollama' },
      ],
    });
  }),
];