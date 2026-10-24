export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  isSuperAdmin: boolean;
  restaurantId: string | null;
  restaurantName: string | null;
  roles: string[];
  enabledModules: string[];
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: CurrentUser;
}

export interface RestaurantListItem {
  id: string;
  name: string;
  code: string;
  city: string | null;
  phone: string | null;
  ownerName: string;
  ownerPhone: string;
  isActive: boolean;
  enabledModules: number;
  totalModules: number;
  adminEmail: string | null;
  createdIp: string | null;
  createdAt: string;
}

export interface AdminUser {
  id: string;
  fullName: string;
  email: string;
  phone: string | null;
  isActive: boolean;
}

export interface ModuleGrant {
  code: string;
  name: string;
  description: string;
  isEnabled: boolean;
  sortOrder: number;
}

export interface RestaurantDetail {
  id: string;
  name: string;
  code: string;
  legalName: string | null;
  address: string | null;
  city: string | null;
  state: string | null;
  phone: string | null;
  email: string | null;
  gstin: string | null;
  ownerName: string;
  ownerPhone: string;
  upiId: string | null;
  isActive: boolean;
  createdAt: string;
  createdIp: string | null;
  updatedAt: string | null;
  updatedIp: string | null;
  admin: AdminUser | null;
  modules: ModuleGrant[];
}

export interface AuditEntry {
  id: string;
  action: string;
  entityName: string;
  entityId: string | null;
  description: string;
  actorEmail: string | null;
  restaurantId: string | null;
  restaurantName: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  createdAt: string;
}

export interface PagedAudit {
  items: AuditEntry[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ModuleUsage {
  code: string;
  name: string;
  enabledCount: number;
  restaurantCount: number;
}

export interface Dashboard {
  restaurants: number;
  activeRestaurants: number;
  staff: number;
  auditsToday: number;
  modules: ModuleUsage[];
  recent: AuditEntry[];
}
