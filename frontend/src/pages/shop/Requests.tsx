import { useEffect, useState, useCallback } from "react";
import { apiFetch } from "../../api/client";
import type { RequestDto, ShopDto } from "../../types";
import { PageShell, Card, Badge, Spinner } from "../../components/Ui";
import { Button } from "../../components/Button";
import { formatDistance } from "../../lib/geo";
import { connectSignalR, joinShopGroup } from "../../lib/signalr";

export default function ShopRequests() {
  const [items, setItems] = useState<RequestDto[] | null>(null);
  const [shop, setShop] = useState<ShopDto | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [acting, setActing] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  const fetchAll = useCallback(async () => {
    try {
      const [reqs, s] = await Promise.all([
        apiFetch<RequestDto[]>("/api/requests/shop/live"),
        apiFetch<ShopDto>("/api/shops/me").catch(() => null),
      ]);
      setItems(reqs);
      if (s) setShop(s as ShopDto);
    } catch (ex: unknown) {
      console.error("[ShopRequests error]", ex);
      const e = ex as { detail?: string; title?: string; status?: number };
      if (e.status === 403) {
        setErr("Access forbidden (403): Your account role cannot view shop requests. Please log in with a Shop Owner account.");
      } else if (e.status === 401) {
        setErr("Session expired (401). Please log in again.");
      } else {
        setErr(e.detail ?? e.title ?? "Failed to load requests.");
      }
    }
  }, []);

  useEffect(() => { fetchAll(); }, [fetchAll]);

  useEffect(() => {
    if (!shop) return;
    let mounted = true;
    (async () => {
      try {
        const conn = await connectSignalR();
        await joinShopGroup(shop.id);
        const onNew = (payload: unknown) => {
          if (!mounted) return;
          const p = payload as { requestId: string; title: string; categoryName: string; distanceM: number };
          // refresh list or prepend
          fetchAll();
          setToast(`New request: ${p.title} — ${formatDistance(p.distanceM)} away`);
          setTimeout(() => setToast(null), 4000);
        };
        const onClosed = () => { fetchAll(); };
        conn.on("NewRequest", onNew);
        conn.on("RequestClosed", onClosed);
        return () => {
          conn.off("NewRequest", onNew);
          conn.off("RequestClosed", onClosed);
        };
      } catch { /* ignore */ }
    })();
    return () => { mounted = false; };
  }, [shop, fetchAll]);

  const available = async (requestId: string) => {
    setActing(requestId);
    try {
      await apiFetch(`/api/requests/${requestId}/available`, { method: "POST", body: JSON.stringify({ message: null }) });
      setToast("Marked as AVAILABLE — customer notified.");
      setTimeout(() => setToast(null), 3000);
      await fetchAll();
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Failed to mark available.");
    } finally { setActing(null); }
  };

  if (err) return <PageShell><Card><p className="text-sm text-red-600">{err}</p></Card></PageShell>;
  if (items === null) return <PageShell><div className="flex justify-center py-16"><Spinner /></div></PageShell>;

  return (
    <PageShell>
      <div className="mx-auto max-w-2xl space-y-4">
        <div className="flex items-center justify-between">
          <h1 className="text-xl font-bold flex items-center gap-2"><span className="h-2 w-2 rounded-full bg-red-600 animate-pulse" /> LIVE requests</h1>
          <span className="text-xs text-gray-500">{shop ? `${shop.name} · ${shop.isOpen ? "OPEN" : "CLOSED"}` : ""}</span>
        </div>
        {shop && !shop.isOpen && <Card className="border-amber-300 bg-amber-50"><p className="text-sm text-amber-800">Your shop is CLOSED — you won&apos;t receive new requests. Set it to OPEN in the dashboard.</p></Card>}
        {shop && shop.status !== "Verified" && <Card className="border-amber-300 bg-amber-50"><p className="text-sm text-amber-800">Your shop is pending verification — only verified OPEN shops receive requests.</p></Card>}
        {toast && <div className="rounded-xl bg-gray-900 px-4 py-3 text-sm font-medium text-white">{toast}</div>}
        <p className="text-sm text-gray-600">Requests matching your category within ~10 km appear here in real time. They disappear automatically when fulfilled, cancelled, or expired.</p>
        {items.length === 0 ? (
          <Card><p className="text-sm text-gray-600">No live requests for your shop right now. Keep your shop OPEN.</p></Card>
        ) : (
          <ul className="space-y-3">
            {items.map((r) => (
              <li key={r.id}>
                <Card>
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <p className="text-sm font-bold">{r.title}</p>
                      {r.description && <p className="text-xs text-gray-600">{r.description}</p>}
                      <p className="mt-1 text-xs text-gray-500">{r.categoryName} · {formatDistance(r.distanceM ?? null)} away · {new Date(r.createdAt).toLocaleTimeString()}</p>
                    </div>
                    <Badge tone="red">LIVE</Badge>
                  </div>
                  <Button onClick={() => available(r.id)} disabled={acting === r.id} className="mt-3 w-full bg-green-600 hover:bg-green-700 py-3 text-base">
                    {acting === r.id ? "Sending…" : "AVAILABLE"}
                  </Button>
                </Card>
              </li>
            ))}
          </ul>
        )}
      </div>
    </PageShell>
  );
}
