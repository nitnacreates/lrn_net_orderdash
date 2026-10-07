// Same-origin by default: Next rewrites /api/* to Central (see next.config.ts).
const API_BASE = process.env.NEXT_PUBLIC_API_BASE ?? "";

const TOKEN_KEY = "orderdash_token";

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem(TOKEN_KEY);
}

export function clearToken() {
  window.localStorage.removeItem(TOKEN_KEY);
}

export async function login(username: string, password: string): Promise<boolean> {
  const res = await fetch(`${API_BASE}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password })
  });
  if (!res.ok) return false;
  const data = (await res.json()) as { token: string };
  window.localStorage.setItem(TOKEN_KEY, data.token);
  return true;
}

async function withAuth(path: string, init: RequestInit = {}): Promise<Response> {
  return fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      ...(init.headers ?? {}),
      Authorization: `Bearer ${getToken() ?? ""}`
    },
    cache: "no-store"
  });
}

export async function apiGet<T>(path: string): Promise<T> {
  const res = await withAuth(path);
  if (!res.ok) throw new Error(`${res.status}`);
  return (await res.json()) as T;
}

export async function apiPost<T>(path: string): Promise<T> {
  const res = await withAuth(path, { method: "POST" });
  if (!res.ok) throw new Error(`${res.status}`);
  return (await res.json()) as T;
}
