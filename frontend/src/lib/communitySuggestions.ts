export interface CommunitySuggestion {
  id: string;
  requestId: string;
  userName: string;
  placeName: string;
  note?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  createdAt: string;
  upvotes: number;
}

const STORAGE_KEY = "locallive_community_suggestions";

export function getSuggestionsForRequest(requestId: string): CommunitySuggestion[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    const list: CommunitySuggestion[] = raw ? JSON.parse(raw) : [];
    return list.filter((s) => s.requestId === requestId || requestId === "all");
  } catch {
    return [];
  }
}

export function getAllSuggestions(): CommunitySuggestion[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
}

export function addCommunitySuggestion(
  requestId: string,
  placeName: string,
  note?: string,
  address?: string,
  userName = "Nearby User"
): CommunitySuggestion {
  const newSug: CommunitySuggestion = {
    id: `sug-${Date.now()}`,
    requestId,
    userName,
    placeName: placeName.trim(),
    note: note?.trim() || undefined,
    address: address?.trim() || undefined,
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
