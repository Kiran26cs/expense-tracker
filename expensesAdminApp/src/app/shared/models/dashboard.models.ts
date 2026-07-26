export interface UserStatsDto {
  total: number;
  newThisMonth: number;
  newThisWeek: number;
  byPlan: { free: number; starter: number; pro: number };
  growthByMonth: { month: string; count: number }[];
}

export interface SubscriptionStatsDto {
  active: number;
  newThisMonth: number;
  cancelledThisMonth: number;
  pendingCancellation: number;
  mrr: {
    starterCount: number; proCount: number;
    starterMrr: number; proMrr: number; total: number;
  };
}

export interface CreditStatsDto {
  consumedThisMonth: number;
  byReason: { aiChat: number; autoClassify: number };
  zeroCreditBooks: { bookId: string; bookName: string; ownerEmail: string; plan: string }[];
}

export interface BookStatsDto {
  total: number;
  templateBooks: number;
  newThisMonth: number;
  aiChatEnabled: number;
}

export interface ImportStatsDto {
  last24h: { completed: number; failed: number; completedWithErrors: number; processing: number; queued: number };
  failedSessions: { id: string; fileName: string; bookId: string; failedAt?: string }[];
}

export interface RecentActionsDto {
  actions: {
    id: string; adminEmail: string; action: string;
    targetType: string; targetId?: string; summary: string; timestamp: string;
  }[];
}
