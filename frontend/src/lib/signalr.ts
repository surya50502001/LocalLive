import * as signalR from "@microsoft/signalr";

let connection: signalR.HubConnection | null = null;

export function getSignalRConnection(): signalR.HubConnection | null {
  return connection;
}

export async function connectSignalR(): Promise<signalR.HubConnection> {
  if (connection && connection.state === signalR.HubConnectionState.Connected) return connection;
  if (connection) {
    try { await connection.stop(); } catch { /* ignore */ }
    connection = null;
  }
  const token = localStorage.getItem("accessToken");
  const base = import.meta.env.VITE_API_URL ?? "";
  const url = `${base}/hubs/live`;
  connection = new signalR.HubConnectionBuilder()
    .withUrl(url, {
      accessTokenFactory: () => token ?? "",
      withCredentials: false,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();
  await connection.start();
  return connection;
}

export async function disconnectSignalR(): Promise<void> {
  if (!connection) return;
  try { await connection.stop(); } catch { /* ignore */ }
  connection = null;
}

export async function joinShopGroup(shopId: string): Promise<void> {
  if (!connection) return;
  await connection.invoke("JoinShop", shopId);
}
export async function leaveShopGroup(shopId: string): Promise<void> {
  if (!connection) return;
  try { await connection.invoke("LeaveShop", shopId); } catch { /* ignore */ }
}
