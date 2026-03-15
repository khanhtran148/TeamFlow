import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
  HttpTransportType,
} from "@microsoft/signalr";
import { useAuthStore } from "@/lib/stores/auth-store";

const SIGNALR_URL =
  process.env.NEXT_PUBLIC_SIGNALR_URL ?? "http://localhost:5000/hubs/teamflow";

/**
 * Creates and returns a configured HubConnection.
 * - Automatic reconnect with exponential backoff (0, 2, 10, 30s, then 30s intervals)
 * - Falls back through WebSockets, SSE, Long-Polling
 * - JWT token passed via query string for authentication
 */
export function createHubConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(SIGNALR_URL, {
      transport:
        HttpTransportType.WebSockets |
        HttpTransportType.ServerSentEvents |
        HttpTransportType.LongPolling,
      accessTokenFactory: () => {
        return useAuthStore.getState().accessToken ?? "";
      },
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000, 30000, 30000])
    .configureLogging(
      process.env.NODE_ENV === "development"
        ? LogLevel.Information
        : LogLevel.Warning,
    )
    .build();
}
