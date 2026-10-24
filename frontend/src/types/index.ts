export interface ApiResponse<T> {
  isSuccess: boolean;
  data?: T;
  error?: {
    code: string;
    message: string;
  };
  timestamp: string;
}

export interface SystemStatus {
  application: string;
  version: string;
  modules: string[];
  status: string;
  serverTime: string;
}

export interface HealthCheck {
  status: string;
  timestamp: string;
  environment: string;
}
