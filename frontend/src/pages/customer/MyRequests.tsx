import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { apiFetch } from "../../api/client";
import type { RequestDto } from "../../types";
import { PageShell, Card, Badge, Spinner } from "../../components/Ui";
import { formatDistance } from "../../lib/geo";

export default function MyRequests() {
  const [items, setItems] = useState<RequestDto[] | null>(null);
  const [err, setErr] = useState<string | null>(null);
  useEffect(() => {
    apiFetch<RequestDto[]>("/api/requests/my/live")
      .then(setItems)
      .catch((ex: unknown) => setErr((ex as { detail?: string })?.detail ?? "Failed to load requests."));
  }, []);
  if (err) return <PageShell><Card><p className="text-sm text-red-600">{err}</p></Card></PageShell>;
  if (items === null) return <PageShell><div className="flex justify-center py-16"><Spinner /></div></PageShell>;
  return (
    <PageShell>
      <div className="mx-auto max-w-2xl space-y-4">
        <h1 className="text-xl font-bold">My LIVE requests</h1>
        <p className="text-sm text-gray-600">Active requests that nearby shops can respond to right now.</p>
        {items.length === 0 ? (
          <Card><p className="text-sm text-gray-600">No active requests. <Link to="/customer" className="font-semibold underline">Create one</Link>.</p></Card>
        ) : (
          <ul className="space-y-3">
            {items.map((r) => (
              <li key={r.id}>
                <Link to={`/customer/requests/${r.id}`} className="block">
                  <Card className="hover:border-gray-300">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <p className="text-sm font-bold">{r.title}</p>
                        <p className="text-xs text-gray-500">{r.categoryName} · {new Date(r.createdAt).toLocaleString()} · {r.availableShops.length} available</p>
                        {r.distanceM != null && <p className="text-xs text-gray-500">{formatDistance(r.distanceM)} · {r.notifiedShopsCount} shops notified</p>}
                      </div>
                      <Badge tone={r.status === "Active" ? "red" : "gray"}>{r.status}</Badge>
                    </div>
                  </Card>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </div>
    </PageShell>
  );
}
