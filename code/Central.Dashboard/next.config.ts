import type { NextConfig } from "next";

// Single-origin by design (§3, §17): the dashboard calls /api/* and Next proxies to Central.
const apiTarget = process.env.API_PROXY_TARGET ?? "http://localhost:5148";

const nextConfig: NextConfig = {
  async rewrites() {
    return [{ source: "/api/:path*", destination: `${apiTarget}/api/:path*` }];
  }
};

export default nextConfig;
