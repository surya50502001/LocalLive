import { useEffect, useState, useCallback } from "react";
import { useParams, Link } from "react-router-dom";
import { apiFetch } from "../../api/client";
import type { RequestDto } from "../../types";
import { PageShell, Card, Badge, Spinner } from "../../components/Ui";
import { Button, SecondaryButton, DangerButton } from "../../components/Button";
import { formatDistance } from "../../lib/geo";
import { connectSignalR } from "../../lib/signalr";

export default function RequestLive() {
  const { id } = useParams<{ id: string }>();
  const [req, setReq] = useState<RequestDto | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [actionMsg, setActionMsg] = useState<string | null>(null);
  const [actioning, setActioning] = useState(false);

  const fetchReq = useCallback(async () => {
    if (!id) return;
    try {
      const data = await apiFetch<RequestDto>(`/api/requests/${id}`);
      setReq(data);
    } catch (ex: unknown) { setErr((ex as { detail?: string })?.detail ?? "Failed to load request."); }
  }, [id]);
  useEffect(() => { fetchReq(); }, [fetchReq]);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const conn = await connectSignalR();
        const onShopAvailable = (_payload: unknown) => { fetchReq(); };
        const onStatus = (payload: unknown) => {
          if (!mounted) return;
          const p = payload as { requestId: string; status: string };
          if (p.requestId === id) setReq((prev) => prev ? { ...prev, status: p.status } : prev);
        };
        conn.on("ShopAvailable", onShopAvailable);
        conn.on("RequestStatusChanged", onStatus);
        return () => { conn.off("ShopAvailable", onShopAvailable); conn.off("RequestStatusChanged", onStatus); };
      } catch { /* ignore */ }
    })();
    return () => { mounted = false; };
  }, [id]);

  const doAction = async (action: "cancel" | "fulfill") => {
    if (!id) return;
    setActioning(true); setActionMsg(null);
    try {
      const data = await apiFetch<RequestDto>(`/api/requests/${id}/${action}`, { method: "POST" });
      setReq(data);
    } catch (ex: unknown) { setActionMsg((ex as { detail?: string })?.detail ?? "Action failed."); }
    finally { setActioning(false); }
  };

  if (err) return <PageShell><Card><p className="text-sm text-red-600">{err}</p><Link to="/customer" className="mt-2 inline-block text-sm font-semibold underline">Back</Link></Card></PageShell>;
  if (!req) return <PageShell><div className="flex justify-center py-16"><Spinner /></div></PageShell>;

  const isActive = req.status === "Active";
  const expiresIn = Math.max(0, Math.floor((new Date(req.expiresAt).getTime() - Date.now()) / 60000));

  return (
    <PageShell>
      <div className="mx-auto max-w-2xl space-y-5">
        <Link to="/customer/requests" className="text-sm font-medium text-gray-600 hover:text-gray-900">← My requests</Link>

        {/* Request card */}
        <Card>
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="flex items-center gap-2">
                <span className="inline-flex items-center gap-1 rounded-full bg-red-50 px-2 py-0.5 text-[10px] font-black tracking-widest text-red-600">
                  <span className={`h-1.5 w-1.5 rounded-full ${isActive ? "bg-red-600 animate-pulse" : "bg-gray-400"}`} />
                  {isActive ? "LIVE" : req.status.toUpperCase()}
                </span>
                <span className="rounded-full bg-gray-100 px-2 py-0.5 text-[10px] font-semibold text-gray-600">{req.categoryName}</span>
              </div>
              <h1 className="mt-2 text-xl font-extrabold">{req.title}</h1>
              {req.description && <p className="mt-1 text-sm text-gray-600">{req.description}</p>}
              <p className="mt-2 text-xs text-gray-500">Created {new Date(req.createdAt).toLocaleString()} · Expires in {expiresIn} min</p>
            </div>
            <Badge tone={isActive ? "red" : req.status === "Fulfilled" ? "green" : "gray"}>{req.status}</Badge>
          </div>
          {isActive && (
            <div className="mt-4 flex gap-2">
              <SecondaryButton onClick={() => doAction("cancel")} disabled={actioning}>Cancel</SecondaryButton>
              <Button onClick={() => doAction("fulfill")} disabled={actioning}>Mark as done</Button>
            </div>
          )}
          {actionMsg && <p className="mt-2 text-sm text-amber-700">{actionMsg}</p>}
        </Card>

        {/* Available shops */}
        <Card>
          <h2 className="flex items-center gap-2 text-sm font-bold">
            <span className="h-2 w-2 rounded-full bg-green-600" /> Available now — tap to navigate
          </h2>
          {req.availableShops.length === 0 ? (
            <div className="mt-4 rounded-xl border border-dashed border-gray-300 p-6 text-center">
              <p className="text-sm font-medium text-gray-700">Waiting for a shop to confirm…</p>
              <p className="mt-1 text-xs text-gray-500">We notified {req.notifiedShopsCount} verified open shops nearby. Responses appear here instantly.</p>
              {isActive && <div className="mt-3 flex justify-center"><Spinner /></div>}
            </div>
          ) : (
            <ul className="mt-4 space-y-3">
              {req.availableShops.map((s) => (
                <li key={s.shopId}>
                  <Card className="border-green-200 bg-green-50/50">
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-sm font-bold">{s.shopName} <Badge tone="green">AVAILABLE NOW</Badge></p>
                        {s.address && <p className="text-xs text-gray-600">{s.address} · {s.phone}</p>}
                        {s.message && <p className="mt-1 text-xs text-gray-700 italic">“{s.message}”</p>}
                        <p className="mt-1 text-xs font-semibold text-gray-900">{formatDistance(s.distanceM ?? null)} away {s.isVerified ? "· Verified" : ""}</p>
                      </div>
                      <span className="text-xs text-gray-500">{new Date(s.respondedAt).toLocaleTimeString()}</span>
                    </div>
                    <a href={s.navigationUrl ?? `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(s.address)}`} target="_blank" rel="noreferrer" className="mt-3 inline-flex w-full items-center justify-center rounded-xl bg-green-600 px-4 py-3 text-sm font-bold text-white hover:bg-green-700">
                      GO THERE — Navigate
                    </a>
                  </Card>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card className="bg-gray-50">
          <p className="text-xs text-gray-500">This request disappears automatically when fulfilled, cancelled, or expired. Only verified, OPEN shops can respond.</p>
        </Card>
      </div>
    </PageShell>
  );
}
