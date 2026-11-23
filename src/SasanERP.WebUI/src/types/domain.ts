export type DeviceType = 
  | 'Smartphone'
  | 'FeaturePhone'
  | 'Tablet'
  | 'Laptop'
  | 'Smartwatch'
  | 'Audio'
  | 'Accessory'
  | 'Drone'
  | 'GamingConsole'
  | 'Other';

export type ConditionGrade = 
  | 'BrandNew'
  | 'LikeNew'
  | 'GradeA_Excellent'
  | 'GradeB_Good'
  | 'GradeC_Fair'
  | 'Refurbished';

export type PaymentMode = 
  | 'Cash'
  | 'UPI'
  | 'Card'
  | 'BankTransfer'
  | 'OldPhoneExchange'
  | 'BajajFinservEMI'
  | 'CreditKhata'
  | 'Cheque';

export type InvoiceTemplateStyle =
  | 'LuxuryGadgetLounge'
  | 'Rule32_5_MarginSchemeAudit'
  | 'DarkCyberLounge'
  | 'ModernNeoBrutalist'
  | 'ClassicCorporateGst'
  | 'ThermalRoll_80mm'
  | 'EcoDigitalWhatsApp'
  | 'DeviceHealthCertificate'
  | 'DualCopyCounterfoil'
  | 'CompactTechDocket'
  | 'EliteBoutiqueWatermark'
  | 'ExecutiveMinimalist';

export type BarcodeLabelStyle =
  | 'Flagship_50x30mm'
  | 'CompactAccessory_50x25mm'
  | 'DumbbellJewelryTag'
  | 'TamperWarrantyVoidSeal_25mm'
  | 'DualQrTechExplorer'
  | 'MultiSticker_A4Sheet'
  | 'MetallicWeatherproof';

export interface Product {
  id: number;
  name: string;
  brand: string;
  category: DeviceType;
  sku: string;
  barcode?: string;
  hsnCode: string;
  isSerialized: boolean;
  defaultCostPrice: number;
  mrp: number;
  sellingPrice: number;
  currentStock: number;
  reorderLevel: number;
}

export interface SerializedUnit {
  id: number;
  productId: number;
  productName: string;
  imei1: string;
  imei2?: string;
  serialNumber?: string;
  color?: string;
  storageCapacity?: string;
  condition: string;
  batteryHealthPercentage?: number;
  purchaseCost: number;
  sellingPrice: number;
  agingDays: number;
  status: string;
}

export interface Customer {
  id: number;
  fullName: string;
  phoneNumber: string;
  email?: string;
  gstin?: string;
  address?: string;
  outstandingCredit: number;
}

export interface DashboardTelemetry {
  timeframe: string;
  todayRevenue: number;
  netGrossProfit: number;
  totalStockValuation: number;
  khataReceivables: number;
  cashInDrawer: number;
  upiBankInflow: number;
  agingDevicesCount: number;
  lowStockAlertsCount: number;
  agingDevices: Array<{
    id: number;
    deviceName: string;
    imei1: string;
    serialNumber?: string;
    conditionGrade: string;
    agingDays: number;
    purchaseCost: number;
    taggedPrice: number;
  }>;
  lowStockItems: Array<{
    id: number;
    name: string;
    sku: string;
    category: string;
    currentStock: number;
    reorderLevel: number;
    sellingPrice: number;
  }>;
  recentInvoices: Array<{
    id: number;
    invoiceNumber: string;
    customerName: string;
    invoiceDate: string;
    grandTotal: number;
    paymentModeSummary: string;
    isVoided: boolean;
  }>;
  status: string;
}

export type AgingDevice = DashboardTelemetry['agingDevices'][number];
export type RecentInvoice = DashboardTelemetry['recentInvoices'][number];
export type LowStockItem = DashboardTelemetry['lowStockItems'][number];

export interface CompanyProfile {
  businessName: string;
  tagline: string;
  addressLine1: string;
  addressLine2: string;
  city: string;
  state: string;
  pincode: string;
  phoneNumber: string;
  email?: string;
  gstin?: string;
  bankName?: string;
  bankAccountNumber?: string;
  bankIFSC?: string;
  bankBranch?: string;
  upiId?: string;
  termsAndConditions?: string;
  isMonochromePrintMode: boolean;
}
