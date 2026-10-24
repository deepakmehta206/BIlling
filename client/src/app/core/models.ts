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

export interface NamedPrice {
  id?: string | null;
  name: string;
  price: number;
}

export interface MenuCategory {
  id: string;
  name: string;
  sortOrder: number;
  isActive: boolean;
  itemCount: number;
}

export interface MenuItem {
  id: string;
  categoryId: string;
  categoryName: string;
  name: string;
  description: string | null;
  foodType: string;
  price: number;
  isAvailable: boolean;
  sortOrder: number;
  variants: NamedPrice[];
  addons: NamedPrice[];
}

export interface MenuBoard {
  categories: MenuCategory[];
  items: MenuItem[];
}

export interface FloorTable {
  id: string;
  name: string;
  section: string;
  capacity: number;
  posX: number;
  posY: number;
  status: string;
  qrToken: string;
  createdIp: string | null;
  booking: { id: string; guestName: string; startsAt: string; endsAt: string } | null;
  openToken: number | null;
}

export interface Booking {
  id: string;
  tableId: string;
  tableName: string;
  guestName: string;
  guestPhone: string;
  guestCount: number;
  startsAt: string;
  endsAt: string;
  status: string;
  notes: string | null;
}

export interface TicketLine {
  itemName: string;
  variantName: string | null;
  addonSummary: string | null;
  quantity: number;
  unitPrice: number;
}

export interface Ticket {
  id: string;
  tokenNumber: number;
  status: string;
  tableName: string | null;
  guestName: string | null;
  createdAt: string;
  lines: TicketLine[];
}

export interface PublicDish {
  id: string;
  name: string;
  description: string | null;
  foodType: string;
  price: number;
  variants: NamedPrice[];
  addons: NamedPrice[];
}

export interface PublicTable {
  restaurantId: string;
  restaurantName: string;
  restaurantCode: string;
  tableName: string;
  menu: { name: string; items: PublicDish[] }[];
}

export interface PublicOrder {
  id: string;
  restaurantId: string;
  restaurantName: string;
  tableName: string | null;
  tokenNumber: number;
  status: string;
  lines: TicketLine[];
}

export interface TokenBoard {
  restaurantId: string;
  restaurantName: string;
  code: string;
  tickets: Ticket[];
}

export interface BillLine {
  itemName: string;
  variantName: string | null;
  quantity: number;
  unitPrice: number;
}

export interface OpenTicket {
  orderId: string;
  tokenNumber: number;
  status: string;
  tableName: string | null;
  guestName: string | null;
  subtotal: number;
  lines: BillLine[];
}

export interface HouseBill {
  id: string;
  restaurantId: string;
  billNumber: number;
  tableName: string | null;
  guestName: string | null;
  subtotal: number;
  gstPercent: number;
  gstAmount: number;
  discount: number;
  total: number;
  status: string;
  paymentMode: string;
  upiUri: string | null;
  qrPng: string | null;
  whatsAppStatus: string | null;
  whatsAppUrl: string | null;
  lines: BillLine[];
}

export interface BillingDesk {
  tickets: OpenTicket[];
  bills: HouseBill[];
}

export interface PublicBill {
  id: string;
  restaurantId: string;
  restaurantName: string;
  billNumber: number;
  tableName: string | null;
  guestName: string | null;
  total: number;
  status: string;
  upiUri: string | null;
  qrPng: string | null;
  lines: BillLine[];
}
