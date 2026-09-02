import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { useEffect } from "react";
import { useAuthStore } from "./store/authStore";
import { Navbar } from "./components/Navbar";
import { RequireAuth, RedirectIfAuthed } from "./components/Guards";
import Landing from "./pages/Landing";
import Login from "./pages/Login";
import Register from "./pages/Register";
import CustomerHome from "./pages/customer/Home";
import RequestLive from "./pages/customer/RequestLive";
import MyRequests from "./pages/customer/MyRequests";
import ShopDashboard from "./pages/shop/Dashboard";
import ShopRequests from "./pages/shop/Requests";
import AdminPanel from "./pages/admin/Panel";

export default function App() {
  const hydrate = useAuthStore((s) => s.hydrate);
  useEffect(() => { hydrate(); }, [hydrate]);

  return (
    <BrowserRouter>
      <div className="min-h-screen bg-gray-50">
        <Navbar />
        <Routes>
          <Route path="/" element={<Landing />} />
          <Route path="/login" element={<RedirectIfAuthed><Login /></RedirectIfAuthed>} />
          <Route path="/register" element={<RedirectIfAuthed><Register /></RedirectIfAuthed>} />

          <Route path="/customer" element={<RequireAuth roles={["Customer"]}><CustomerHome /></RequireAuth>} />
          <Route path="/customer/requests" element={<RequireAuth roles={["Customer"]}><MyRequests /></RequireAuth>} />
          <Route path="/customer/requests/:id" element={<RequireAuth roles={["Customer"]}><RequestLive /></RequireAuth>} />

          <Route path="/shop" element={<RequireAuth roles={["ShopOwner"]}><ShopDashboard /></RequireAuth>} />
          <Route path="/shop/requests" element={<RequireAuth roles={["ShopOwner"]}><ShopRequests /></RequireAuth>} />

          <Route path="/admin" element={<RequireAuth roles={["Admin"]}><AdminPanel /></RequireAuth>} />

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </div>
    </BrowserRouter>
  );
}
