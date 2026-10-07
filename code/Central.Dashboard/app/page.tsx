"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { apiGet, apiPost, clearToken, getToken } from "@/lib/api";

type Channel = {
  key: string;
  name: string;
  type: string;
  scheduleMinutes: number;
  isActive: boolean;
  lastSyncAt: string | null;
  status: string;
};

type Order = {
  channelKey: string;
  orderNumber: string;
  orderDate: string;
  status: string;
  receivedAt: string;
  lines: unknown[];
};

type Log = {
  id: number;
  channelKey: string;
  direction: string;
  level: string;
  message: string;
  createdAt: string;
};

type Stats = {
  newToday: number;
  pendingImport: number;
  imported: number;
  dispatched: number;
  errors: number;
  channels: number;
};

const STATUS_BADGE: Record<string, string> = {
  Received: "secondary",
  Imported: "info",
  DispatchSent: "success",
  Error: "danger"
};

const DOT: Record<string, string> = { green: "success", amber: "warning", red: "danger" };

function fmt(value: string | null) {
  return value ? new Date(value).toLocaleString() : "—";
}

export default function DashboardPage() {
  const router = useRouter();
  const [stats, setStats] = useState<Stats | null>(null);
  const [channels, setChannels] = useState<Channel[]>([]);
  const [orders, setOrders] = useState<Order[]>([]);
  const [logs, setLogs] = useState<Log[]>([]);
  const [channelFilter, setChannelFilter] = useState("");
  const [statusFilter, setStatusFilter] = useState("");
  const [error, setError] = useState("");

  const loadOrders = useCallback(async () => {
    const params = new URLSearchParams();
    if (channelFilter) params.set("channel", channelFilter);
    if (statusFilter) params.set("status", statusFilter);
    setOrders(await apiGet<Order[]>(`/api/orders?${params.toString()}`));
  }, [channelFilter, statusFilter]);

  const load = useCallback(async () => {
    try {
      const [s, c, l] = await Promise.all([
        apiGet<Stats>("/api/stats"),
        apiGet<Channel[]>("/api/channels"),
        apiGet<Log[]>("/api/logs")
      ]);
      setStats(s);
      setChannels(c);
      setLogs(l);
      setError("");
    } catch (e) {
      if (String(e).includes("401")) {
        clearToken();
        router.replace("/login");
        return;
      }
      setError("Failed to load dashboard data.");
    }
  }, [router]);

  useEffect(() => {
    if (!getToken()) {
      router.replace("/login");
      return;
    }
    load();
  }, [load, router]);

  useEffect(() => {
    if (getToken()) loadOrders().catch(() => setError("Failed to load orders."));
  }, [loadOrders]);

  async function replay(order: Order) {
    await apiPost(`/api/orders/${order.channelKey}/${order.orderNumber}/replay`);
    await Promise.all([load(), loadOrders()]);
  }

  function signOut() {
    clearToken();
    router.replace("/login");
  }

  return (
    <div className="container py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h3 mb-0">OrderDash — Central</h1>
        <button className="btn btn-outline-secondary btn-sm" onClick={signOut}>
          Sign out
        </button>
      </div>

      {error && <div className="alert alert-danger py-2">{error}</div>}

      <div className="row g-3 mb-4">
        {[
          { label: "New today", value: stats?.newToday, color: "primary" },
          { label: "Pending import", value: stats?.pendingImport, color: "secondary" },
          { label: "Imported", value: stats?.imported, color: "info" },
          { label: "Dispatched", value: stats?.dispatched, color: "success" },
          { label: "Errors", value: stats?.errors, color: "danger" }
        ].map((c) => (
          <div className="col" key={c.label}>
            <div className={`card border-${c.color} h-100`}>
              <div className="card-body py-3">
                <div className="text-muted small">{c.label}</div>
                <div className={`h2 mb-0 text-${c.color}`}>{c.value ?? "—"}</div>
              </div>
            </div>
          </div>
        ))}
      </div>

      <h2 className="h5">Channels</h2>
      <table className="table table-sm bg-white align-middle">
        <thead>
          <tr>
            <th>Name</th>
            <th>Type</th>
            <th>Every</th>
            <th>Last sync</th>
            <th>Status</th>
          </tr>
        </thead>
        <tbody>
          {channels.map((ch) => (
            <tr key={ch.key}>
              <td>{ch.name}</td>
              <td>
                <code>{ch.type}</code>
              </td>
              <td>{ch.scheduleMinutes} min</td>
              <td>{fmt(ch.lastSyncAt)}</td>
              <td>
                <span className={`badge bg-${DOT[ch.status] ?? "secondary"}`}>{ch.status}</span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <h2 className="h5 mt-4">Orders</h2>
      <div className="row g-2 mb-2">
        <div className="col-auto">
          <select
            className="form-select form-select-sm"
            value={channelFilter}
            onChange={(e) => setChannelFilter(e.target.value)}
          >
            <option value="">All channels</option>
            {channels.map((ch) => (
              <option key={ch.key} value={ch.key}>
                {ch.name}
              </option>
            ))}
          </select>
        </div>
        <div className="col-auto">
          <select
            className="form-select form-select-sm"
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
          >
            <option value="">All statuses</option>
            {Object.keys(STATUS_BADGE).map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </div>
      </div>
      <table className="table table-sm bg-white align-middle">
        <thead>
          <tr>
            <th>Order</th>
            <th>Channel</th>
            <th>Date</th>
            <th>Status</th>
            <th>Received</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {orders.map((o) => (
            <tr key={`${o.channelKey}-${o.orderNumber}`}>
              <td>
                <code>{o.orderNumber}</code>
              </td>
              <td>{o.channelKey}</td>
              <td>{o.orderDate}</td>
              <td>
                <span className={`badge bg-${STATUS_BADGE[o.status] ?? "secondary"}`}>{o.status}</span>
              </td>
              <td>{fmt(o.receivedAt)}</td>
              <td className="text-end">
                {o.status === "Error" && (
                  <button className="btn btn-outline-danger btn-sm" onClick={() => replay(o)}>
                    Replay
                  </button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>

      <h2 className="h5 mt-4">Logs</h2>
      <table className="table table-sm bg-white">
        <thead>
          <tr>
            <th>When</th>
            <th>Channel</th>
            <th>Direction</th>
            <th>Level</th>
            <th>Message</th>
          </tr>
        </thead>
        <tbody>
          {logs.map((l) => (
            <tr key={l.id}>
              <td>{fmt(l.createdAt)}</td>
              <td>{l.channelKey}</td>
              <td>{l.direction}</td>
              <td>
                <span className={`badge bg-${l.level === "error" ? "danger" : "light text-dark"}`}>
                  {l.level}
                </span>
              </td>
              <td>{l.message}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
