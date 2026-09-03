import { Link, useNavigate } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import { disconnectSignalR } from "../lib/signalr";

export function Navbar() {
  const { user, isAuthenticated, logout, role } = useAuthStore();
  const navigate = useNavigate();
  const handleLogout = async () => {
    try { await disconnectSignalR(); } catch { /* ignore */ }
    logout();
    navigate("/login");
  };
  return (
    <header className="sticky top-0 z-30 border-b border-white/5 bg-[#090d16]/95 backdrop-blur-md text-slate-100 shadow-md">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6">
        <Link to="/" className="flex items-center gap-2">
          <span className="flex h-8 w-8 items-center justify-center rounded-xl mini-button-primary text-xs font-black text-white">LL</span>
          <span className="text-base font-extrabold tracking-tight text-white">LocalLive</span>
          <span className="hidden sm:inline-flex items-center gap-1.5 rounded-full bg-slate-900 border border-rose-500/30 px-2.5 py-0.5 text-[10px] font-bold tracking-widest text-rose-400">
            <span className="h-1.5 w-1.5 rounded-full bg-rose-500 animate-pulse" /> POOL
          </span>
        </Link>
        <nav className="flex items-center gap-2 text-sm font-semibold">
          {!isAuthenticated ? (
            <>
              <Link to="/login" className="mini-button-secondary px-3 py-1.5 text-xs text-slate-200 hover:text-white">Log in</Link>
              <Link to="/register" className="mini-button-primary px-4 py-1.5 text-xs text-white">Sign up</Link>
            </>
          ) : role === "Admin" ? (
            <>
              <Link to="/admin" className="mini-button-secondary px-3 py-1.5 text-xs text-slate-200">Admin</Link>
              <button onClick={handleLogout} className="mini-button-secondary px-3 py-1.5 text-xs text-slate-300">Log out</button>
              <span className="hidden sm:inline text-xs font-mono text-slate-400">{user?.email}</span>
            </>
          ) : role === "ShopOwner" ? (
            <>
              <Link to="/shop" className="mini-button-secondary px-3 py-1.5 text-xs text-slate-200">Dashboard</Link>
              <Link to="/shop/requests" className="mini-button-secondary px-3 py-1.5 text-xs text-slate-200">Live Stream</Link>
              <button onClick={handleLogout} className="mini-button-secondary px-3 py-1.5 text-xs text-slate-300">Log out</button>
            </>
          ) : (
            <>
              <Link to="/customer" className="mini-button-primary px-3 py-1.5 text-xs text-white">Live Pool ⚡</Link>
              <Link to="/customer/favorites" className="mini-button-secondary px-2.5 py-1.5 text-xs text-slate-200">⭐ Favorites</Link>
              <Link to="/customer/profile" className="mini-button-secondary px-2.5 py-1.5 text-xs text-slate-200">👤 Profile</Link>
              <button onClick={handleLogout} className="mini-button-secondary px-2.5 py-1.5 text-xs text-slate-300">Log out</button>
            </>
          )}
        </nav>
      </div>
    </header>
  );
}

export function ToastContainer({ toasts, onDismiss }: { toasts: { id: number; message: string; tone?: string }[]; onDismiss: (id: number) => void }) {
  return (
    <div className="pointer-events-none fixed bottom-4 right-4 z-50 flex flex-col gap-2">
      {toasts.map((t) => (
        <div
          key={t.id}
          onClick={() => onDismiss(t.id)}
          className={`pointer-events-auto cursor-pointer rounded-xl px-4 py-3 text-sm font-medium shadow-lg ${t.tone === "error" ? "bg-red-600 text-white" : t.tone === "success" ? "bg-green-600 text-white" : "bg-gray-900 text-white"}`}
        >
          {t.message}
        </div>
      ))}
    </div>
  );
}
