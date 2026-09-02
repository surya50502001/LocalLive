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
    <header className="sticky top-0 z-30 border-b border-gray-200 bg-white/90 backdrop-blur-md text-gray-900">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3 sm:px-6">
        <Link to="/" className="flex items-center gap-2">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-red-600 text-sm font-bold text-white shadow-lg shadow-red-600/30">LL</span>
          <span className="text-base font-extrabold tracking-tight">LocalLive</span>
          <span className="hidden sm:inline-flex items-center gap-1 rounded-full bg-red-600 px-2.5 py-0.5 text-[10px] font-extrabold tracking-widest text-white shadow-sm shadow-red-500/50 animate-pulse">
            <span className="h-1.5 w-1.5 rounded-full bg-white animate-ping" /> POOL
          </span>
        </Link>
        <nav className="flex items-center gap-2 text-sm font-medium">
          {!isAuthenticated ? (
            <>
              <Link to="/login" className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Log in</Link>
              <Link to="/register" className="rounded-lg bg-red-600 px-4 py-2 font-semibold text-white hover:bg-red-500 shadow-md shadow-red-600/20 transition">Sign up</Link>
            </>
          ) : role === "Admin" ? (
            <>
              <Link to="/admin" className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Admin</Link>
              <button onClick={handleLogout} className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Log out</button>
              <span className="hidden sm:inline text-xs text-gray-400">{user?.email}</span>
            </>
          ) : role === "ShopOwner" ? (
            <>
              <Link to="/shop" className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Dashboard</Link>
              <Link to="/shop/requests" className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Live Stream</Link>
              <button onClick={handleLogout} className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Log out</button>
            </>
          ) : (
            <>
              <Link to="/customer" className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Live Pool ⚡</Link>
              <Link to="/customer/requests" className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">My Requests</Link>
              <button onClick={handleLogout} className="rounded-lg px-3 py-2 text-gray-600 hover:bg-gray-100 hover:text-gray-900 transition">Log out</button>
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
