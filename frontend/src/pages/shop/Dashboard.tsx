import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiFetch } from "../../api/client";
import type { CategoryDto, ShopDto } from "../../types";
import { PageShell, Card, Badge, Spinner } from "../../components/Ui";
import { Button, SecondaryButton } from "../../components/Button";
import { Input, Textarea, Label, Select } from "../../components/Field";
import { getCurrentPosition } from "../../lib/geo";

export default function ShopDashboard() {
  const [shop, setShop] = useState<ShopDto | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [loading, setLoading] = useState(true);
  const [toggling, setToggling] = useState(false);
  const [msg, setMsg] = useState<string | null>(null);

  // onboarding form
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [address, setAddress] = useState("");
  const [lat, setLat] = useState<number | "">("");
  const [lng, setLng] = useState<number | "">("");
  const [catIds, setCatIds] = useState<string[]>([]);
  const [imageUrl, setImageUrl] = useState("");
  const [creating, setCreating] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const loadShop = async () => {
    setLoading(true);
    try {
      const data = await apiFetch<ShopDto>("/api/shops/me");
      setShop(data); setNotFound(false);
    } catch (ex: unknown) {
      const status = (ex as { status?: number })?.status;
      if (status === 404) setNotFound(true);
      else setMsg((ex as { detail?: string })?.detail ?? "Failed to load shop.");
    } finally { setLoading(false); }
  };
  useEffect(() => { loadShop(); apiFetch<CategoryDto[]>("/api/categories").then(setCategories).catch(() => {}); }, []);

  const locate = async () => {
    try { const p = await getCurrentPosition(); setLat(p.latitude); setLng(p.longitude); } catch (e: unknown) { setErr((e as Error).message); }
  };

  const toggleOpen = async () => {
    if (!shop) return;
    setToggling(true); setMsg(null);
    try {
      const updated = await apiFetch<ShopDto>(`/api/shops/${shop.id}/status`, { method: "PUT", body: JSON.stringify({ isOpen: !shop.isOpen }) });
      setShop(updated);
    } catch (ex: unknown) { setMsg((ex as { detail?: string })?.detail ?? "Failed to update status."); }
    finally { setToggling(false); }
  };

  const createShop = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!catIds.length) { setErr("Select at least one category."); return; }
    setErr(null); setCreating(true);
    try {
      const data = await apiFetch<ShopDto>("/api/shops", { method: "POST", body: JSON.stringify({ name, phone, address, latitude: Number(lat), longitude: Number(lng), categoryIds: catIds, imageUrl: imageUrl || undefined }) });
      setShop(data); setNotFound(false);
    } catch (ex: unknown) {
      const msg2 = (ex as { detail?: string })?.detail ?? "Failed to create shop.";
      const errs = (ex as { errors?: Record<string, string[]> })?.errors;
      setErr(errs ? Object.values(errs).flat().join(" ") : msg2);
    } finally { setCreating(false); }
  };

  if (loading) return <PageShell><div className="flex justify-center py-16"><Spinner /></div></PageShell>;

  if (notFound) {
    return (
      <PageShell>
        <div className="mx-auto max-w-2xl">
          <h1 className="text-xl font-bold">Set up your shop</h1>
          <p className="text-sm text-gray-600">Customers nearby will see your shop when you are verified and OPEN.</p>
          <Card className="mt-4">
            <form onSubmit={createShop} className="space-y-4">
              <div><Label>Shop name *</Label><Input value={name} onChange={(e) => setName(e.target.value)} required placeholder="ABC Fashion" /></div>
              <div><Label>Phone *</Label><Input value={phone} onChange={(e) => setPhone(e.target.value)} required placeholder="9876543210" /></div>
              <div><Label>Address *</Label><Textarea value={address} onChange={(e) => setAddress(e.target.value)} required placeholder="Street, area, city" rows={2} /></div>
              <div className="grid grid-cols-2 gap-3">
                <div><Label>Latitude *</Label><Input type="number" step="any" value={lat} onChange={(e) => setLat(e.target.value === "" ? "" : Number(e.target.value))} required /></div>
                <div><Label>Longitude *</Label><Input type="number" step="any" value={lng} onChange={(e) => setLng(e.target.value === "" ? "" : Number(e.target.value))} required /></div>
              </div>
              <SecondaryButton type="button" onClick={locate}>Use my location</SecondaryButton>
              <div>
                <Label>Categories *</Label>
                <div className="grid grid-cols-2 gap-2">
                  {categories.map((c) => (
                    <label key={c.id} className="flex items-center gap-2 rounded-lg border px-3 py-2 text-sm">
                      <input type="checkbox" checked={catIds.includes(c.id)} onChange={(e) => setCatIds((prev) => e.target.checked ? [...prev, c.id] : prev.filter((x) => x !== c.id))} />
                      {c.icon} {c.name}
                    </label>
                  ))}
                </div>
              </div>
              <div><Label>Shop image URL (optional)</Label><Input value={imageUrl} onChange={(e) => setImageUrl(e.target.value)} placeholder="https://…" /></div>
              {err && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{err}</div>}
              <Button type="submit" disabled={creating} className="w-full">{creating ? "Creating…" : "Create shop"}</Button>
            </form>
          </Card>
        </div>
      </PageShell>
    );
  }

  if (!shop) return <PageShell><Card><p className="text-sm text-red-600">{msg ?? "No shop found."}</p></Card></PageShell>;

  return (
    <PageShell>
      <div className="mx-auto max-w-3xl space-y-4">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-bold">Shop dashboard</h1>
          <Link to="/shop/requests" className="text-sm font-semibold text-gray-900 underline">Live requests →</Link>
        </div>
        <Card>
          <div className="flex items-start justify-between gap-4">
            <div>
              <h2 className="text-lg font-bold">{shop.name}</h2>
              <p className="text-sm text-gray-600">{shop.address} · {shop.phone}</p>
              <p className="mt-1 text-xs text-gray-500">{shop.categories.map((c) => c.name).join(", ")}</p>
              <div className="mt-2 flex gap-2">
                <Badge tone={shop.status === "Verified" ? "green" : shop.status === "Pending" ? "amber" : "red"}>{shop.status}</Badge>
                <Badge tone={shop.isOpen ? "green" : "gray"}>{shop.isOpen ? "OPEN" : "CLOSED"}</Badge>
              </div>
              {shop.status !== "Verified" && <p className="mt-2 text-xs text-amber-700">Your shop is pending verification. An admin will verify it soon. Only verified OPEN shops receive LIVE requests.</p>}
            </div>
            {shop.imageUrl && <img src={shop.imageUrl} alt={shop.name} className="h-20 w-20 rounded-xl object-cover" />}
          </div>
          <div className="mt-4 flex items-center gap-3">
            <span className="text-sm font-medium">Shop is {shop.isOpen ? "OPEN — receiving requests" : "CLOSED — not receiving requests"}</span>
            <Button onClick={toggleOpen} disabled={toggling} className={shop.isOpen ? "bg-amber-600 hover:bg-amber-700" : "bg-green-600 hover:bg-green-700"}>
              {toggling ? "Updating…" : shop.isOpen ? "Set CLOSED" : "Set OPEN"}
            </Button>
          </div>
          {msg && <p className="mt-2 text-sm text-amber-700">{msg}</p>}
        </Card>
        <Card className="bg-gray-50">
          <p className="text-sm font-semibold">How it works</p>
          <p className="text-sm text-gray-600">When a customer creates a LIVE request matching your category within ~10 km, you&apos;ll see it in <Link to="/shop/requests" className="underline">Live requests</Link> instantly. Tap <strong>AVAILABLE</strong> and the customer is notified immediately.</p>
        </Card>
      </div>
    </PageShell>
  );
}
