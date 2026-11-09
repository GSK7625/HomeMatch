import { API_BASE, getStoredToken } from "@/lib/api-client";
import { MessageDto } from "@/types";

const RECORD_SEPARATOR = "\x1e";

export type RealtimeConnectionState = "disconnected" | "connecting" | "connected" | "reconnecting";

export interface ConversationUpdatedPayload {
  conversationId: number;
  lastMessageContent: string;
  lastMessageSenderId: number;
  lastMessageAt: string;
}

export interface MessagesReadPayload {
  conversationId: number;
  readerUserId: number;
  markedReadCount: number;
  readAt: string;
}

export interface UserTypingPayload {
  conversationId: number;
  userId: number;
  isTyping: boolean;
  timestamp: string;
}

export interface ChatRealtimeHandlers {
  onStateChange?: (state: RealtimeConnectionState) => void;
  onReceiveMessage?: (message: MessageDto) => void;
  onConversationUpdated?: (payload: ConversationUpdatedPayload) => void;
  onMessagesRead?: (payload: MessagesReadPayload) => void;
  onUserTyping?: (payload: UserTypingPayload) => void;
}

function getWebSocketHubUrl(accessToken: string): string {
  let baseUrl = API_BASE;
  if (!baseUrl && typeof window !== "undefined") {
    baseUrl = window.location.origin;
  }
  const wsBase = baseUrl.replace(/^http:/i, "ws:").replace(/^https:/i, "wss:");
  return `${wsBase}/api/v1/messaging/hub?access_token=${encodeURIComponent(accessToken)}`;
}

/**
 * Lightweight native ASP.NET Core SignalR JSON Protocol WebSocket client
 * Speaks {"protocol":"json","version":1}\x1e directly without external package bloat.
 */
export class ChatRealtimeClient {
  private ws: WebSocket | null = null;
  private handlers: ChatRealtimeHandlers;
  private state: RealtimeConnectionState = "disconnected";
  private shouldReconnect = false;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private activeConversationId: number | null = null;
  private invocationId = 0;

  constructor(handlers: ChatRealtimeHandlers) {
    this.handlers = handlers;
  }

  public updateHandlers(handlers: ChatRealtimeHandlers) {
    this.handlers = handlers;
  }

  public start(): void {
    const token = getStoredToken();
    if (!token || typeof window === "undefined") {
      this.setState("disconnected");
      return;
    }

    this.shouldReconnect = true;
    this.connectInternal(token);
  }

  public stop(): void {
    this.shouldReconnect = false;
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    if (this.ws) {
      try {
        this.ws.close();
      } catch {
        // Ignore close error
      }
      this.ws = null;
    }
    this.setState("disconnected");
  }

  public joinConversation(conversationId: number): void {
    if (this.activeConversationId && this.activeConversationId !== conversationId) {
      this.invokeHubMethod("LeaveConversation", [this.activeConversationId]);
    }
    this.activeConversationId = conversationId;
    if (this.state === "connected") {
      this.invokeHubMethod("JoinConversation", [conversationId]);
    }
  }

  public leaveConversation(conversationId: number): void {
    if (this.activeConversationId === conversationId) {
      this.activeConversationId = null;
    }
    if (this.state === "connected") {
      this.invokeHubMethod("LeaveConversation", [conversationId]);
    }
  }

  public sendTypingIndicator(conversationId: number, isTyping: boolean): void {
    if (this.state === "connected") {
      this.invokeHubMethod("SendTypingIndicator", [conversationId, isTyping]);
    }
  }

  private connectInternal(token: string): void {
    if (this.ws && (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING)) {
      return;
    }

    this.setState(this.state === "reconnecting" ? "reconnecting" : "connecting");

    try {
      const wsUrl = getWebSocketHubUrl(token);
      const socket = new WebSocket(wsUrl);
      this.ws = socket;

      socket.onopen = () => {
        // Send SignalR JSON protocol v1 handshake frame
        this.sendRaw(`${JSON.stringify({ protocol: "json", version: 1 })}${RECORD_SEPARATOR}`);
      };

      socket.onmessage = (event) => {
        if (typeof event.data !== "string") return;
        const frames = event.data.split(RECORD_SEPARATOR).filter((f) => f.trim().length > 0);

        for (const rawFrame of frames) {
          try {
            const msg = JSON.parse(rawFrame);

            // Empty object {} is the SignalR handshake response
            if (Object.keys(msg).length === 0 || (!msg.type && !msg.error)) {
              this.setState("connected");
              if (this.activeConversationId) {
                this.invokeHubMethod("JoinConversation", [this.activeConversationId]);
              }
              continue;
            }

            // Type 6 = Ping -> Reply with Type 6 Ping
            if (msg.type === 6) {
              this.sendRaw(`${JSON.stringify({ type: 6 })}${RECORD_SEPARATOR}`);
              continue;
            }

            // Type 1 = Invocation from Server Hub
            if (msg.type === 1 && typeof msg.target === "string") {
              const arg = Array.isArray(msg.arguments) ? msg.arguments[0] : undefined;
              switch (msg.target) {
                case "ReceiveMessage":
                  if (arg) this.handlers.onReceiveMessage?.(arg as MessageDto);
                  break;
                case "ConversationUpdated":
                  if (arg) this.handlers.onConversationUpdated?.(arg as ConversationUpdatedPayload);
                  break;
                case "MessagesRead":
                  if (arg) this.handlers.onMessagesRead?.(arg as MessagesReadPayload);
                  break;
                case "UserTyping":
                  if (arg) this.handlers.onUserTyping?.(arg as UserTypingPayload);
                  break;
              }
            }
          } catch {
            // Ignore malformed frame
          }
        }
      };

      socket.onclose = () => {
        this.ws = null;
        if (this.shouldReconnect) {
          this.setState("reconnecting");
          this.reconnectTimer = setTimeout(() => {
            const latestToken = getStoredToken();
            if (latestToken && this.shouldReconnect) {
              this.connectInternal(latestToken);
            } else {
              this.setState("disconnected");
            }
          }, 3000);
        } else {
          this.setState("disconnected");
        }
      };

      socket.onerror = () => {
        // Error will be followed by onclose
      };
    } catch {
      this.setState("disconnected");
    }
  }

  private invokeHubMethod(target: string, args: unknown[]): void {
    this.invocationId += 1;
    const payload = {
      type: 1,
      invocationId: String(this.invocationId),
      target,
      arguments: args,
    };
    this.sendRaw(`${JSON.stringify(payload)}${RECORD_SEPARATOR}`);
  }

  private sendRaw(data: string): void {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      try {
        this.ws.send(data);
      } catch {
        // Ignore send error
      }
    }
  }

  private setState(next: RealtimeConnectionState): void {
    this.state = next;
    this.handlers.onStateChange?.(next);
  }
}
