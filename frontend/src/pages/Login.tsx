import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { apiFetch, saveAuth, saveUser } from "../api/client";
import { useAuthStore } from "../store/authStore";
import type { AuthResultDto } from "../types";
import { Card, PageShell } from "../components/Ui";
import { Button } from "../components/Button";
import { Input, Label, FieldError } from "../components/Field";

export default function Login() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const setUser = useAuthStore((s) => s.setUser);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(null); setLoading(true);
    try {
      const data = await apiFetch<AuthResultDto>("/api/auth/login", { method: "POST", body: JSON.stringify({ email, password }) });
      saveAuth(data.tokens.accessToken, data.tokens.refreshToken);
      saveUser(data.user);
      setUser(data.user as unknown as import("../types").UserDto);
      if (data.user.role === "Admin") navigate("/admin", { replace: true });
      else if (data.user.role === "ShopOwner") navigate("/shop", { replace: true });
      else navigate("/customer", { replace: true });
    } catch (ex: unknown) {
      const e = ex as { detail?: string; title?: string };
      const msg = e.detail ?? e.title ?? "Login failed. Check your credentials.";
      setErr(msg);
    } finally { setLoading(false); }
  };

  return (
    <PageShell>
      <div className="mx-auto max-w-md">
        <Card>
          <h1 className="text-xl font-bold">Welcome back</h1>
          <p className="mt-1 text-sm text-gray-500">Log in to continue to LocalLive.</p>
          <form onSubmit={onSubmit} className="mt-6 space-y-4">
            <div>
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@example.com" />
            </div>
            <div>
              <Label htmlFor="password">Password</Label>
              <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" />
            </div>
            {err && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{err}</div>}
            <Button type="submit" disabled={loading} className="w-full">{loading ? "Signing in…" : "Log in"}</Button>
            <p className="text-center text-sm text-gray-600">No account? <Link to="/register" className="font-semibold text-gray-900 underline">Create one</Link></p>
          </form>
          <div className="mt-6 rounded-lg bg-gray-50 p-3 text-xs text-gray-600">
            <p className="font-semibold">Demo accounts (when SeedDemoData is enabled):</p>
            <p>customer@example.com / DemoPass123! · shop1@example.com / DemoPass123! · admin@locallive.app / Admin123!</p>
          </div>
        </Card>
      </div>
    </PageShell>
  );
}

export function FieldWrap({ label, children, error }: { label: string; children: React.ReactNode; error?: string }) {
  return (
    <div>
      <Label>{label}</Label>
      {children}
      <FieldError message={error} />
    </div>
  );
}
