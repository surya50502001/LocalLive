export interface Coords { latitude: number; longitude: number; }

export function formatDistance(meters: number | null | undefined): string {
  if (meters == null) return "";
  if (meters < 1000) return `${Math.round(meters)} m`;
  return `${(meters / 1000).toFixed(1)} km`;
}

export function haversineMeters(a: Coords, b: Coords): number {
  const R = 6371000;
  const dLat = ((b.latitude - a.latitude) * Math.PI) / 180;
  const dLon = ((b.longitude - a.longitude) * Math.PI) / 180;
  const sLat = Math.sin(dLat / 2);
  const sLon = Math.sin(dLon / 2);
  const h = sLat * sLat + Math.cos((a.latitude * Math.PI) / 180) * Math.cos((b.latitude * Math.PI) / 180) * sLon * sLon;
  return R * 2 * Math.atan2(Math.sqrt(h), Math.sqrt(1 - h));
}

export function getCurrentPosition(): Promise<Coords> {
  return new Promise((resolve) => {
    // Default fallback coordinates (city center) if geolocation is restricted or blocked on HTTP
    const fallback: Coords = { latitude: 11.0294, longitude: 76.9675 };

    if (!navigator.geolocation || !window.isSecureContext && window.location.hostname !== "localhost") {
      resolve(fallback);
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => resolve({ latitude: pos.coords.latitude, longitude: pos.coords.longitude }),
      () => resolve(fallback),
      { enableHighAccuracy: false, timeout: 5000, maximumAge: 60000 },
    );
  });
}
