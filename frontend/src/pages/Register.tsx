import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { apiFetch, saveAuth, saveUser } from "../api/client";
import { useAuthStore } from "../store/authStore";
import type { AuthResultDto } from "../types";
import { Card, PageShell } from "../components/Ui";
import { Button } from "../components/Button";
import { Input, Label, Select } from "../components/Field";

export default function Register() {
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [phone, setPhone] = useState("");
  const [role, setRole] = useState<"customer" | "shop">("customer");
  const [err, setErr] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const setUser = useAuthStore((s) => s.setUser);

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErr(null); setLoading(true);
    try {
      const data = await apiFetch<AuthResultDto>("/api/auth/register", {
        method: "POST",
        body: JSON.stringify({ email, password, fullName, phone: phone || undefined, registerAs: role }),
      });
      saveAuth(data.tokens.accessToken, data.tokens.refreshToken);
      saveUser(data.user);
      setUser(data.user as unknown as import("../types").UserDto);
      if (data.user.role === "ShopOwner") navigate("/shop", { replace: true });
      else navigate("/customer", { replace: true });
    } catch (ex: unknown) {
      const e = ex as { detail?: string; title?: string; errors?: Record<string, string[]>; raw?: unknown };
      const raw = e.raw as Record<string, unknown> | undefined;
      const msg = e.detail ?? e.title ?? (raw?.detail as string) ?? "Registration failed.";
      if (e.errors) {
        const first = Object.values(e.errors).flat()[0];
        setErr(first ?? msg);
      } else setErr(msg);
    } finally { setLoading(false); }
  };

  return (
    <PageShell>
      <div className="mx-auto max-w-md">
        <Card>
          <h1 className="text-xl font-bold">Create your account</h1>
          <p className="mt-1 text-sm text-gray-500">Customer or shop owner — choose how you&apos;ll use LocalLive.</p>
          <form onSubmit={onSubmit} className="mt-6 space-y-4">
            <div>
              <Label>I am a</Label>
              <Select value={role} onChange={(e) => setRole(e.target.value as "customer" | "shop")}>
                <option value="customer">Customer — I need things right now</option>
                <option value="shop">Shop owner — I want to receive LIVE requests</option>
              </Select>
            </div>
            <div>
              <Label htmlFor="fullName">Full name</Label>
              <Input id="fullName" required value={fullName} onChange={(e) => setFullName(e.target.value)} placeholder="Alex Kumar" />
            </div>
            <div>
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" required value={email} onChange={(e) => setEmail(e.target.value)} placeholder="you@example.com" />
            </div>
            <div>
              <Label htmlFor="phone">Phone (optional)</Label>
              <Input id="phone" value={phone} onChange={(e) => setPhone(e.target.value)} placeholder="+91 90000 00000" />
            </div>
            <div>
              <Label htmlFor="password">Password (min 8 characters)</Label>
              <Input id="password" type="password" required value={password} onChange={(e) => setPassword(e.target.value)} placeholder="••••••••" />
            </div>
            {err && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{err}</div>}
            <Button type="submit" disabled={loading} className="w-full">{loading ? "Creating account…" : "Create account"}</Button>
            <p className="text-center text-sm text-gray-600">Already have an account? <Link to="/login" className="font-semibold text-gray-900 underline">Log in</Link></p>
          </form>
        </Card>
      </div>
    </PageShell>
  );
}
