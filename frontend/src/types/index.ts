export type Role = "Customer" | "ShopOwner" | "Admin";

export interface UserDto {
  id: string;
  email: string;
  fullName: string;
  phone?: string | null;
  role: string;
  isVerified: boolean;
}

export interface TokenPairDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface AuthResultDto {
  user: UserDto;
  tokens: TokenPairDto;
}

export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  icon?: string | null;
  sortOrder: number;
}

export interface ShopDto {
  id: string;
  name: string;
  description?: string | null;
  phone: string;
  address: string;
  latitude: number;
  longitude: number;
  imageUrl?: string | null;
  isOpen: boolean;
  status: string;
  isVerified: boolean;
  ownerUserId: string;
  categories: { id: string; name: string; slug: string }[];
  distanceM?: number | null;
  navigationUrl?: string | null;
}

export interface RequestDto {
  id: string;
  title: string;
  description?: string | null;
  categoryId: string;
  categoryName: string;
  status: string;
  expiresAt: string;
  createdAt: string;
  latitude: number;
  longitude: number;
  notifiedShopsCount: number;
  availableShops: ShopAvailableDto[];
  distanceM?: number | null;
}

export interface ShopAvailableDto {
  shopId: string;
  shopName: string;
  description?: string | null;
  address: string;
  phone: string;
  distanceM?: number | null;
  isVerified: boolean;
  navigationUrl?: string | null;
  message?: string | null;
  respondedAt: string;
}

export interface AdminStatsDto {
  totalUsers: number;
  totalCustomers: number;
  totalShopOwners: number;
  totalShops: number;
  verifiedShops: number;
  pendingShops: number;
  disabledShops: number;
  activeShopsNow: number;
  totalRequests: number;
  activeRequestsNow: number;
  fulfilledRequests: number;
  cancelledRequests: number;
  expiredRequests: number;
  totalResponses: number;
  avgDistanceToRespondingShopM: number;
  openReports: number;
  requestsByCategory: { categoryName: string; count: number }[];
  requestsLast7Days: { day: string; requests: number; responses: number; fulfilled: number }[];
}
