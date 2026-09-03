import { useEffect, useRef, useState, useCallback } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { apiFetch } from "../api/client";

// Fix default leaflet icons
delete (L.Icon.Default.prototype as unknown as { _getIconUrl?: unknown })._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
  iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
  shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

interface RouteStep {
  instruction: string;
  maneuver: string;
  distanceMeters: number;
  durationSeconds: number;
  latitude?: number;
  longitude?: number;
}

interface RouteResponse {
  totalDistanceMeters: number;
  totalDurationSeconds: number;
  distanceText: string;
  durationText: string;
  polylineCoordinates: { latitude: number; longitude: number }[];
  steps: RouteStep[];
  mode: string;
}

interface InAppNavigationModalProps {
  isOpen: boolean;
  onClose: () => void;
  destination: {
    name: string;
    latitude: number;
    longitude: number;
    address?: string;
    phone?: string;
  };
}

export default function InAppNavigationModal({ isOpen, onClose, destination }: InAppNavigationModalProps) {
  const mapContainerRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const userMarkerRef = useRef<L.Marker | null>(null);
  const destMarkerRef = useRef<L.Marker | null>(null);
  const polylineRef = useRef<L.Polyline | null>(null);

  const [mode, setMode] = useState<"walking" | "driving">("walking");
  const [userLocation, setUserLocation] = useState<{ lat: number; lng: number } | null>(null);
  const [route, setRoute] = useState<RouteResponse | null>(null);
  const [currentStepIndex, setCurrentStepIndex] = useState(0);
  const [remainingDistanceM, setRemainingDistanceM] = useState<number | null>(null);
  const [arrived, setArrived] = useState(false);
  const [gpsError, setGpsError] = useState<string | null>(null);
  const [loadingRoute, setLoadingRoute] = useState(false);

  // Calculate distance between two lat/lng points in meters
  const calcDist = (lat1: number, lon1: number, lat2: number, lon2: number) => {
    const R = 6371e3;
    const φ1 = (lat1 * Math.PI) / 180;
    const φ2 = (lat2 * Math.PI) / 180;
    const Δφ = ((lat2 - lat1) * Math.PI) / 180;
    const Δλ = ((lon2 - lon1) * Math.PI) / 180;
    const a = Math.sin(Δφ / 2) ** 2 + Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) ** 2;
    return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
  };

  // 1. Fetch Route from API
  const fetchRoute = useCallback(
    async (fromLat: number, fromLng: number, selectedMode: "walking" | "driving") => {
      setLoadingRoute(true);
      try {
        const query = `/api/navigation/route?fromLat=${fromLat}&fromLng=${fromLng}&toLat=${destination.latitude}&toLng=${destination.longitude}&mode=${selectedMode}`;
        const data = await apiFetch<RouteResponse>(query);
        setRoute(data);
        setCurrentStepIndex(0);

        if (data.polylineCoordinates && data.polylineCoordinates.length > 0 && mapInstanceRef.current) {
          const latLngs = data.polylineCoordinates.map((p) => [p.latitude, p.longitude] as [number, number]);
          if (polylineRef.current) {
            polylineRef.current.setLatLngs(latLngs);
          } else {
            polylineRef.current = L.polyline(latLngs, {
              color: selectedMode === "walking" ? "#10b981" : "#3b82f6",
              weight: 6,
              opacity: 0.9,
              dashArray: selectedMode === "walking" ? "4, 8" : undefined,
            }).addTo(mapInstanceRef.current);
          }
          mapInstanceRef.current.fitBounds(L.polyline(latLngs).getBounds(), { padding: [50, 50] });
        }
      } catch {
        // Algorithmic straight fallback line if network fails
        if (mapInstanceRef.current) {
          const fallbackLatLngs: [number, number][] = [
            [fromLat, fromLng],
            [destination.latitude, destination.longitude],
          ];
          if (polylineRef.current) {
            polylineRef.current.setLatLngs(fallbackLatLngs);
          } else {
            polylineRef.current = L.polyline(fallbackLatLngs, {
              color: "#10b981",
              weight: 5,
              opacity: 0.8,
            }).addTo(mapInstanceRef.current);
          }
          mapInstanceRef.current.fitBounds(L.polyline(fallbackLatLngs).getBounds(), { padding: [50, 50] });
        }
      } finally {
        setLoadingRoute(false);
      }
    },
    [destination.latitude, destination.longitude]
  );

  // 2. Watch User GPS
  useEffect(() => {
    if (!isOpen) return;

    if (!navigator.geolocation) {
      setGpsError("Geolocation is not supported by your browser.");
      return;
    }

    const watchId = navigator.geolocation.watchPosition(
      (pos) => {
        setGpsError(null);
        const lat = pos.coords.latitude;
        const lng = pos.coords.longitude;
        setUserLocation({ lat, lng });

        // Update remaining distance
        const dist = calcDist(lat, lng, destination.latitude, destination.longitude);
        setRemainingDistanceM(Math.round(dist));

        // Arrival detection: within 25 meters
        if (dist <= 25) {
          setArrived(true);
        }

        // Update user marker
        if (mapInstanceRef.current) {
          const userLatLng = L.latLng(lat, lng);
          if (userMarkerRef.current) {
            userMarkerRef.current.setLatLng(userLatLng);
          } else {
            const userIcon = L.divIcon({
              className: "relative",
              html: `
                <div class="h-6 w-6 rounded-full bg-blue-600 border-2 border-white shadow-lg flex items-center justify-center animate-pulse">
                  <div class="h-2 w-2 rounded-full bg-white"></div>
                </div>
              `,
              iconSize: [24, 24],
              iconAnchor: [12, 12],
            });
            userMarkerRef.current = L.marker(userLatLng, { icon: userIcon }).addTo(mapInstanceRef.current);
          }
        }
      },
      (err) => {
        if (err.code === 1) {
          setGpsError("GPS permission was denied. Please allow location access in your browser.");
        } else {
          setGpsError("Unable to acquire high-accuracy GPS signal.");
        }
      },
      { enableHighAccuracy: true, maximumAge: 3000, timeout: 10000 }
    );

    return () => {
      navigator.geolocation.clearWatch(watchId);
    };
  }, [isOpen, destination.latitude, destination.longitude]);

  // 3. Initialize Map
  useEffect(() => {
    if (!isOpen || !mapContainerRef.current) return;

    if (!mapInstanceRef.current) {
      const map = L.map(mapContainerRef.current, { zoomControl: false }).setView(
        [destination.latitude, destination.longitude],
        15
      );

      L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
        maxZoom: 19,
      }).addTo(map);

      L.control.zoom({ position: "bottomright" }).addTo(map);

      // Destination Shop Pin
      const destIcon = L.divIcon({
        className: "relative",
        html: `
          <div class="flex flex-col items-center">
            <div class="bg-red-600 text-white p-1.5 rounded-full shadow-lg border-2 border-white flex items-center justify-center">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
            </div>
          </div>
        `,
        iconSize: [36, 36],
        iconAnchor: [18, 36],
      });

      destMarkerRef.current = L.marker([destination.latitude, destination.longitude], { icon: destIcon })
        .addTo(map)
        .bindPopup(`<b>${destination.name}</b><br/>${destination.address || ""}`)
        .openPopup();

      mapInstanceRef.current = map;
    }

    // Force map resize
    setTimeout(() => {
      mapInstanceRef.current?.invalidateSize();
    }, 200);

    return () => {
      if (!isOpen && mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
        userMarkerRef.current = null;
        destMarkerRef.current = null;
        polylineRef.current = null;
      }
    };
  }, [isOpen, destination]);

  // 4. Update route when user location or mode changes
  useEffect(() => {
    if (userLocation) {
      fetchRoute(userLocation.lat, userLocation.lng, mode);
    }
  }, [userLocation?.lat, userLocation?.lng, mode, fetchRoute]);

  const reCenter = () => {
    if (userLocation && mapInstanceRef.current) {
      mapInstanceRef.current.setView([userLocation.lat, userLocation.lng], 16, { animate: true });
    } else if (mapInstanceRef.current) {
      mapInstanceRef.current.setView([destination.latitude, destination.longitude], 15, { animate: true });
    }
  };

  if (!isOpen) return null;

  const currentStep = route?.steps && route.steps[currentStepIndex];

  return (
    <div className="fixed inset-0 z-50 flex flex-col bg-slate-900 text-white">
      {/* Top Bar: Next Turn Navigation Banner */}
      <div className="relative z-10 bg-slate-900/95 backdrop-blur border-b border-slate-800 p-4 shadow-xl">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-xl bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-lg border border-emerald-500/30">
              🧭
            </div>
            <div>
              <p className="text-xs font-semibold uppercase tracking-wider text-emerald-400">In-App Live Navigation</p>
              <h2 className="text-base font-extrabold text-white truncate max-w-[200px] sm:max-w-md">
                {destination.name}
              </h2>
            </div>
          </div>
          <button
            onClick={onClose}
            className="rounded-full bg-slate-800 p-2 text-slate-300 hover:text-white hover:bg-slate-700 transition"
          >
            ✕
          </button>
        </div>

        {/* Turn Step Banner */}
        {currentStep ? (
          <div className="mt-3 flex items-center justify-between rounded-xl bg-emerald-950/60 border border-emerald-800/50 p-3">
            <div className="flex items-center gap-3">
              <span className="text-2xl">
                {currentStep.maneuver.includes("right")
                  ? "↱"
                  : currentStep.maneuver.includes("left")
                  ? "↰"
                  : currentStep.maneuver.includes("arrive")
                  ? "🎯"
                  : "↑"}
              </span>
              <div>
                <p className="text-sm font-bold text-white">{currentStep.instruction}</p>
                <p className="text-xs text-emerald-300">
                  Step {currentStepIndex + 1} of {route.steps.length}
                </p>
              </div>
            </div>
            <div className="flex gap-1">
              <button
                disabled={currentStepIndex === 0}
                onClick={() => setCurrentStepIndex((i) => Math.max(0, i - 1))}
                className="rounded bg-slate-800 px-2 py-1 text-xs text-slate-300 disabled:opacity-30"
              >
                ◀
              </button>
              <button
                disabled={currentStepIndex >= route.steps.length - 1}
                onClick={() => setCurrentStepIndex((i) => Math.min(route.steps.length - 1, i + 1))}
                className="rounded bg-slate-800 px-2 py-1 text-xs text-slate-300 disabled:opacity-30"
              >
                ▶
              </button>
            </div>
          </div>
        ) : (
          <div className="mt-2 text-xs text-slate-400">
            {loadingRoute ? "Calculating optimal route…" : "Follow the highlighted path to destination."}
          </div>
        )}

        {/* GPS Warning Banner */}
        {gpsError && (
          <div className="mt-2 rounded-lg bg-amber-500/10 border border-amber-500/30 p-2 text-xs text-amber-300 flex items-center gap-2">
            <span>⚠️</span>
            <span>{gpsError}</span>
          </div>
        )}

        {/* Arrival Celebration Banner */}
        {arrived && (
          <div className="mt-2 rounded-xl bg-emerald-600 border border-emerald-400 p-3 text-center shadow-lg animate-bounce">
            <p className="text-base font-black text-white">🎉 You have arrived at {destination.name}!</p>
            <p className="text-xs text-emerald-100">Within 25 meters of destination.</p>
          </div>
        )}
      </div>

      {/* Map Container */}
      <div className="relative flex-1 w-full bg-slate-950">
        <div ref={mapContainerRef} className="absolute inset-0 h-full w-full" />

        {/* Floating Controls Overlay */}
        <div className="absolute right-4 bottom-24 z-[1000] flex flex-col gap-2">
          <button
            onClick={reCenter}
            className="flex h-11 w-11 items-center justify-center rounded-full bg-slate-900/90 text-white shadow-xl border border-slate-700 hover:bg-slate-800"
            title="Re-center location"
          >
            🎯
          </button>
        </div>
      </div>

      {/* Bottom HUD: ETA, Distance, Modes, Details */}
      <div className="relative z-10 bg-slate-900 border-t border-slate-800 p-4">
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-baseline gap-2">
              <span className="text-2xl font-black text-white">
                {remainingDistanceM !== null
                  ? remainingDistanceM < 1000
                    ? `${remainingDistanceM} m`
                    : `${(remainingDistanceM / 1000).toFixed(1)} km`
                  : route?.distanceText || "..."}
              </span>
              <span className="text-sm font-semibold text-slate-400">
                · {route?.durationText || "ETA calculating…"}
              </span>
            </div>
            <p className="text-xs text-slate-400 truncate max-w-[220px]">
              {destination.address || `${destination.latitude.toFixed(4)}, ${destination.longitude.toFixed(4)}`}
            </p>
          </div>

          {/* Mode Selector */}
          <div className="flex items-center gap-1 rounded-xl bg-slate-800 p-1 border border-slate-700">
            <button
              onClick={() => setMode("walking")}
              className={`rounded-lg px-3 py-1.5 text-xs font-bold transition ${
                mode === "walking" ? "bg-emerald-600 text-white shadow" : "text-slate-400 hover:text-white"
              }`}
            >
              🚶 Walking
            </button>
            <button
              onClick={() => setMode("driving")}
              className={`rounded-lg px-3 py-1.5 text-xs font-bold transition ${
                mode === "driving" ? "bg-blue-600 text-white shadow" : "text-slate-400 hover:text-white"
              }`}
            >
              🚗 Driving
            </button>
          </div>
        </div>

        {destination.phone && (
          <div className="mt-3 flex items-center justify-between border-t border-slate-800/80 pt-2 text-xs">
            <span className="text-slate-400">Need to call shop?</span>
            <a
              href={`tel:${destination.phone}`}
              className="font-bold text-emerald-400 hover:underline flex items-center gap-1"
            >
              📞 {destination.phone}
            </a>
          </div>
        )}
      </div>
    </div>
  );
}
