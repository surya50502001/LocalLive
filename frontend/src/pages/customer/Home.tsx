import { useEffect, useState, useRef, useCallback } from "react";
import { Link } from "react-router-dom";
import { apiFetch } from "../../api/client";
import type { CategoryDto, RequestDto, ShopDto, ShopAvailableDto } from "../../types";
import { PageShell, Spinner } from "../../components/Ui";
import { getCurrentPosition } from "../../lib/geo";
import { connectSignalR } from "../../lib/signalr";
import { useAuthStore } from "../../store/authStore";
import InAppNavigationModal from "../../components/InAppNavigationModal";
import ChatDrawer from "../../components/ChatDrawer";

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
  const { user } = useAuthStore();
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [inputText, setInputText] = useState("");
  const [selectedCategory, setSelectedCategory] = useState<string>("");
  const [lat, setLat] = useState<number | null>(null);
  const [lng, setLng] = useState<number | null>(null);
  const [publishing, setPublishing] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  // Live pool of requests
  const [myRequests, setMyRequests] = useState<RequestDto[]>([]);
  const [loadingPool, setLoadingPool] = useState(true);

  // Search state
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<{ categories: CategoryDto[]; shops: ShopDto[] } | null>(null);
  const [searching, setSearching] = useState(false);

  // In-App Navigation Modal state
  const [navDestination, setNavDestination] = useState<{
    name: string;
    latitude: number;
    longitude: number;
    address?: string;
    phone?: string;
  } | null>(null);

  // Contextual Chat Drawer state
  const [chatTarget, setChatTarget] = useState<{
    requestId: string;
    shopId: string;
    shopName: string;
    requestTitle: string;
  } | null>(null);

  const chatEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // 1. Initial Data Load & Location Acquisition
  useEffect(() => {
    let mounted = true;

    getCurrentPosition()
      .then((p) => {
        if (!mounted) return;
        setLat(p.latitude);
        setLng(p.longitude);
      })
      .catch(() => {
        if (!mounted) return;
        setLat(12.9716);
        setLng(77.5946); // Default downtown center fallback
      });

    apiFetch<CategoryDto[]>("/api/categories")
      .then((data) => {
        if (!mounted) return;
        setCategories(data);
        if (data.length > 0) setSelectedCategory(data[0].id);
      })
      .catch(() => {});

    return () => {
      mounted = false;
    };
  }, []);

  // 2. Fetch Live Stream Requests
  const fetchLiveStream = useCallback(async () => {
    try {
      const data = await apiFetch<RequestDto[]>("/api/requests/my/live");
      setMyRequests(data);
    } catch {
      // Ignore if not logged in or network error
    } finally {
      setLoadingPool(false);
    }
  }, []);

  useEffect(() => {
    fetchLiveStream();
    const interval = setInterval(fetchLiveStream, 8000);
    return () => clearInterval(interval);
  }, [fetchLiveStream]);

  // 3. Real-Time SignalR Updates
  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const hub = await connectSignalR();
        const onShopAvailable = () => {
          if (mounted) fetchLiveStream();
        };
        const onStatusChanged = (payload: unknown) => {
          if (!mounted) return;
          const p = payload as { requestId: string; status: string };
          setMyRequests((prev) =>
            prev.map((r) => (r.id === p.requestId ? { ...r, status: p.status } : r))
          );
        };
        const onRequestClosed = (payload: unknown) => {
          if (!mounted) return;
          const p = payload as { requestId: string };
          setMyRequests((prev) => prev.filter((r) => r.id !== p.requestId));
        };

        hub.on("ShopAvailable", onShopAvailable);
        hub.on("RequestStatusChanged", onStatusChanged);
        hub.on("RequestClosed", onRequestClosed);

        return () => {
          hub.off("ShopAvailable", onShopAvailable);
          hub.off("RequestStatusChanged", onStatusChanged);
          hub.off("RequestClosed", onRequestClosed);
        };
      } catch {
        /* fallback to polling */
      }
    })();
    return () => {
      mounted = false;
    };
  }, [fetchLiveStream]);

  // 4. Instant Search Handler
  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!searchQuery.trim()) {
      setSearchResults(null);
      return;
    }

    setSearching(true);
    try {
      const locParams = lat && lng ? `&latitude=${lat}&longitude=${lng}` : "";
      const data = await apiFetch<{ categories: CategoryDto[]; shops: ShopDto[] }>(
        `/api/search?q=${encodeURIComponent(searchQuery.trim())}${locParams}`
      );
      setSearchResults(data);
    } catch {
      setSearchResults(null);
    } finally {
      setSearching(false);
    }
  };

  // 5. Publish Live Request
  const handleSendRequest = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!inputText.trim()) return;

    if (!lat || !lng) {
      setErr("Please allow location access to match nearby open shops.");
      return;
    }

    const titleToSend = inputText.trim();
    setPublishing(true);
    setErr(null);

    try {
      const catId = selectedCategory || categories[0]?.id;
      const created = await apiFetch<RequestDto>("/api/requests", {
        method: "POST",
        body: JSON.stringify({
          title: titleToSend,
          categoryId: catId,
          latitude: lat,
          longitude: lng,
          ttlMinutes: 30,
        }),
      });

      setMyRequests((prev) => [created, ...prev]);
      setInputText("");
      setTimeout(() => {
        chatEndRef.current?.scrollIntoView({ behavior: "smooth" });
      }, 150);
    } catch (ex: unknown) {
      setErr((ex as { detail?: string })?.detail ?? "Failed to publish request. Please retry.");
    } finally {
      setPublishing(false);
    }
  };

  const selectPreset = (preset: QuickPreset) => {
    setInputText(preset.text);
    if (preset.categorySlug) {
      const matched = categories.find((c) => c.slug === preset.categorySlug);
      if (matched) setSelectedCategory(matched.id);
    }
    inputRef.current?.focus();
  };

  const formatCountdown = (expiresAt: string) => {
    const diff = Math.max(0, Math.floor((new Date(expiresAt).getTime() - Date.now()) / 60000));
    return `${diff}m`;
  };

  return (
    <PageShell>
      <div className="flex flex-col h-[calc(100vh-6rem)] max-w-2xl mx-auto">
        {/* TOP SEARCH & DISCOVERY BAR */}
        <div className="pb-3 border-b border-white/10">
          <form onSubmit={handleSearch} className="flex gap-2">
            <div className="relative flex-1">
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => {
                  setSearchQuery(e.target.value);
                  if (!e.target.value) setSearchResults(null);
                }}
                placeholder="Search shops or categories nearby…"
                className="w-full rounded-xl bg-slate-900 border border-slate-700 px-4 py-2 text-xs text-white placeholder-slate-400 focus:outline-none focus:border-indigo-500"
              />
              {searchQuery && (
                <button
                  type="button"
                  onClick={() => {
                    setSearchQuery("");
                    setSearchResults(null);
                  }}
                  className="absolute right-3 top-2.5 text-xs text-slate-400 hover:text-white"
                >
                  ✕
                </button>
              )}
            </div>
            <button
              type="submit"
              disabled={searching}
              className="rounded-xl bg-indigo-600 px-4 py-2 text-xs font-bold text-white hover:bg-indigo-500 transition"
            >
              {searching ? "…" : "Search"}
            </button>
          </form>

          {/* Search Results Dropdown */}
          {searchResults && (
            <div className="mt-2 rounded-2xl bg-slate-900 border border-slate-800 p-4 shadow-2xl max-h-80 overflow-y-auto space-y-3">
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-slate-400 uppercase tracking-wider">Search Results</span>
                <button
                  onClick={() => setSearchResults(null)}
                  className="text-xs text-slate-400 hover:text-white"
                >
                  Close
                </button>
              </div>

              {searchResults.shops.length === 0 && searchResults.categories.length === 0 ? (
                <p className="text-xs text-slate-400">No matching shops or categories found nearby.</p>
              ) : (
                <>
                  {searchResults.categories.length > 0 && (
                    <div className="space-y-1">
                      <p className="text-[11px] font-semibold text-indigo-400">Categories</p>
                      <div className="flex flex-wrap gap-1.5">
                        {searchResults.categories.map((c) => (
                          <button
                            key={c.id}
                            onClick={() => {
                              setSelectedCategory(c.id);
                              setSearchResults(null);
                            }}
                            className="rounded-lg bg-slate-800 px-2.5 py-1 text-xs text-slate-200 hover:bg-slate-700"
                          >
                            {c.name}
                          </button>
                        ))}
                      </div>
                    </div>
                  )}

                  {searchResults.shops.length > 0 && (
                    <div className="space-y-2 pt-2 border-t border-slate-800">
                      <p className="text-[11px] font-semibold text-emerald-400">Nearby Verified Shops</p>
                      {searchResults.shops.map((s) => (
                        <div
                          key={s.id}
                          className="flex items-center justify-between rounded-xl bg-slate-950 p-2.5 border border-slate-800/80"
                        >
                          <div>
                            <p className="text-xs font-bold text-white">{s.name}</p>
                            <p className="text-[10px] text-slate-400 truncate max-w-[200px]">
                              {s.address} · {s.distanceM ? `${Math.round(s.distanceM)}m away` : "Nearby"}
                            </p>
                          </div>
                          <button
                            onClick={() =>
                              setNavDestination({
                                name: s.name,
                                latitude: s.latitude,
                                longitude: s.longitude,
                                address: s.address,
                                phone: s.phone,
                              })
                            }
                            className="rounded-lg bg-emerald-600 px-3 py-1.5 text-xs font-bold text-white hover:bg-emerald-700 flex items-center gap-1 shadow-sm"
                          >
                            <span>🧭</span>
                            <span>Navigate</span>
                          </button>
                        </div>
                      ))}
                    </div>
                  )}
                </>
              )}
            </div>
          )}
        </div>

        {/* ERROR NOTIFICATION */}
        {err && (
          <div className="mt-2 rounded-xl bg-rose-500/10 border border-rose-500/30 p-2.5 text-xs text-rose-300">
            {err}
          </div>
        )}

        {/* LIVE REQUEST STREAM */}
        <div className="flex-1 overflow-y-auto px-1 py-3 space-y-4">
          {loadingPool ? (
            <div className="flex flex-col items-center justify-center h-full text-slate-400 space-y-2">
              <Spinner />
              <p className="text-xs font-mono">Connecting to live network…</p>
            </div>
          ) : myRequests.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full text-center p-8 space-y-2 text-slate-400">
              <div className="text-3xl">📡</div>
              <p className="text-sm font-semibold text-slate-200">No active requests in your area</p>
              <p className="text-xs max-w-xs text-slate-400">
                Ask for what you need right now. Verified local shops will confirm instantaneous availability.
              </p>
            </div>
          ) : (
            myRequests.map((req) => {
              const availableShops = req.availableShops || [];
              const isActive = req.status === "Active";

              return (
                <div key={req.id} className="rounded-2xl bg-slate-900 border border-slate-800 p-4 space-y-3 shadow-md">
                  {/* Request Header */}
                  <div className="flex items-center justify-between text-xs">
                    <div className="flex items-center gap-2">
                      <span className="inline-flex items-center gap-1 rounded-full bg-rose-950/60 px-2 py-0.5 text-[10px] font-black tracking-wider text-rose-400 border border-rose-800/40">
                        <span className={`h-1.5 w-1.5 rounded-full ${isActive ? "bg-rose-500 animate-pulse" : "bg-slate-500"}`} />
                        {isActive ? "LIVE" : req.status.toUpperCase()}
                      </span>
                      <span className="text-[10px] text-slate-400 bg-slate-800 px-2 py-0.5 rounded-md">
                        {req.categoryName}
                      </span>
                    </div>
                    <span className="text-[11px] font-mono text-slate-400">
                      ⏱️ {formatCountdown(req.expiresAt)}
                    </span>
                  </div>

                  <Link to={`/customer/requests/${req.id}`} className="block">
                    <h3 className="text-base font-extrabold text-white hover:text-indigo-400 transition">
                      {req.title}
                    </h3>
                  </Link>

                  <div className="flex items-center justify-between text-[11px] text-slate-400 border-t border-slate-800/60 pt-2">
                    <span>Notified {req.notifiedShopsCount} open shops</span>
                    <Link
                      to={`/customer/requests/${req.id}`}
                      className="font-bold text-indigo-400 hover:underline"
                    >
                      View Live Tracker →
                    </Link>
                  </div>

                  {/* Responding Shops */}
                  {availableShops.length > 0 && (
                    <div className="pl-3 border-l-2 border-emerald-500/50 space-y-2 pt-1">
                      {availableShops.map((shop: ShopAvailableDto) => (
                        <div
                          key={shop.shopId}
                          className="rounded-xl bg-slate-950 p-3 border border-emerald-500/30 space-y-2"
                        >
                          <div className="flex items-center justify-between">
                            <span className="font-bold text-emerald-300 flex items-center gap-1.5 text-xs">
                              <span className="h-2 w-2 rounded-full bg-emerald-400 animate-pulse" />
                              {shop.shopName}
                              <span className="text-[10px] text-emerald-400 bg-emerald-950 px-1.5 py-0.5 rounded font-black border border-emerald-800/60">
                                AVAILABLE NOW
                              </span>
                            </span>
                            <span className="font-mono text-emerald-400 text-[11px]">
                              {shop.distanceM ? `${Math.round(shop.distanceM)}m away` : "Nearby"}
                            </span>
                          </div>

                          {shop.message && (
                            <p className="text-[11px] text-slate-300 italic bg-slate-900/60 p-1.5 rounded-lg border border-slate-800">
                              “{shop.message}”
                            </p>
                          )}

                          <p className="text-slate-400 text-[10px] font-mono">
                            {shop.address} {shop.phone ? `· 📞 ${shop.phone}` : ""}
                          </p>

                          {/* Action Buttons: In-App Navigation & Real-time Chat */}
                          <div className="flex gap-2 pt-1">
                            <button
                              onClick={() =>
                                setNavDestination({
                                  name: shop.shopName,
                                  latitude: shop.latitude || req.latitude,
                                  longitude: shop.longitude || req.longitude,
                                  address: shop.address,
                                  phone: shop.phone,
                                })
                              }
                              className="flex-1 rounded-xl bg-emerald-600 px-3 py-2 text-xs font-bold text-white hover:bg-emerald-700 transition flex items-center justify-center gap-1 shadow-sm"
                            >
                              <span>🧭</span>
                              <span>GO THERE — Navigate</span>
                            </button>

                            <button
                              onClick={() =>
                                setChatTarget({
                                  requestId: req.id,
                                  shopId: shop.shopId,
                                  shopName: shop.shopName,
                                  requestTitle: req.title,
                                })
                              }
                              className="rounded-xl bg-indigo-600 px-3 py-2 text-xs font-bold text-white hover:bg-indigo-700 transition flex items-center gap-1 shadow-sm"
                            >
                              <span>💬</span>
                              <span>Chat</span>
                            </button>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              );
            })
          )}
          <div ref={chatEndRef} />
        </div>

        {/* QUICK PRESET CHIPS */}
        <div className="py-2 overflow-x-auto flex gap-1.5 no-scrollbar border-t border-white/5">
          {PRESETS.map((p, idx) => (
            <button
              key={idx}
              type="button"
              onClick={() => selectPreset(p)}
              className="flex items-center gap-1.5 text-xs bg-slate-900 border border-slate-800 text-slate-300 hover:text-white hover:border-slate-700 rounded-full px-3 py-1.5 whitespace-nowrap transition cursor-pointer"
            >
              <span>{p.icon}</span>
              <span>{p.text}</span>
            </button>
          ))}
        </div>

        {/* COMPOSER FORM */}
        <div className="pt-2">
          <form onSubmit={handleSendRequest} className="space-y-2">
            <div className="flex items-center gap-2">
              <input
                ref={inputRef}
                type="text"
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                placeholder="What do you need right now? (e.g. Paracetamol 500mg, Size 10 Running Shoes)"
                disabled={publishing}
                className="flex-1 bg-slate-900 border border-slate-700 rounded-xl px-4 py-3 text-sm text-white placeholder-slate-400 focus:outline-none focus:border-rose-500 shadow-inner"
              />
              <button
                type="submit"
                disabled={publishing || !inputText.trim()}
                className="bg-rose-600 hover:bg-rose-700 disabled:opacity-40 text-white font-bold rounded-xl px-5 py-3 text-sm flex items-center gap-1.5 transition shadow-lg shrink-0 cursor-pointer"
              >
                <span>Broadcast</span>
                <span>📡</span>
              </button>
            </div>

            {/* Category selection row */}
            <div className="flex items-center justify-between text-xs px-1">
              <div className="flex items-center gap-1.5">
                <span className="text-slate-400">Category:</span>
                <select
                  value={selectedCategory}
                  onChange={(e) => setSelectedCategory(e.target.value)}
                  className="bg-slate-900 border border-slate-700 rounded-lg px-2 py-1 text-xs text-white focus:outline-none"
                >
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
              <span className="text-[11px] text-slate-400">
                {lat && lng ? "📍 Location locked" : "Acquiring GPS…"}
              </span>
            </div>
          </form>
        </div>
      </div>

      {/* In-App Interactive Navigation Modal (No External Redirects) */}
      {navDestination && (
        <InAppNavigationModal
          isOpen={!!navDestination}
          onClose={() => setNavDestination(null)}
          destination={navDestination}
        />
      )}

      {/* Real-Time Contextual Chat Drawer */}
      {chatTarget && user && (
        <ChatDrawer
          isOpen={!!chatTarget}
          onClose={() => setChatTarget(null)}
          requestId={chatTarget.requestId}
          shopId={chatTarget.shopId}
          title={chatTarget.requestTitle}
          otherPartyName={chatTarget.shopName}
          currentUserId={user.id}
        />
      )}
    </PageShell>
  );
}
