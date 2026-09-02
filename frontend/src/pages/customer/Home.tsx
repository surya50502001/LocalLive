import { useEffect, useState, useCallback, useRef } from "react";
import { Link } from "react-router-dom";
import { apiFetch } from "../../api/client";
import type { CategoryDto, RequestDto } from "../../types";
import { PageShell, Spinner, EmptyState } from "../../components/Ui";
import { Button } from "../../components/Button";
import { formatDistance, getCurrentPosition } from "../../lib/geo";
import { connectSignalR } from "../../lib/signalr";

const ACTIVE = "Active";

export default function CustomerHome() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [requests, setRequests] = useState<RequestDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  const [categoryId, setCategoryId] = useState("");
  const [message, setMessage] = useState("");
  const [lat, setLat] = useState<number | null>(null);
  const [lng, setLng] = useState<number | null>(null);
  const [locating, setLocating] = useState(false);
  const [creating, setCreating] = useState(false);
  const [liveCount, setLiveCount] = useState(0);

  const scrollRef = useRef<HTMLDivElement>(null);

  const load = useCallback(async () => {
    try {
      const [cats, reqs] = await Promise.all([
        apiFetch<CategoryDto[]>("/api/categories"),
        apiFetch<RequestDto[]>("/api/requests/my/live"),
      ]);
      setCategories(cats);
      if (!categoryId && cats[0]) setCategoryId(cats[0].id);
      setRequests(reqs);
      setLiveCount(reqs.filter((r) => r.status === ACTIVE).length);
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Failed to load feed.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { load(); }, [load]);

  // real-time updates reach this customer via SignalR
  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const conn = await connectSignalR();
        const onStatus = (payload: unknown) => {
          if (!mounted) return;
          const p = payload as { requestId: string; status?: string };
          const rid = p.requestId ?? (payload as { id?: string }).id;
          if (!rid || !p.status) return;
          setRequests((prev) =>
            prev.map((r) => {
              if (r.id !== rid) return r;
              const wasActive = r.status === ACTIVE;
              const nowActive = p.status === ACTIVE;
              if (wasActive && !nowActive) setLiveCount((c) => Math.max(0, c - 1));
              return { ...r, status: p.status! };
            })
          );
        };
        const onClosed = (payload: unknown) => {
          const p = payload as { requestId?: string };
          if (p?.requestId) setRequests((prev) => prev.filter((r) => r.id !== p.requestId));
        };
        const onShop = (payload: unknown) => {
          if (!mounted) return;
          const p = payload as { requestId?: string; shopId?: string };
          if (!p?.requestId) return;
          setRequests((prev) =>
            prev.map((r) =>
              r.id === p.requestId && p.shopId && !r.availableShops.some((s) => s.shopId === p.shopId)
                ? { ...r, availableShops: [...r.availableShops, { shopId: p.shopId, shopName: "", address: "", phone: "", isVerified: true, respondedAt: new Date().toISOString(), distanceM: null }] }
                : r
            )
          );
        };
        conn.on("RequestStatusChanged", onStatus);
        conn.on("RequestClosed", onClosed);
        conn.on("ShopAvailable", onShop);
        return () => {
          conn.off("RequestStatusChanged", onStatus);
          conn.off("RequestClosed", onClosed);
          conn.off("ShopAvailable", onShop);
        };
      } catch { /* ignore */ }
    })();
    return () => { mounted = false; };
  }, []);

  const locate = async () => {
    setLocating(true); setErr(null);
    try { const p = await getCurrentPosition(); setLat(p.latitude); setLng(p.longitude); }
    catch (e: unknown) { setErr((e as Error).message); }
    finally { setLocating(false); }
  };

  const post = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!message.trim() || !categoryId) { setErr("Pick a category and type your need."); return; }
    if (lat === null || lng === null) { setErr("Set your location so nearby shops find you."); return; }
    setErr(null); setCreating(true);
    try {
      await apiFetch<{ id: string }>("/api/requests", {
        method: "POST",
        body: JSON.stringify({ title: message.trim(), categoryId, latitude: lat, longitude: lng, ttlMinutes: 30 }),
      });
      setMessage("");
      await load();
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Failed to post request.");
    } finally { setCreating(false); }
  };

  const activeOnly = requests.filter((r) => r.status === ACTIVE);
  const isLive = liveCount > 0;

  return (
    <div className="flex h-[calc(100vh-4rem)] flex-col bg-gray-50">
      {/* channel header */}
      <header className="border-b border-gray-200 bg-white px-4 py-3 sm:px-6">
        <div className="mx-auto flex max-w-3xl items-center justify-between">
          <div>
            <div className="flex items-center gap-2">
              <span className="rounded-full bg-black px-2 py-0.5 text-[10px] font-black tracking-widest text-white">NEARBY</span>
              <h1 className="text-base font-extrabold tracking-tight"># live-requests</h1>
            </div>
            <p className="mt-0.5 text-xs text-gray-500">
              {isLive
                ? <span className="inline-flex items-center gap-1.5"><span className="h-2 w-2 rounded-full bg-red-600 animate-pulse" /> {liveCount} active request{liveCount !== 1 ? "s" : ""} — shops are responding live</span>
                : "Waiting for nearby requests…"}
            </p>
          </div>
          <Link to="/customer/requests" className="text-xs font-semibold text-gray-600 underline hover:text-gray-900">My requests</Link>
        </div>
      </header>

      {/* scrollable chat thread */}
      <div ref={scrollRef} className="flex-1 overflow-y-auto px-4 py-4 sm:px-6">
        <div className="mx-auto max-w-3xl space-y-4">
          {loading ? (
            <div className="flex justify-center py-16"><Spinner /></div>
          ) : activeOnly.length === 0 ? (
            <div className="rounded-2xl border border-dashed border-gray-300 bg-white">
              <EmptyState icon="📡" title="No live requests right now" description="Post a need below and nearby open shops will respond instantly." />
            </div>
          ) : (
            activeOnly.map((r) => (
                <article key={r.id} className="max-w-[92%] rounded-2xl border border-gray-300 bg-white p-4 shadow-sm">
                  <div className="flex items-center gap-2 text-xs text-gray-500">
                    <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-0.5 text-[10px] font-black tracking-widest text-red-600">
                      <span className="h-1.5 w-1.5 rounded-full bg-red-600 animate-pulse" /> LIVE
                    </span>
                    <span className="rounded-full bg-gray-100 px-2 py-0.5 font-semibold">{r.categoryName}</span>
                    {r.distanceM !== null && r.distanceM !== undefined && (
                      <span className="rounded-full bg-gray-100 px-2 py-0.5 font-semibold">{formatDistance(r.distanceM)}</span>
                    )}
                    <span className="ml-auto">{new Date(r.createdAt).toLocaleTimeString()}</span>
                  </div>
                  <p className="mt-2 text-base font-bold text-gray-900">{r.title}</p>
                  {r.description && <p className="mt-0.5 text-sm text-gray-600">{r.description}</p>}
                  <div className="mt-2.5 flex flex-wrap items-center gap-3 text-xs text-gray-500">
                    <span>📍 {formatDistance(r.distanceM ?? null)}</span>
                    <span>🏪 {r.notifiedShopsCount} notified</span>
                    <span className={r.availableShops.length > 0 ? "font-bold text-green-600" : ""}>✅ {r.availableShops.length} available</span>
                    <Link to={`/customer/requests/${r.id}`} className="ml-auto font-semibold text-gray-700 underline">Open →</Link>
                  </div>
                </article>
              ))
          )}
        </div>
      </div>

      {/* composer */}
      <footer className="border-t border-gray-200 bg-white px-4 py-3 sm:px-6">
        <form onSubmit={post} className="mx-auto max-w-3xl space-y-2">
          {err && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{err}</div>}
          <div className="flex items-center gap-2">
            <select value={categoryId} onChange={(e) => setCategoryId(e.target.value)} className="h-11 rounded-xl border border-gray-300 bg-white px-2 text-sm outline-none focus:border-gray-900 focus:ring-1 focus:ring-gray-900">
              {categories.map((c) => <option key={c.id} value={c.id}>{c.icon ? `${c.icon} ` : ""}{c.name}</option>)}
            </select>
            <button type="button" onClick={locate} disabled={locating} title="Use my location"
              className={`flex h-11 items-center gap-1 rounded-xl border px-3 text-sm font-semibold ${lat !== null ? "border-green-500 bg-green-50 text-green-700" : "border-gray-300 bg-white hover:bg-gray-50"} disabled:opacity-50`}>
              {locating ? "…" : lat !== null ? "📍 Set" : "📍"}
            </button>
          </div>
          <div className="flex items-center gap-2">
            <input
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder="I need… (e.g. Black shirt size L, birthday cake)"
              maxLength={200}
              className="h-12 flex-1 rounded-xl border border-gray-300 bg-white px-4 text-sm outline-none focus:border-gray-900 focus:ring-1 focus:ring-gray-900"
            />
            <Button type="submit" disabled={creating} className="h-12 px-6">{creating ? "…" : "Ask"}</Button>
          </div>
          <p className="text-[11px] text-gray-400">Posts to verified, OPEN shops nearby and streams to the live feed in real time.</p>
        </form>
      </footer>
    </div>
  );
}
