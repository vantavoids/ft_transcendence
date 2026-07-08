import { UserDataExportResponse } from './users.types';

export type DataExportStatus = 'pending' | 'ready' | 'failed';

export interface CreateDataExportResponse {
  export_id: string;
  status: 'pending';
}

export interface DataExportStatusResponse {
  export_id: string;
  status: DataExportStatus;
  download_url?: string;
  expires_at?: string;
  error?: string;
}

export interface DataExportReadyEvent {
  user_id: string;
  email: string;
  download_url: string;
  expires_at: string;
}

export interface DataExportBundle {
  export_id: string;
  user_id: string;
  generated_at: string;
  services: {
    auth?: unknown;
    user?: UserDataExportResponse;
    guild?: unknown;
    chat?: unknown;
    notification?: unknown;
  };
  errors?: Record<string, string>;
}
