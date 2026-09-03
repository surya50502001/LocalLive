import { useEffect, useState, useCallback } from "react";
import { apiFetch } from "../../api/client";
import type { RequestDto, ShopDto } from "../../types";
import { PageShell, Card, Badge, Spinner } from "../../components/Ui";
import { Button, SecondaryButton } from "../../components/Button";
import { formatDistance } from "../../lib/geo";
import { connectSignalR, joinShopGroup } from "../../lib/signalr";
import { useAuthStore } from "../../store/authStore";
import ChatDrawer from "../../components/ChatDrawer";

export default function ShopRequests() {
  const { user } = useAuthStore();
  const [items, setItems] = useState<RequestDto[] | null>(null);
  const [shop, setShop] = useState<ShopDto | null>(null);
  const [err, setErr] = useState<string | null>(null);
  const [acting, setActing] = useState<string | null>(null);
  const [toast, setToast] = useState<string | null>(null);

  // Note dialog state
  const [activeNoteReqId, setActiveNoteReqId] = useState<string | null>(null);
  const [noteText, setNoteText] = useState("");

  // Chat drawer state
  const [chatTarget, setChatTarget] = useState<{
    requestId: string;
    requestTitle: string;
    customerName: string;
  } | null>(null);

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
        setErr("Access forbidden (403): Please log in with a Shop Owner account.");
      } else if (e.status === 401) {
        setErr("Session expired (401). Please log in again.");
      } else {
        setErr(e.detail ?? e.title ?? "Failed to load requests.");
      }
    }
  }, []);

  useEffect(() => {
    fetchAll();
  }, [fetchAll]);

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
          fetchAll();
          setToast(`⚡ New request: ${p.title} — ${formatDistance(p.distanceM)} away`);
          setTimeout(() => setToast(null), 4500);
        };
        const onClosed = () => {
          fetchAll();
        };
        conn.on("NewRequest", onNew);
        conn.on("RequestClosed", onClosed);
        return () => {
          conn.off("NewRequest", onNew);
          conn.off("RequestClosed", onClosed);
        };
      } catch {
        /* ignore */
      }
    })();
    return () => {
      mounted = false;
    };
  }, [shop, fetchAll]);

  const markAvailable = async (requestId: string, message?: string) => {
    setActing(requestId);
    try {
      await apiFetch(`/api/requests/${requestId}/available`, {
        method: "POST",
        body: JSON.stringify({ message: message || "Item is available now!" }),
      });
      setToast("✓ Marked as AVAILABLE — customer notified immediately!");
      setActiveNoteReqId(null);
      setNoteText("");
      setTimeout(() => setToast(null), 4000);
      await fetchAll();
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Failed to mark available.");
    } finally {
      setActing(null);
    }
  };

  if (err)
    return (
      <PageShell>
        <Card>
          <p className="text-sm text-red-600">{err}</p>
        </Card>
      </PageShell>
    );

  if (items === null)
    return (
      <PageShell>
        <div className="flex justify-center py-16">
          <Spinner />
        </div>
      </PageShell>
    );

  return (
    <PageShell>
      <div className="mx-auto max-w-2xl space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-extrabold flex items-center gap-2">
              <span className="h-2.5 w-2.5 rounded-full bg-red-600 animate-pulse" />
              Incoming LIVE Requests
            </h1>
            <p className="text-xs text-gray-500">Real-time matching feed</p>
          </div>
          {shop && (
            <div className="flex items-center gap-2">
              <Badge tone={shop.isOnline ? "green" : "gray"}>
                {shop.isOnline ? "● ONLINE" : "○ OFFLINE"}
              </Badge>
              <Badge tone={shop.isOpen ? "green" : "gray"}>
                {shop.isOpen ? "STORE OPEN" : "STORE CLOSED"}
              </Badge>
            </div>
          )}
        </div>

        {shop && (!shop.isOpen || !shop.isOnline) && (
          <Card className="border-amber-300 bg-amber-50">
            <p className="text-xs text-amber-800">
              ⚠️ Your shop is currently {!shop.isOpen ? "CLOSED" : "OFFLINE"}. You will not receive customer requests
              until you set both Store OPEN and Live ONLINE in your dashboard.
            </p>
          </Card>
        )}

        {shop && shop.status !== "Verified" && (
          <Card className="border-amber-300 bg-amber-50">
            <p className="text-xs text-amber-800">
              Your shop is pending verification — only verified open shops receive live broadcasts.
            </p>
          </Card>
        )}

        {toast && (
          <div className="rounded-xl bg-slate-900 border border-emerald-500/50 px-4 py-3 text-xs font-bold text-emerald-400 shadow-xl animate-fade-in">
            {toast}
          </div>
        )}

        {items.length === 0 ? (
          <Card className="text-center py-12">
            <div className="text-3xl mb-2">📡</div>
            <p className="text-sm font-bold text-gray-800">No active customer requests right now</p>
            <p className="mt-1 text-xs text-gray-500 max-w-sm mx-auto">
              When a customer within ~10 km searches for items in your categories, their request appears here with an
              audio alert and instant 1-tap availability button.
            </p>
          </Card>
        ) : (
          <ul className="space-y-3">
            {items.map((r) => {
              const hasResponded = r.availableShops?.some((s) => s.shopId === shop?.id);

              return (
                <li key={r.id}>
                  <Card className="space-y-3 border-gray-200">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="rounded-full bg-rose-50 px-2 py-0.5 text-[10px] font-black tracking-wider text-rose-600">
                            LIVE REQUEST
                          </span>
                          <span className="text-[10px] text-gray-500 bg-gray-100 px-2 py-0.5 rounded-full">
                            {r.categoryName}
                          </span>
                        </div>
                        <h3 className="mt-1 text-base font-bold text-gray-900">{r.title}</h3>
                        {r.description && <p className="text-xs text-gray-600 mt-0.5">{r.description}</p>}
                        <p className="mt-2 text-xs font-semibold text-gray-700">
                          📍 {formatDistance(r.distanceM ?? null)} away · {new Date(r.createdAt).toLocaleTimeString()}
                        </p>
                      </div>
                      <Badge tone="red">LIVE</Badge>
                    </div>

                    {/* Action buttons */}
                    {hasResponded ? (
                      <div className="flex gap-2">
                        <div className="flex-1 rounded-xl bg-emerald-50 border border-emerald-200 p-2 text-center text-xs font-bold text-emerald-700">
                          ✓ You confirmed availability
                        </div>
                        <Button
                          onClick={() =>
                            setChatTarget({
                              requestId: r.id,
                              requestTitle: r.title,
                              customerName: "Customer",
                            })
                          }
                          className="bg-indigo-600 hover:bg-indigo-700 text-xs py-2"
                        >
                          💬 Chat
                        </Button>
                      </div>
                    ) : (
                      <div className="space-y-2 pt-2 border-t border-gray-100">
                        {activeNoteReqId === r.id ? (
                          <div className="space-y-2">
                            <input
                              type="text"
                              value={noteText}
                              onChange={(e) => setNoteText(e.target.value)}
                              placeholder="Add optional note (e.g. In stock, $24, ready for pickup)…"
                              className="w-full rounded-xl border border-gray-300 p-2.5 text-xs text-gray-900 focus:outline-none focus:border-green-600"
                            />
                            <div className="flex gap-2">
                              <SecondaryButton
                                onClick={() => setActiveNoteReqId(null)}
                                className="text-xs py-2 flex-1"
                              >
                                Cancel
                              </SecondaryButton>
                              <Button
                                onClick={() => markAvailable(r.id, noteText)}
                                disabled={acting === r.id}
                                className="bg-green-600 hover:bg-green-700 text-xs py-2 flex-1 font-bold"
                              >
                                {acting === r.id ? "Confirming…" : "Confirm Available"}
                              </Button>
                            </div>
                          </div>
                        ) : (
                          <div className="flex gap-2">
                            <Button
                              onClick={() => markAvailable(r.id)}
                              disabled={acting === r.id}
                              className="flex-1 bg-green-600 hover:bg-green-700 py-3 text-sm font-bold shadow-sm"
                            >
                              {acting === r.id ? "Sending…" : "✓ AVAILABLE NOW"}
                            </Button>
                            <SecondaryButton
                              onClick={() => {
                                setActiveNoteReqId(r.id);
                                setNoteText("");
                              }}
                              className="text-xs px-3"
                              title="Add custom note"
                            >
                              + Note
                            </SecondaryButton>
                          </div>
                        )}
                      </div>
                    )}
                  </Card>
                </li>
              );
            })}
          </ul>
        )}
      </div>

      {/* Real-time Contextual Chat Drawer */}
      {chatTarget && shop && user && (
        <ChatDrawer
          isOpen={!!chatTarget}
          onClose={() => setChatTarget(null)}
          requestId={chatTarget.requestId}
          shopId={shop.id}
          title={chatTarget.requestTitle}
          otherPartyName={chatTarget.customerName}
          currentUserId={user.id}
        />
      )}
    </PageShell>
  );
}
