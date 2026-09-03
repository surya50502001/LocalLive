import { useEffect, useState, useCallback } from "react";
import { Link } from "react-router-dom";
import { apiFetch } from "../../api/client";
import type { ShopDto } from "../../types";
import { PageShell, Card, Badge, Spinner } from "../../components/Ui";
import InAppNavigationModal from "../../components/InAppNavigationModal";

export default function CustomerFavorites() {
  const [favorites, setFavorites] = useState<ShopDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [err, setErr] = useState<string | null>(null);

  // In-app navigation modal state
  const [navDestination, setNavDestination] = useState<{
    name: string;
    latitude: number;
    longitude: number;
    address?: string;
    phone?: string;
  } | null>(null);

  const fetchFavorites = useCallback(async () => {
    try {
      const data = await apiFetch<ShopDto[]>("/api/shops/favorites");
      setFavorites(data);
    } catch {
      setErr("Failed to load favorite shops.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchFavorites();
  }, [fetchFavorites]);

  const removeFavorite = async (shopId: string) => {
    try {
      await apiFetch(`/api/shops/${shopId}/favorite`, { method: "POST" });
      setFavorites((prev) => prev.filter((s) => s.id !== shopId));
    } catch {
      alert("Failed to remove favorite.");
    }
  };

  if (loading) {
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
      <div className="mx-auto max-w-2xl space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-xl font-extrabold text-gray-900">⭐ Favorite Shops</h1>
            <p className="text-xs text-gray-500">Quick access to your preferred neighborhood merchants</p>
          </div>
          <Link to="/customer" className="text-xs font-semibold text-gray-600 hover:text-gray-900">
            ← Home
          </Link>
        </div>

        {err && (
          <div className="rounded-xl bg-red-50 border border-red-200 p-3 text-xs text-red-700">
            {err}
          </div>
        )}

        {favorites.length === 0 ? (
          <Card className="text-center py-12">
            <div className="text-3xl mb-2">⭐</div>
            <p className="text-sm font-bold text-gray-800">No favorite shops saved yet</p>
            <p className="mt-1 text-xs text-gray-500 max-w-xs mx-auto">
              Whenever a shop responds to your live requests, you can tap the star icon to save them here for easy repeat visits.
            </p>
            <Link
              to="/customer"
              className="mt-4 inline-block text-xs font-bold text-indigo-600 hover:underline"
            >
              Browse live pool →
            </Link>
          </Card>
        ) : (
          <ul className="space-y-3">
            {favorites.map((s) => (
              <li key={s.id}>
                <Card className="space-y-3">
                  <div className="flex items-start justify-between gap-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <h2 className="text-base font-bold text-gray-900">{s.name}</h2>
                        <Badge tone={s.isOpen ? "green" : "gray"}>
                          {s.isOpen ? "OPEN" : "CLOSED"}
                        </Badge>
                      </div>
                      <p className="text-xs text-gray-600 mt-1">{s.address} {s.phone ? `· ${s.phone}` : ""}</p>
                      <div className="flex flex-wrap gap-1 mt-2">
                        {s.categories.map((c) => (
                          <span
                            key={c.id}
                            className="rounded-full bg-gray-100 px-2 py-0.5 text-[10px] font-medium text-gray-600"
                          >
                            {c.name}
                          </span>
                        ))}
                      </div>
                    </div>
                    <button
                      onClick={() => removeFavorite(s.id)}
                      className="text-xs text-red-500 hover:text-red-700 p-1"
                      title="Remove from favorites"
                    >
                      ✕
                    </button>
                  </div>

                  <div className="pt-2 border-t border-gray-100 flex gap-2">
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
                      className="flex-1 rounded-xl bg-emerald-600 px-4 py-2.5 text-xs font-bold text-white hover:bg-emerald-700 flex items-center justify-center gap-1.5 transition shadow-sm"
                    >
                      <span>🧭</span>
                      <span>GO THERE — In-App Navigation</span>
                    </button>
                    {s.phone && (
                      <a
                        href={`tel:${s.phone}`}
                        className="rounded-xl bg-gray-100 hover:bg-gray-200 px-3 py-2.5 text-xs font-bold text-gray-700 flex items-center gap-1 transition"
                      >
                        📞 Call
                      </a>
                    )}
                  </div>
                </Card>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* In-App Interactive Navigation Modal */}
      {navDestination && (
        <InAppNavigationModal
          isOpen={!!navDestination}
          onClose={() => setNavDestination(null)}
          destination={navDestination}
        />
      )}
    </PageShell>
  );
}
