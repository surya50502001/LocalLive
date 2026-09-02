export interface CommunitySuggestion {
  id: string;
  requestId: string;
  userName: string;
  placeName: string;
  note?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  googleMapsUrl: string;
  googleEmbedUrl?: string;
  createdAt: string;
  upvotes: number;
}

const STORAGE_KEY = "locallive_community_suggestions";

// Preset initial suggestions so the pool is lively out of the box
const DEFAULT_SUGGESTIONS: CommunitySuggestion[] = [
  {
    id: "sug-101",
    requestId: "demo-req-1",
    userName: "Alex M. (0.3km away)",
    placeName: "Apex Electronics & Supplies",
    note: "They have all Type-C 65W chargers in stock right now!",
    address: "102 Cross Road, City Center",
    googleMapsUrl: "https://www.google.com/maps/search/?api=1&query=Apex+Electronics+Supplies",
    createdAt: new Date(Date.now() - 5 * 60000).toISOString(),
    upvotes: 4,
  },
  {
    id: "sug-102",
    requestId: "demo-req-2",
    userName: "Priya R. (0.7km away)",
    placeName: "Urban Bakery & Café",
    note: "Fresh chocolate cakes available until 10 PM!",
    address: "45 Bakery Lane, Near Park",
    googleMapsUrl: "https://www.google.com/maps/search/?api=1&query=Urban+Bakery+Cafe",
    createdAt: new Date(Date.now() - 12 * 60000).toISOString(),
    upvotes: 7,
  },
];

export function getSuggestionsForRequest(requestId: string): CommunitySuggestion[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const list: CommunitySuggestion[] = raw ? JSON.parse(raw) : DEFAULT_SUGGESTIONS;
    return list.filter((s) => s.requestId === requestId || requestId === "all");
  } catch {
    return DEFAULT_SUGGESTIONS;
  }
}

export function getAllSuggestions(): CommunitySuggestion[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : DEFAULT_SUGGESTIONS;
  } catch {
    return DEFAULT_SUGGESTIONS;
  }
}

export function addCommunitySuggestion(
  requestId: string,
  placeName: string,
  note?: string,
  address?: string,
  userName = "Nearby Neighbor"
): CommunitySuggestion {
  const query = encodeURIComponent(`${placeName} ${address || ""}`.trim());
  const mapsUrl = `https://www.google.com/maps/search/?api=1&query=${query}`;
  const embedUrl = `https://maps.google.com/maps?q=${query}&t=&z=15&ie=UTF8&iwloc=&output=embed`;

  const newSug: CommunitySuggestion = {
    id: `sug-${Date.now()}`,
    requestId,
    userName,
    placeName: placeName.trim(),
    note: note?.trim() || undefined,
    address: address?.trim() || undefined,
    googleMapsUrl: mapsUrl,
    googleEmbedUrl: embedUrl,
    createdAt: new Date().toISOString(),
    upvotes: 1,
  };

  const all = getAllSuggestions();
  const updated = [newSug, ...all];
  localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
  return newSug;
}

export function upvoteSuggestion(id: string): void {
  const all = getAllSuggestions();
  const updated = all.map((s) => (s.id === id ? { ...s, upvotes: s.upvotes + 1 } : s));
  localStorage.setItem(STORAGE_KEY, JSON.stringify(updated));
}
