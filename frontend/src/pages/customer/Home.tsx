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
      <div className="mx-auto max-w-3xl flex flex-col h-[85vh] py-2">
        
        {/* SEAMLESS BORDERLESS HEADER */}
        <div className="flex items-center justify-between py-3 px-1 border-b border-white/10 mb-2">
          <div className="flex items-center gap-3">
            <span className="flex h-2.5 w-2.5 rounded-full bg-rose-500 animate-pulse" />
            <div>
              <h1 className="text-base font-bold text-white tracking-tight flex items-center gap-2">
                <span>Live Pool Stream</span>
                <span className="text-[10px] font-mono text-rose-400 bg-rose-950/40 px-2 py-0.5 rounded-full border border-rose-500/20">
                  REALTIME
                </span>
              </h1>
              <p className="text-[11px] text-slate-400">
                Continuous minimal stream • Requests & responses render inline.
              </p>
            </div>
          </div>

          <button
            onClick={fetchLivePool}
            className="text-xs font-semibold text-slate-400 hover:text-white transition px-2 py-1"
          >
            🔄 Refresh
          </button>
        </div>

        {/* SEAMLESS STREAM STREAM AREA (NO BULKY CARDS) */}
        <div className="flex-1 overflow-y-auto px-1 py-3 space-y-6">
          
          {loadingPool ? (
            <div className="flex flex-col items-center justify-center h-full text-slate-400 space-y-2">
              <Spinner />
              <p className="text-xs font-mono">Loading stream...</p>
            </div>
          ) : myRequests.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-center p-8 space-y-2 text-slate-400">
              <div className="text-3xl">💬</div>
              <p className="text-sm font-semibold text-slate-200">No active requests in stream</p>
              <p className="text-xs max-w-xs text-slate-400">
                Type your request in the composer below to publish into the stream.
              </p>
            </div>
          ) : (
            myRequests.map((req) => {
              const availableShops = req.availableShops || [];
              const sugs = communitySuggestions[req.id] || [];
              const isFulfilled = req.status === "fulfilled";
              const isExpired = req.status === "expired";

              return (
                <div key={req.id} className="space-y-3 pb-4 border-b border-white/5">
                  
                  {/* CLEAN MINIMALIST USER REQUEST */}
                  <div className="space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-rose-400">Customer Request</span>
                        <span className="text-[10px] text-slate-400 bg-slate-800/60 px-2 py-0.5 rounded-md">
                          {req.categoryName}
                        </span>
                      </div>
                      <span className="text-[11px] font-mono text-slate-400">
                        ⏱️ {formatCountdown(req.expiresAt)}
                      </span>
                    </div>

                    <p className="text-base font-semibold text-white leading-relaxed">
                      {req.title}
                    </p>

                    <div className="flex items-center justify-between text-[11px] font-mono text-slate-400">
                      <span>Notified {req.notifiedShopsCount} open shops</span>
                      {isFulfilled ? (
                        <span className="text-emerald-400 font-bold">✓ Fulfilled</span>
                      ) : isExpired ? (
                        <span className="text-slate-500">Expired</span>
                      ) : (
                        <span className="text-rose-400 font-bold animate-pulse">● Active</span>
                      )}
                    </div>
                  </div>

                  {/* INLINE SHOP RESPONSES & PLACE SUGGESTIONS */}
                  {(availableShops.length > 0 || sugs.length > 0 || suggestingReqId === req.id) && (
                    <div className="pl-3 border-l-2 border-slate-700/60 space-y-2 pt-1">
                      
                      {/* Shop Responses */}
                      {availableShops.map((shop, i) => (
                        <div key={i} className="py-1.5 space-y-1 text-xs">
                          <div className="flex items-center justify-between">
                            <span className="font-bold text-emerald-300 flex items-center gap-1.5">
                              <span className="h-1.5 w-1.5 rounded-full bg-emerald-400" />
                              🏬 {shop.shopName}
                              <span className="text-[10px] text-emerald-400 bg-emerald-950/60 px-1.5 py-0.2 rounded font-bold">
                                AVAILABLE
                              </span>
                            </span>
                            <span className="font-mono text-emerald-400 text-[11px]">
                              {shop.distanceM ? `${Math.round(shop.distanceM)}m away` : "Nearby"}
                            </span>
                          </div>
                          <p className="text-slate-400 text-[11px] font-mono">{shop.address} · 📞 {shop.phone}</p>
                          <a
                            href={shop.navigationUrl ?? `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(shop.address)}`}
                            target="_blank"
                            rel="noreferrer"
                            className="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-400 hover:underline pt-0.5"
                          >
                            <span>Navigate on Google Maps</span>
                            <span>➔</span>
                          </a>
                        </div>
                      ))}

                      {/* Community Place Pin Suggestions */}
                      {sugs.map((sug) => (
                        <div key={sug.id} className="py-1.5 space-y-1 text-xs">
                          <div className="flex items-center justify-between">
                            <span className="font-semibold text-amber-300">
                              💡 {sug.userName}: Suggested &quot;{sug.placeName}&quot;
                            </span>
                            <button
                              type="button"
                              onClick={() => handleUpvote(sug.id, req.id)}
                              className="text-[10px] text-slate-400 hover:text-white transition"
                            >
                              👍 {sug.upvotes}
                            </button>
                          </div>
                          {sug.note && <p className="text-[11px] text-slate-400 italic">“{sug.note}”</p>}
                          <a
                            href={sug.googleMapsUrl}
                            target="_blank"
                            rel="noreferrer"
                            className="inline-flex items-center gap-1 text-[11px] font-bold text-amber-400 hover:underline pt-0.5"
                          >
                            <span>Open Google Maps Pin</span>
                            <span>➔</span>
                          </a>
                        </div>
                      ))}

                      {/* Suggest Pin Form */}
                      {suggestingReqId === req.id ? (
                        <form onSubmit={(e) => handleAddInlineSuggestion(req.id, e)} className="pt-2 space-y-2">
                          <input
                            type="text"
                            value={inlinePlaceName}
                            onChange={(e) => setInlinePlaceName(e.target.value)}
                            placeholder="Place or shop name..."
                            required
                            className="w-full bg-slate-900 border border-slate-700 rounded-lg px-3 py-1.5 text-xs text-white"
                          />
                          <input
                            type="text"
                            value={inlineNote}
                            onChange={(e) => setInlineNote(e.target.value)}
                            placeholder="Note (optional)..."
                            className="w-full bg-slate-900 border border-slate-700 rounded-lg px-3 py-1.5 text-xs text-white"
                          />
                          <div className="flex gap-2">
                            <button
                              type="button"
                              onClick={() => setSuggestingReqId(null)}
                              className="text-xs text-slate-400 hover:text-white"
                            >
                              Cancel
                            </button>
                            <button
                              type="submit"
                              className="text-xs font-bold text-amber-400 hover:underline"
                            >
                              Post Pin 🗺️
                            </button>
                          </div>
                        </form>
                      ) : (
                        <button
                          type="button"
                          onClick={() => setSuggestingReqId(req.id)}
                          className="text-[11px] font-medium text-slate-400 hover:text-amber-300 transition pt-1 cursor-pointer"
                        >
                          + Suggest a place pin
                        </button>
                      )}

                    </div>
                  )}

                </div>
              );
            })
          )}

          <div ref={chatEndRef} />
        </div>

        {/* MINIMALIST BOTTOM COMPOSER BAR (NO BULKY CARD) */}
        <div className="pt-3 border-t border-white/10 space-y-2.5">
          
          {/* Preset Buttons */}
          <div className="flex items-center gap-1.5 overflow-x-auto pb-1">
            <span className="text-[10px] font-bold text-slate-500 uppercase tracking-wider flex-shrink-0">Presets:</span>
            {PRESETS.map((p, idx) => (
              <button
                key={idx}
                type="button"
                onClick={() => handlePresetClick(p)}
                className="text-[11px] font-medium text-slate-300 bg-slate-800/80 hover:bg-slate-700 rounded-lg px-2.5 py-1 flex-shrink-0 flex items-center gap-1 transition cursor-pointer"
              >
                <span>{p.icon}</span>
                <span>{p.text}</span>
              </button>
            ))}
          </div>

          {/* Category Chips & Input Line */}
          <form onSubmit={handleSendRequest} className="space-y-2">
            <div className="flex flex-wrap items-center gap-1.5">
              {categories.map((cat) => {
                const isSelected = selectedCategory === cat.id;
                return (
                  <button
                    key={cat.id}
                    type="button"
                    onClick={() => setSelectedCategory(cat.id)}
                    className={`px-2.5 py-1 text-[11px] font-semibold rounded-lg transition cursor-pointer ${
                      isSelected
                        ? "bg-rose-600 text-white"
                        : "bg-slate-800/60 text-slate-400 hover:text-white"
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
                className="w-full bg-slate-900/90 border border-slate-700/80 focus:border-rose-500 rounded-xl px-4 py-2.5 text-sm font-medium text-white placeholder-slate-500 outline-none transition"
                maxLength={200}
                disabled={publishing}
              />

              <button
                type="submit"
                disabled={publishing}
                className="bg-rose-600 hover:bg-rose-500 active:scale-95 text-white font-bold text-xs px-5 h-[42px] rounded-xl transition flex items-center gap-1 flex-shrink-0 cursor-pointer disabled:opacity-50"
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

            {err && <div className="text-xs text-rose-400 font-medium px-1">{err}</div>}
          </form>

        </div>

      </div>
    </PageShell>
  );
}
