import { useEffect, useState } from "react";
import { apiFetch } from "../../api/client";
import type { AdminStatsDto } from "../../types";
import { PageShell, Card, Spinner, Badge } from "../../components/Ui";
import { Button } from "../../components/Button";

type Tab = "overview" | "shops" | "users" | "requests" | "reports" | "categories";

export default function AdminPanel() {
  const [tab, setTab] = useState<Tab>("overview");
  return (
    <PageShell>
      <h1 className="text-xl font-bold">Admin</h1>
      <div className="mt-3 flex flex-wrap gap-2">
        {(["overview","shops","users","requests","reports","categories"] as Tab[]).map((t) => (
          <button key={t} onClick={() => setTab(t)} className={`rounded-lg px-3 py-2 text-sm font-semibold capitalize ${tab===t ? "bg-gray-900 text-white" : "bg-white border"}`}>{t}</button>
        ))}
      </div>
      <div className="mt-4">
        {tab==="overview" && <Overview />}
        {tab==="shops" && <ShopsTab />}
        {tab==="users" && <UsersTab />}
        {tab==="requests" && <RequestsTab />}
        {tab==="reports" && <ReportsTab />}
        {tab==="categories" && <CategoriesTab />}
      </div>
    </PageShell>
  );
}

function Overview() {
  const [stats, setStats] = useState<AdminStatsDto | null>(null);
  useEffect(() => { apiFetch<AdminStatsDto>("/api/admin/stats").then(setStats).catch(()=>{}); }, []);
  if (!stats) return <div className="flex justify-center py-8"><Spinner /></div>;
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {[
          ["Users", stats.totalUsers], ["Shops", stats.totalShops], ["Verified", stats.verifiedShops], ["Pending", stats.pendingShops],
          ["Requests", stats.totalRequests], ["Active now", stats.activeRequestsNow], ["Fulfilled", stats.fulfilledRequests], ["Responses", stats.totalResponses],
        ].map(([k,v]) => (
          <Card key={k as string}><p className="text-xs text-gray-500">{k}</p><p className="text-xl font-bold">{String(v)}</p></Card>
        ))}
      </div>
      <Card>
        <p className="text-sm font-bold">Requests by category</p>
        <ul className="mt-2 space-y-1 text-sm">
          {stats.requestsByCategory.map((x) => <li key={x.categoryName} className="flex justify-between"><span>{x.categoryName}</span><span className="font-semibold">{x.count}</span></li>)}
          {stats.requestsByCategory.length===0 && <li className="text-gray-500">No data</li>}
        </ul>
      </Card>
      <Card>
        <p className="text-sm font-bold">Last 7 days</p>
        <div className="mt-2 overflow-x-auto">
          <table className="w-full text-sm">
            <thead><tr className="text-left text-xs text-gray-500"><th>Day</th><th>Req</th><th>Resp</th><th>Done</th></tr></thead>
            <tbody>{stats.requestsLast7Days.map((d) => <tr key={d.day}><td>{d.day}</td><td>{d.requests}</td><td>{d.responses}</td><td>{d.fulfilled}</td></tr>)}</tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}

function ShopsTab() {
  const [items, setItems] = useState<{ items: { id:string; name:string; status:string; isOpen:boolean; ownerEmail:string; categories:string[] }[]; total:number } | null>(null);
  const [q, setQ] = useState("");
  const load = async () => {
    const data = await apiFetch<{ items: { id:string; name:string; status:string; isOpen:boolean; ownerEmail:string; categories:string[] }[]; total:number }>(`/api/admin/shops?page=1&pageSize=50${q?`&search=${encodeURIComponent(q)}`:""}`);
    setItems(data);
  };
  useEffect(() => { load(); }, []);
  const act = async (id:string, action:"verify"|"disable"|"enable") => {
    await apiFetch(`/api/admin/shops/${id}/${action}`, { method:"POST" });
    await load();
  };
  if (!items) return <div className="flex justify-center py-8"><Spinner /></div>;
  return (
    <div className="space-y-3">
      <div className="flex gap-2"><input value={q} onChange={(e)=>setQ(e.target.value)} placeholder="Search shops or owner email" className="flex-1 rounded-lg border px-3 py-2 text-sm" /><Button onClick={load}>Search</Button></div>
      {items.items.map((s) => (
        <Card key={s.id}>
          <div className="flex items-start justify-between gap-2">
            <div><p className="text-sm font-bold">{s.name}</p><p className="text-xs text-gray-500">{s.ownerEmail} · {s.categories.join(", ")}</p></div>
            <Badge tone={s.status==="Verified"?"green":s.status==="Pending"?"amber":"red"}>{s.status}</Badge>
          </div>
          <div className="mt-2 flex gap-2">
            <Button onClick={()=>act(s.id,"verify")} className="text-xs">Verify</Button>
            <Button onClick={()=>act(s.id,"disable")} className="bg-red-600 hover:bg-red-700 text-xs">Disable</Button>
            <Button onClick={()=>act(s.id,"enable")} className="bg-green-600 hover:bg-green-700 text-xs">Enable</Button>
          </div>
        </Card>
      ))}
    </div>
  );
}

function UsersTab() {
  const [items, setItems] = useState<{ items: { id:string; email:string; fullName:string; role:string; isBlocked:boolean }[] } | null>(null);
  const load = async () => { const d = await apiFetch<{ items: { id:string; email:string; fullName:string; role:string; isBlocked:boolean }[] }>(`/api/admin/users?page=1&pageSize=50`); setItems(d); };
  useEffect(()=>{ load(); }, []);
  const toggle = async (id:string, blocked:boolean) => {
    await apiFetch(`/api/admin/users/${id}/${blocked?"unblock":"block"}`, { method:"POST", body: JSON.stringify({ reason: blocked ? undefined : "Blocked by admin" }) });
    await load();
  };
  if (!items) return <div className="flex justify-center py-8"><Spinner /></div>;
  return (
    <div className="space-y-2">
      {items.items.map((u) => (
        <Card key={u.id}><div className="flex items-center justify-between"><div><p className="text-sm font-bold">{u.fullName} <span className="text-xs text-gray-500">({u.role})</span></p><p className="text-xs text-gray-500">{u.email}</p></div><div className="flex items-center gap-2"><Badge tone={u.isBlocked?"red":"green"}>{u.isBlocked?"Blocked":"Active"}</Badge><Button onClick={()=>toggle(u.id,u.isBlocked)} className={u.isBlocked?"bg-green-600 hover:bg-green-700":"bg-red-600 hover:bg-red-700"}>{u.isBlocked?"Unblock":"Block"}</Button></div></div></Card>
      ))}
    </div>
  );
}

function RequestsTab() {
  const [items, setItems] = useState<{ items: { id:string; title:string; status:string; categoryName:string; customerName:string }[] } | null>(null);
  useEffect(()=>{ apiFetch<{ items: { id:string; title:string; status:string; categoryName:string; customerName:string }[] }>(`/api/admin/requests?page=1&pageSize=50`).then(setItems).catch(()=>{}); }, []);
  if (!items) return <div className="flex justify-center py-8"><Spinner /></div>;
  return (
    <div className="space-y-2">
      {items.items.map((r) => <Card key={r.id}><div className="flex justify-between"><div><p className="text-sm font-bold">{r.title}</p><p className="text-xs text-gray-500">{r.categoryName} · {r.customerName}</p></div><Badge tone={r.status==="Active"?"red":r.status==="Fulfilled"?"green":"gray"}>{r.status}</Badge></div></Card>)}
      {items.items.length===0 && <Card><p className="text-sm text-gray-500">No requests.</p></Card>}
    </div>
  );
}

function ReportsTab() {
  const [items, setItems] = useState<{ items: { id:string; reason:string; status:string; targetType:string }[] } | null>(null);
  const load = async()=>{ const d=await apiFetch<{ items: { id:string; reason:string; status:string; targetType:string }[] }>(`/api/admin/reports?page=1&pageSize=50`); setItems(d); };
  useEffect(()=>{ load(); }, []);
  const act = async(id:string, kind:"resolve"|"dismiss")=>{ await apiFetch(`/api/admin/reports/${id}/${kind}`,{method:"POST"}); await load(); };
  if (!items) return <div className="flex justify-center py-8"><Spinner /></div>;
  return (
    <div className="space-y-2">
      {items.items.map((r)=><Card key={r.id}><div className="flex justify-between gap-2"><div><p className="text-sm font-bold">{r.targetType}</p><p className="text-xs text-gray-600">{r.reason}</p></div><Badge tone={r.status==="Open"?"amber":r.status==="Resolved"?"green":"gray"}>{r.status}</Badge></div><div className="mt-2 flex gap-2"><Button onClick={()=>act(r.id,"resolve")} className="text-xs">Resolve</Button><Button onClick={()=>act(r.id,"dismiss")} className="bg-gray-600 hover:bg-gray-700 text-xs">Dismiss</Button></div></Card>)}
      {items.items.length===0 && <Card><p className="text-sm text-gray-500">No reports.</p></Card>}
    </div>
  );
}

function CategoriesTab() {
  const [items, setItems] = useState<{ id:string; name:string; slug:string; icon?:string|null; sortOrder:number; isActive:boolean }[] | null>(null);
  const [name, setName] = useState(""); const [icon, setIcon] = useState(""); const [sort, setSort] = useState(0);
  const load = async()=>{ const d=await apiFetch<{ id:string; name:string; slug:string; icon?:string|null; sortOrder:number; isActive:boolean }[]>("/api/admin/categories"); setItems(d); };
  useEffect(()=>{ load(); }, []);
  const create = async()=>{
    await apiFetch("/api/admin/categories",{ method:"POST", body: JSON.stringify({ name, icon: icon||undefined, sortOrder: sort }) });
    setName(""); setIcon(""); await load();
  };
  const del = async(id:string)=>{ await apiFetch(`/api/admin/categories/${id}`,{method:"DELETE"}); await load(); };
  if (!items) return <div className="flex justify-center py-8"><Spinner /></div>;
  return (
    <div className="space-y-3">
      <Card>
        <p className="text-sm font-bold">Add category</p>
        <div className="mt-2 flex gap-2">
          <input value={name} onChange={(e)=>setName(e.target.value)} placeholder="Name" className="flex-1 rounded-lg border px-3 py-2 text-sm" />
          <input value={icon} onChange={(e)=>setIcon(e.target.value)} placeholder="Icon" className="w-24 rounded-lg border px-3 py-2 text-sm" />
          <input type="number" value={sort} onChange={(e)=>setSort(Number(e.target.value))} className="w-20 rounded-lg border px-3 py-2 text-sm" />
          <Button onClick={create}>Add</Button>
        </div>
      </Card>
      {items.map((c)=><Card key={c.id}><div className="flex justify-between"><div><p className="text-sm font-bold">{c.icon} {c.name}</p><p className="text-xs text-gray-500">{c.slug} · order {c.sortOrder} · {c.isActive?"active":"inactive"}</p></div><Button onClick={()=>del(c.id)} className="bg-red-600 hover:bg-red-700">Delete</Button></div></Card>)}
    </div>
  );
}
