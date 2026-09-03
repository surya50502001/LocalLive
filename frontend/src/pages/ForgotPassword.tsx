import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { apiFetch } from "../api/client";
import { Card, PageShell } from "../components/Ui";
import { Button } from "../components/Button";
import { Input, Label } from "../components/Field";

export default function ForgotPassword() {
  const [step, setStep] = useState<"request" | "reset">("request");
  const [email, setEmail] = useState("");
  const [token, setToken] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const navigate = useNavigate();

  const handleRequestToken = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErr(null);
    setMsg(null);
    try {
      const res = await apiFetch<{ message: string; token?: string }>("/api/auth/forgot-password", {
        method: "POST",
        body: JSON.stringify({ email }),
      });
      setMsg(res.message);
      if (res.token) {
        setToken(res.token); // Pre-fill token for demo convenience
      }
      setStep("reset");
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Failed to initiate reset.");
    } finally {
      setLoading(false);
    }
  };

  const handleResetPassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setErr(null);
    setMsg(null);
    try {
      await apiFetch("/api/auth/reset-password", {
        method: "POST",
        body: JSON.stringify({ email, token, newPassword }),
      });
      alert("Password reset successfully! You can now log in.");
      navigate("/login");
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Invalid or expired token.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <PageShell>
      <div className="mx-auto max-w-md">
        <Card>
          <h1 className="text-xl font-bold">Reset Password</h1>
          <p className="mt-1 text-sm text-gray-500">
            {step === "request"
              ? "Enter your email to receive a password reset code."
              : "Enter the code and your new password."}
          </p>

          {msg && (
            <div className="mt-4 rounded-lg bg-emerald-50 border border-emerald-200 p-3 text-xs font-semibold text-emerald-800">
              {msg}
            </div>
          )}

          {err && (
            <div className="mt-4 rounded-lg bg-red-50 border border-red-200 p-3 text-xs font-semibold text-red-700">
              {err}
            </div>
          )}

          {step === "request" ? (
            <form onSubmit={handleRequestToken} className="mt-6 space-y-4">
              <div>
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="you@example.com"
                />
              </div>

              <Button type="submit" disabled={loading} className="w-full">
                {loading ? "Sending Code…" : "Send Reset Code"}
              </Button>

              <p className="text-center text-xs text-gray-600">
                Remember your password?{" "}
                <Link to="/login" className="font-semibold text-gray-900 underline">
                  Back to login
                </Link>
              </p>
            </form>
          ) : (
            <form onSubmit={handleResetPassword} className="mt-6 space-y-4">
              <div>
                <Label htmlFor="token">6-Digit Reset Code</Label>
                <Input
                  id="token"
                  type="text"
                  required
                  value={token}
                  onChange={(e) => setToken(e.target.value)}
                  placeholder="123456"
                />
              </div>

              <div>
                <Label htmlFor="newPassword">New Password (min 8 chars)</Label>
                <Input
                  id="newPassword"
                  type="password"
                  required
                  minLength={8}
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  placeholder="••••••••"
                />
              </div>

              <Button type="submit" disabled={loading} className="w-full">
                {loading ? "Resetting…" : "Set New Password"}
              </Button>

              <button
                type="button"
                onClick={() => setStep("request")}
                className="w-full text-center text-xs text-gray-600 hover:text-gray-900 underline"
              >
                ← Back to enter email
              </button>
            </form>
          )}
        </Card>
      </div>
    </PageShell>
  );
}
