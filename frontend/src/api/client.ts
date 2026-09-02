const API_BASE = import.meta.env.VITE_API_URL ?? "";

function buildUrl(path: string): string {
  if (!API_BASE) return path;
  return `${API_BASE.replace(/\/$/, "")}${path}`;
}

function getAccessToken(): string | null {
  return localStorage.getItem("accessToken");
}
function getRefreshToken(): string | null {
  return localStorage.getItem("refreshToken");
}
function setTokens(access: string, refresh: string) {
  localStorage.setItem("accessToken", access);
  localStorage.setItem("refreshToken", refresh);
}
function clearTokens() {
  localStorage.removeItem("accessToken");
  localStorage.removeItem("refreshToken");
}

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  if (refreshPromise) return refreshPromise;
  const rt = getRefreshToken();
  if (!rt) return false;
  refreshPromise = (async () => {
    try {
      const res = await fetch(buildUrl("/api/auth/refresh"), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: rt }),
      });
      if (!res.ok) {
        clearTokens();
        return false;
      }
      const data = await res.json();
      setTokens(data.accessToken, data.refreshToken);
      return true;
    } catch {
      clearTokens();
      return false;
    } finally {
      refreshPromise = null;
    }
  })();
  return refreshPromise;
}

export interface ApiError {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  raw?: unknown;
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers: Record<string, string> = { ...(init.headers as Record<string, string> | undefined) };
  const token = getAccessToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;
  if (!(init.body instanceof FormData) && init.body && !headers["Content-Type"]) {
    headers["Content-Type"] = "application/json";
  }

  const doFetch = async (): Promise<Response> =>
    fetch(buildUrl(path), { ...init, headers });

  let res = await doFetch();

  if (res.status === 401) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      const newToken = getAccessToken();
      if (newToken) headers["Authorization"] = `Bearer ${newToken}`;
      res = await fetch(buildUrl(path), { ...init, headers });
    }
  }

  if (res.status === 204) return undefined as unknown as T;
  const text = await res.text();
  const body: unknown = text ? JSON.parse(text) : null;

  if (!res.ok) {
    const err: ApiError = {
      status: res.status,
      raw: body,
    };
    if (body && typeof body === "object") {
      const b = body as Record<string, unknown>;
      err.title = (b.title as string) ?? (b.Title as string);
      err.detail = (b.detail as string) ?? (b.Detail as string) ?? (b.message as string);
      err.errors = (b.errors as Record<string, string[]>) ?? (b.Errors as Record<string, string[]>);
      if (!err.detail && !err.title) err.detail = text;
    } else {
      err.detail = text;
    }
    throw err;
  }
  return body as T;
}

export function saveAuth(accessToken: string, refreshToken: string) {
  setTokens(accessToken, refreshToken);
}
export function clearAuth() {
  clearTokens();
  localStorage.removeItem("user");
}
export function saveUser(user: unknown) {
  localStorage.setItem("user", JSON.stringify(user));
}
export function loadUser<T>(): T | null {
  const raw = localStorage.getItem("user");
  if (!raw) return null;
  try { return JSON.parse(raw) as T; } catch { return null; }
}
