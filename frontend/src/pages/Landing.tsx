import { Link } from "react-router-dom";
import { PageShell, Card, Badge } from "../components/Ui";

export default function Landing() {
  return (
    <PageShell>
      <div className="mx-auto max-w-3xl text-center py-12 sm:py-16">
        <p className="inline-flex items-center gap-2 rounded-full bg-red-50 px-3 py-1 text-xs font-bold tracking-widest text-red-600">
          <span className="h-2 w-2 rounded-full bg-red-600 animate-pulse" /> LIVE HYPERLOCAL NETWORK
        </p>
        <h1 className="mt-4 text-4xl font-extrabold tracking-tight sm:text-5xl">
          I need this <span className="text-red-600">right now.</span>
        </h1>
        <p className="mt-4 text-base text-gray-600 sm:text-lg">
          Ask for what you need. Nearby open shops see it in real time and confirm availability. You go there.
        </p>
        <p className="mt-2 text-sm text-gray-500">No quotation. No bidding. No delivery. Just <strong>REQUEST → AVAILABLE → GO THERE</strong>.</p>
        <div className="mt-8 flex flex-col sm:flex-row items-center justify-center gap-3">
          <Link to="/register" className="w-full sm:w-auto rounded-xl bg-gray-900 px-6 py-3.5 text-sm font-bold text-white hover:bg-black">Get started — it&apos;s free</Link>
          <Link to="/login" className="w-full sm:w-auto rounded-xl border border-gray-300 bg-white px-6 py-3.5 text-sm font-bold hover:bg-gray-50">Log in</Link>
        </div>
        <div className="mt-10 grid grid-cols-1 gap-4 text-left sm:grid-cols-3">
          {[
            { k: "Customer", v: "Create a LIVE request with your location. Watch nearby shops confirm availability in real time." },
            { k: "Shop", v: "Receive requests matching your category within your radius. One tap: AVAILABLE." },
            { k: "Go there", v: "Customer sees your shop + distance and opens navigation immediately." },
          ].map((x) => (
            <Card key={x.k}><p className="text-sm font-bold">{x.k}</p><p className="mt-1 text-sm text-gray-600">{x.v}</p></Card>
          ))}
        </div>
        <div className="mt-8 flex items-center justify-center gap-2 text-xs text-gray-500">
          <Badge tone="red">LIVE</Badge> Real-time via SignalR · Real shops · Real location · Real database
        </div>
      </div>
    </PageShell>
  );
}
