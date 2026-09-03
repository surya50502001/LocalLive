import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiFetch } from "../../api/client";
import type { UserDto } from "../../types";
import { useAuthStore } from "../../store/authStore";
import { PageShell, Card, Badge, Spinner } from "../../components/Ui";
import { Button } from "../../components/Button";
import { Input, Label } from "../../components/Field";

export default function CustomerProfile() {
  const { user, setUser, logout } = useAuthStore();
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [saving, setSaving] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    if (user) {
      setFullName(user.fullName);
      setPhone(user.phone || "");
    }
  }, [user]);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setMsg(null);
    setErr(null);

    try {
      const updated = await apiFetch<UserDto>("/api/auth/profile", {
        method: "PUT",
        body: JSON.stringify({ fullName, phone: phone || null }),
      });
      setUser(updated);
      setMsg("Profile updated successfully!");
      setTimeout(() => setMsg(null), 3000);
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Failed to update profile.");
    } finally {
      setSaving(false);
    }
  };

  if (!user) {
    return (
      <PageShell>
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      </PageShell>
    );
  }

  return (
    <PageShell>
      <div className="mx-auto max-w-xl space-y-5">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-extrabold text-gray-900">My Account</h1>
          <Badge tone="green">{user.role}</Badge>
        </div>

        {msg && (
          <div className="rounded-xl bg-emerald-50 border border-emerald-200 p-3 text-xs font-bold text-emerald-800">
            {msg}
          </div>
        )}

        {err && (
          <div className="rounded-xl bg-red-50 border border-red-200 p-3 text-xs font-bold text-red-800">
            {err}
          </div>
        )}

        <Card>
          <form onSubmit={handleSave} className="space-y-4">
            <div>
              <Label>Email Address</Label>
              <Input value={user.email} disabled className="bg-gray-100 text-gray-500 cursor-not-allowed" />
              <p className="mt-1 text-[11px] text-gray-400">Email cannot be changed.</p>
            </div>

            <div>
              <Label>Full Name *</Label>
              <Input
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                required
                placeholder="Your Name"
              />
            </div>

            <div>
              <Label>Phone Number</Label>
              <Input
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="+1 555-0199"
              />
            </div>

            <Button type="submit" disabled={saving} className="w-full">
              {saving ? "Saving Changes…" : "Save Profile"}
            </Button>
          </form>
        </Card>

        {/* Quick Links */}
        <Card className="space-y-3">
          <h2 className="text-sm font-bold text-gray-900">Account Navigation</h2>
          <div className="flex flex-col gap-2">
            <Link
              to="/customer/favorites"
              className="flex items-center justify-between p-2.5 rounded-xl bg-gray-50 hover:bg-gray-100 text-xs font-semibold text-gray-800 transition"
            >
              <span>⭐ My Saved Favorite Shops</span>
              <span>→</span>
            </Link>
            <Link
              to="/customer/requests"
              className="flex items-center justify-between p-2.5 rounded-xl bg-gray-50 hover:bg-gray-100 text-xs font-semibold text-gray-800 transition"
            >
              <span>📋 My Live Requests History</span>
              <span>→</span>
            </Link>
          </div>
        </Card>

        <div className="pt-2 flex justify-center">
          <button
            onClick={logout}
            className="text-xs font-bold text-red-600 hover:text-red-700 underline"
          >
            Log out from LocalLive
          </button>
        </div>
      </div>
    </PageShell>
  );
}
