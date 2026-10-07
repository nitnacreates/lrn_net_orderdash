"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { login } from "@/lib/api";

export default function LoginPage() {
  const router = useRouter();
  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (await login(username, password)) {
      router.replace("/");
    } else {
      setError("Invalid credentials.");
    }
  }

  return (
    <div className="container" style={{ maxWidth: 380, marginTop: "10vh" }}>
      <div className="card shadow-sm">
        <div className="card-body">
          <h1 className="h4 mb-1">OrderDash</h1>
          <p className="text-muted mb-4">Central — sign in</p>
          <form onSubmit={onSubmit}>
            <div className="mb-3">
              <label className="form-label">Username</label>
              <input
                className="form-control"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
              />
            </div>
            <div className="mb-3">
              <label className="form-label">Password</label>
              <input
                type="password"
                className="form-control"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </div>
            {error && <div className="alert alert-danger py-2">{error}</div>}
            <button className="btn btn-primary w-100" type="submit">
              Sign in
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
