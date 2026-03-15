// ============================================================
// SignalR Hub connection factory
// Endpoint: NEXT_PUBLIC_SIGNALR_URL (default: http://localhost:5000/hubs/teamflow)
//
// KNOWN ISSUE (Phase 1): The backend TeamFlowHub has [Authorize].
// Anonymous connections will be rejected with 401 until the backend
// removes [Authorize] from TeamFlowHub (one-line change needed — see
// src/apps/TeamFlow.Api/Hubs/TeamFlowHub.cs). This is documented as
// a required backend change for Phase 1. Phase 2 will re-add auth via JWT.
// ============================================================

import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
  HttpTransportType,
} from "@microsoft/signalr";

const SIGNALR_URL =
  process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5000/hubs/teamflow";

/**
 * Creates and returns a configured HubConnection.
 * - Automatic reconnect with exponential backoff (0, 2, 10, 30s, then 30s intervals)
 * - Falls back through WebSockets, SSE, Long-Polling
 * - No JWT in Phase 1 (backend must allow anonymous)
 */
export function createHubConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
      transport:
        HttpTransportType.WebSockets |
        HttpTransportType.ServerSentEvents |
        HttpTransportType.LongPolling,
      // Phase 1: no auth. Phase 2: inject accessTokenFactory here.
      // accessTokenFactory: () => getToken(),
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000, 30000, 30000])
    .configureLogging(
      process.env.NODE_ENV === "development" ? LogLevel.Information : LogLevel.Warning,
    )
    .build();
}
