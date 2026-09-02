import { useEffect, useState, useRef } from "react";
import { apiFetch } from "../../api/client";
import type { CategoryDto, RequestDto } from "../../types";
import { PageShell, Spinner } from "../../components/Ui";
import { getCurrentPosition } from "../../lib/geo";
import { connectSignalR } from "../../lib/signalr";
import {
  getSuggestionsForRequest,
  addCommunitySuggestion,
  upvoteSuggestion,
  type CommunitySuggestion,
} from "../../lib/communitySuggestions";

interface QuickPreset {
  icon: string;
  text: string;
  categorySlug?: string;
}

const PRESETS: QuickPreset[] = [
  { icon: "☕", text: "Hot Cappuccino & Muffin", categorySlug: "food-snacks" },
  { icon: "📱", text: "Type-C 65W Fast Charger", categorySlug: "electronics" },
  { icon: "💊", text: "Paracetamol 500mg", categorySlug: "pharmacy" },
  { icon: "🎂", text: "Chocolate Birthday Cake", categorySlug: "food-snacks" },
  { icon: "👕", text: "Plain Black T-Shirt Size L", categorySlug: "clothing" },
  { icon: "🚗", text: "Car Battery Jump Start Cable", categorySlug: "home-tools" },
];

export default function CustomerHome() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [inputText, setInputText] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string>("");
  const [lat, setLat] = useState<number | null>(null);
  const [lng, setLng] = useState<number | null>(null);
  const [publishing, setPublishing] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const [myRequests, setMyRequests] = useState<RequestDto[]>([]);
  const [loadingPool, setLoadingPool] = useState(true);
  const [communitySuggestions, setCommunitySuggestions] = useState<Record<string, CommunitySuggestion[]>>({});

  // Inline suggestion input state
  const [suggestingReqId, setSuggestingReqId] = useState<string | null>(null);
  const [inlinePlaceName, setInlinePlaceName] = useState("");
  const [inlineNote, setInlineNote] = useState("");

  const chatEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    apiFetch<CategoryDto[]>("/api/categories")
      .then((cats) => {
        setCategories(cats);
        if (cats.length > 0) setSelectedCategory(cats[0].id);
      })
      .catch(() => {});

    detectLocation();
    fetchLivePool();
  }, []);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [myRequests, communitySuggestions]);

  // Connect SignalR WebSockets for real-time live chat responses
  useEffect(() => {
    let hubConn: any = null;
    connectSignalR()
      .then((hub) => {
        hubConn = hub;
        hub.on("shopAvailable", (data: any) => {
          setMyRequests((prev) =>
            prev.map((req) => {
              if (req.id === data.requestId) {
                const existing = req.availableShops || [];
                const alreadyExists = existing.some((s) => s.shopId === data.shopId);
                if (alreadyExists) return req;
                return {
                  ...req,
                  availableShops: [
                    ...existing,
                    {
                      shopId: data.shopId,
                      shopName: data.shopName,
                      address: data.address || "",
                      phone: data.phone || "",
                      distanceM: data.distanceM,
                      isVerified: data.verified ?? true,
                      respondedAt: new Date().toISOString(),
                    },
                  ],
                };
              }
              return req;
            })
          );
        });

        hub.on("requestStatusChanged", (data: any) => {
          setMyRequests((prev) =>
            prev.map((req) =>
              req.id === data.requestId ? { ...req, status: data.status } : req
            )
          );
        });
      })
      .catch(() => {});

    return () => {
      if (hubConn) {
        hubConn.off("shopAvailable");
        hubConn.off("requestStatusChanged");
      }
    };
  }, []);

  const detectLocation = async () => {
    try {
      const p = await getCurrentPosition();
      setLat(p.latitude);
      setLng(p.longitude);
    } catch {
      setLat(11.0294);
      setLng(76.9675);
    }
  };

  const fetchLivePool = async () => {
    setLoadingPool(true);
    try {
      const data = await apiFetch<RequestDto[]>("/api/requests/my/live");
      const list = data || [];
      setMyRequests(list);

      const sugsMap: Record<string, CommunitySuggestion[]> = {};
      list.forEach((req) => {
        sugsMap[req.id] = getSuggestionsForRequest(req.id);
      });
      setCommunitySuggestions(sugsMap);
    } catch {
      // Ignore guest fetch error
    } finally {
      setLoadingPool(false);
    }
  };

  const handleSendRequest = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!inputText.trim()) {
      setErr("Please type what you need before sending.");
      inputRef.current?.focus();
      return;
    }

    const currentLat = lat ?? 11.0294;
    const currentLng = lng ?? 76.9675;
    const catId = selectedCategory || categories[0]?.id;

    if (!catId) {
      setErr("Categories loading... Please try again in a moment.");
      return;
    }

    setErr(null);
    setPublishing(true);

    try {
      await apiFetch<{ id: string }>("/api/requests", {
        method: "POST",
        body: JSON.stringify({
          title: inputText.trim(),
          categoryId: catId,
          latitude: currentLat,
          longitude: currentLng,
          ttlMinutes: 30,
        }),
      });

      setInputText("");
      await fetchLivePool();
    } catch (ex: any) {
      setErr(ex.detail || ex.title || "Failed to send request.");
    } finally {
      setPublishing(false);
    }
  };

  const handlePresetClick = (preset: QuickPreset) => {
    setInputText(preset.text);
    if (preset.categorySlug) {
      const found = categories.find((c) => c.slug === preset.categorySlug);
      if (found) setSelectedCategory(found.id);
    }
    inputRef.current?.focus();
  };

  const handleAddInlineSuggestion = (reqId: string, e: React.FormEvent) => {
    e.preventDefault();
    if (!inlinePlaceName.trim()) return;

    const added = addCommunitySuggestion(reqId, inlinePlaceName.trim(), inlineNote.trim());
    setCommunitySuggestions((prev) => ({
      ...prev,
      [reqId]: [added, ...(prev[reqId] || [])],
    }));

    setInlinePlaceName("");
    setInlineNote("");
    setSuggestingReqId(null);
  };

  const handleUpvote = (sugId: string, reqId: string) => {
    upvoteSuggestion(sugId);
    setCommunitySuggestions((prev) => ({
      ...prev,
      [reqId]: (prev[reqId] || []).map((s) =>
        s.id === sugId ? { ...s, upvotes: s.upvotes + 1 } : s
      ),
    }));
  };

  const formatCountdown = (expiresAt: string) => {
    const diff = new Date(expiresAt).getTime() - Date.now();
    if (diff <= 0) return "Expired";
    const mins = Math.floor(diff / 60000);
    const secs = Math.floor((diff % 60000) / 1000);
    return `${mins}m ${secs}s`;
  };

  return (
    <PageShell>
      <div className="mx-auto max-w-4xl space-y-4 py-2">
        
        {/* MINIMALIST LIVE STREAM CONTAINER */}
        <div className="mini-card flex flex-col h-[82vh] overflow-hidden relative p-4 space-y-3">
          
          {/* MINIMALIST HEADER */}
          <div className="flex items-center justify-between px-3 py-2 border-b border-white/5">
            <div className="flex items-center gap-3">
              <span className="flex h-2.5 w-2.5 rounded-full bg-rose-500 animate-pulse" />
              <div>
                <h1 className="text-base font-extrabold text-white tracking-tight flex items-center gap-2">
                  <span>LIVE POOL STREAM</span>
                  <span className="text-[10px] font-mono text-rose-400 bg-rose-950/60 border border-rose-500/30 px-2 py-0.5 rounded-full">
                    REALTIME ⚡
                  </span>
                </h1>
                <p className="text-[11px] text-slate-400 font-mono">
                  Single live stream for requests, shop responses, and Google Maps pins.
                </p>
              </div>
            </div>

            <button
              onClick={fetchLivePool}
              className="mini-button-secondary px-3 py-1 text-xs cursor-pointer"
            >
              🔄 Refresh
            </button>
          </div>

          {/* STREAM BODY (MESSAGES SCROLL AREA) */}
          <div className="mini-screen flex-1 overflow-y-auto p-4 sm:p-6 space-y-5">
            
            {loadingPool ? (
              <div className="flex flex-col items-center justify-center h-full text-slate-400 space-y-2">
                <Spinner />
                <p className="text-xs font-mono">Loading Stream...</p>
              </div>
            ) : myRequests.length === 0 ? (
              <div className="flex flex-col items-center justify-center h-full text-center p-8 space-y-2 text-slate-400">
                <div className="text-4xl">⚡</div>
                <p className="text-base font-bold text-white">Live Stream Active</p>
                <p className="text-xs max-w-xs text-slate-400">
                  Type your request in the bar below to send it live into the stream!
                </p>
              </div>
            ) : (
              myRequests.map((req) => {
                const availableShops = req.availableShops || [];
                const sugs = communitySuggestions[req.id] || [];
                const isFulfilled = req.status === "fulfilled";
                const isExpired = req.status === "expired";

                return (
                  <div key={req.id} className="space-y-3">
                    
                    {/* REQUEST CARD BUBBLE */}
                    <div className="flex items-start gap-3">
                      <div className="h-8 w-8 rounded-xl bg-rose-600 flex items-center justify-center text-white text-xs font-bold flex-shrink-0 shadow-md">
                        ⚡
                      </div>

                      <div className="flex-1 mini-bubble p-4 space-y-3">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-bold text-rose-400">YOU (CUSTOMER)</span>
                            <span className="text-[10px] font-medium text-slate-300 bg-slate-800 border border-slate-700 px-2 py-0.5 rounded-full">
                              {req.categoryName}
                            </span>
                          </div>
                          <span className="text-[11px] font-mono text-slate-400">
                            ⏱️ {formatCountdown(req.expiresAt)}
                          </span>
                        </div>

                        <p className="text-base font-bold text-white leading-relaxed">
                          {req.title}
                        </p>

                        <div className="flex items-center justify-between text-[11px] font-mono text-slate-400 pt-1 border-t border-white/5">
                          <span>Notified {req.notifiedShopsCount} open shops</span>
                          {isFulfilled ? (
                            <span className="text-emerald-400 font-bold">✓ Fulfilled</span>
                          ) : isExpired ? (
                            <span className="text-slate-500">Expired</span>
                          ) : (
                            <span className="text-rose-400 font-bold animate-pulse">● Active</span>
                          )}
                        </div>

                        {/* INLINE SHOP RESPONSES & COMMUNITY SUGGESTIONS */}
                        <div className="pt-2 space-y-2.5">
                          
                          {/* Official Shop Responses */}
                          {availableShops.map((shop, i) => (
                            <div
                              key={i}
                              className="rounded-xl bg-emerald-950/40 border border-emerald-500/40 p-3 space-y-2"
                            >
                              <div className="flex items-center justify-between text-xs">
                                <div className="font-bold text-emerald-200 flex items-center gap-2">
                                  <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
                                  <span>🏬 {shop.shopName}</span>
                                  <span className="rounded bg-emerald-500 text-black px-1.5 py-0.2 text-[9px] font-bold">
                                    AVAILABLE
                                  </span>
                                </div>
                                <span className="text-emerald-400 font-mono font-bold">
                                  {shop.distanceM ? `${Math.round(shop.distanceM)}m away` : "Nearby"}
                                </span>
                              </div>

                              <p className="text-xs text-emerald-300/80 font-mono">{shop.address} · 📞 {shop.phone}</p>

                              <a
                                href={shop.navigationUrl ?? `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(shop.address)}`}
                                target="_blank"
                                rel="noreferrer"
                                className="inline-flex items-center gap-1.5 rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-emerald-500 transition shadow"
                              >
                                <span>GO THERE — Navigate</span>
                                <span>➔</span>
                              </a>
                            </div>
                          ))}

                          {/* Community Suggestions */}
                          {sugs.map((sug) => (
                            <div
                              key={sug.id}
                              className="rounded-xl bg-amber-950/30 border border-amber-500/30 p-3 space-y-2 text-xs"
                            >
                              <div className="flex items-start justify-between gap-2">
                                <div>
                                  <div className="font-bold text-amber-200 flex items-center gap-1.5">
                                    <span>💡 {sug.userName}: Suggested &quot;{sug.placeName}&quot;</span>
                                  </div>
                                  {sug.note && <p className="text-[11px] text-amber-100/90 italic mt-0.5">“{sug.note}”</p>}
                                </div>

                                <button
                                  type="button"
                                  onClick={() => handleUpvote(sug.id, req.id)}
                                  className="mini-button-secondary px-2 py-0.5 text-[10px] cursor-pointer"
                                >
                                  👍 {sug.upvotes}
                                </button>
                              </div>

                              <a
                                href={sug.googleMapsUrl}
                                target="_blank"
                                rel="noreferrer"
                                className="inline-flex items-center gap-1.5 rounded-lg bg-slate-900 border border-amber-500/40 px-3 py-1.5 text-[11px] font-bold text-amber-300 hover:bg-amber-500 hover:text-black transition"
                              >
                                <span>🗺️ Open Google Maps Marker</span>
                                <span>➔</span>
                              </a>
                            </div>
                          ))}

                          {/* Action: Add Inline Suggestion */}
                          {suggestingReqId === req.id ? (
                            <form onSubmit={(e) => handleAddInlineSuggestion(req.id, e)} className="p-3 bg-slate-900/90 rounded-xl border border-amber-500/40 space-y-2">
                              <input
                                type="text"
                                value={inlinePlaceName}
                                onChange={(e) => setInlinePlaceName(e.target.value)}
                                placeholder="Place / Shop name..."
                                required
                                className="w-full mini-input px-3 py-1.5 text-xs font-medium"
                              />
                              <input
                                type="text"
                                value={inlineNote}
                                onChange={(e) => setInlineNote(e.target.value)}
                                placeholder="Note (optional)..."
                                className="w-full mini-input px-3 py-1.5 text-xs font-medium"
                              />
                              <div className="flex gap-2">
                                <button
                                  type="button"
                                  onClick={() => setSuggestingReqId(null)}
                                  className="w-full py-1 text-xs text-slate-400 hover:text-white"
                                >
                                  Cancel
                                </button>
                                <button
                                  type="submit"
                                  className="w-full py-1 rounded-lg bg-amber-500 text-black font-extrabold text-xs"
                                >
                                  Post Pin 🗺️
                                </button>
                              </div>
                            </form>
                          ) : (
                            <button
                              type="button"
                              onClick={() => setSuggestingReqId(req.id)}
                              className="text-[11px] font-bold text-amber-400 hover:underline flex items-center gap-1 cursor-pointer pt-1"
                            >
                              <span>+ Suggest a place pin for this request</span>
                            </button>
                          )}

                        </div>
                      </div>
                    </div>

                  </div>
                );
              })
            )}

            <div ref={chatEndRef} />
          </div>

          {/* MINIMALIST BOTTOM COMPOSER BAR */}
          <div className="p-2 space-y-2 border-t border-white/5">
            
            {/* Quick Presets */}
            <div className="flex items-center gap-2 overflow-x-auto pb-1">
              <span className="text-[10px] font-bold text-slate-400 uppercase tracking-wider flex-shrink-0">Presets:</span>
              {PRESETS.map((p, idx) => (
                <button
                  key={idx}
                  type="button"
                  onClick={() => handlePresetClick(p)}
                  className="mini-button-secondary px-2.5 py-1 text-[11px] flex-shrink-0 flex items-center gap-1 cursor-pointer"
                >
                  <span>{p.icon}</span>
                  <span>{p.text}</span>
                </button>
              ))}
            </div>

            {/* Main Input & Category Pills */}
            <form onSubmit={handleSendRequest} className="space-y-2">
              <div className="flex flex-wrap items-center gap-1.5">
                {categories.map((cat) => {
                  const isSelected = selectedCategory === cat.id;
                  return (
                    <button
                      key={cat.id}
                      type="button"
                      onClick={() => setSelectedCategory(cat.id)}
                      className={`mini-pill px-2.5 py-1 text-[11px] font-bold cursor-pointer ${
                        isSelected ? "active" : ""
                      }`}
                    >
                      {cat.icon ? `${cat.icon} ` : ""}{cat.name}
                    </button>
                  );
                })}
              </div>

              <div className="flex items-center gap-2">
                <input
                  ref={inputRef}
                  type="text"
                  value={inputText}
                  onChange={(e) => setInputText(e.target.value)}
                  placeholder="Type your live request... e.g. Black shirt size L"
                  className="w-full mini-input px-4 py-3 text-sm font-medium text-white placeholder-slate-400"
                  maxLength={200}
                  disabled={publishing}
                />

                <button
                  type="submit"
                  disabled={publishing}
                  className="mini-button-primary px-6 h-[46px] text-sm flex items-center gap-1.5 flex-shrink-0 cursor-pointer disabled:opacity-50"
                >
                  {publishing ? (
                    <Spinner />
                  ) : (
                    <>
                      <span>Send</span>
                      <span>⚡</span>
                    </>
                  )}
                </button>
              </div>

              {err && <div className="text-xs text-rose-400 font-bold px-1">{err}</div>}
            </form>

          </div>

        </div>

      </div>
    </PageShell>
  );
}
