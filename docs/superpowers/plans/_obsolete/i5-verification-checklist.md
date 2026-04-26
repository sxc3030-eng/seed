# i5 Box Verification Checklist

**Run this BEFORE Task 2 of `2026-04-26-seed-genia-i5-integration-plan.md`.**

The i5 box is bui1's Intel-i5 machine that will host the `Seed.Server` ASP.NET API and the `.dna` file storage. This checklist confirms the box is reachable, has the .NET runtime, and is ready to host an HTTPS endpoint.

If any item is **RED**, the corresponding fix must happen before the integration plan can proceed.

---

## A. Identity & Access

- [ ] **A1.** Confirm the i5 box's role
  - What is it? (PC perso, NAS Synology, mini-PC, VPS Intel-based, ...?)
  - Always-on? (24/7 uptime expected for a backend)
  - Power policy? (sleep on inactivity = downtime risk)

- [ ] **A2.** OS confirmed
  - Windows 11 / Windows 10 / Windows Server / Linux / WSL ?
  - Adapt deploy steps in plan accordingly (NSSM is Windows-only)

- [ ] **A3.** Remote access works
  - SSH / RDP / Tailscale / direct LAN ?
  - bui1 can run admin commands without physical access ?

- [ ] **A4.** Disk free space at intended storage path
  - At least **5 GB** free at `C:\seed-data\` (or equivalent on Linux)
  - Each `.dna` is small (<10 KB) but multi-user volume could grow

---

## B. .NET Runtime & Build Tools

- [ ] **B1.** .NET 8 SDK or runtime present
  - Run on i5: `dotnet --info`
  - Expect : `.NET SDK ... Version: 8.0.x` OR at minimum `.NET Runtimes Microsoft.AspNetCore.App 8.0.x`
  - If absent : install from https://dotnet.microsoft.com/download/dotnet/8.0

- [ ] **B2.** ASP.NET Core 8 hosting bundle (Windows only)
  - For IIS reverse-proxy scenario, install ASP.NET Core Module v2 hosting bundle
  - Skip if using direct Kestrel (recommended for simplicity)

---

## C. Network Reachability

Pick **one** of the three deployment options below. Mark its sub-items green; ignore the others.

### C-Option-1 : Direct port-forward + Let's Encrypt (most invasive)

- [ ] **C1a.** Static or dynamic public IP confirmed
- [ ] **C1b.** Router admin access confirmed (port 443 forward to i5:5001)
- [ ] **C1c.** Domain owned and DNS A record points to public IP
- [ ] **C1d.** Caddy or Nginx installed on i5 for TLS termination
- [ ] **C1e.** Let's Encrypt cert obtained for `seed.<bui1-domain>`

### C-Option-2 : Cloudflare Tunnel (recommended)

- [ ] **C2a.** Cloudflare account exists
- [ ] **C2b.** Domain managed by Cloudflare
- [ ] **C2c.** `cloudflared` binary downloaded on i5
- [ ] **C2d.** Tunnel created : `cloudflared tunnel create seed-i5`
- [ ] **C2e.** DNS routed : `cloudflared tunnel route dns seed-i5 seed.<bui1-domain>`
- [ ] **C2f.** Tunnel config maps `http://localhost:5001` to public hostname

### C-Option-3 : Tailscale Funnel (zero-config TLS for personal use)

- [ ] **C3a.** Tailscale installed on i5 + on developer machine
- [ ] **C3b.** MagicDNS enabled in Tailnet admin console
- [ ] **C3c.** Tailscale Funnel allowed (admin console → Settings → Funnel)
- [ ] **C3d.** `tailscale funnel 5001` works → returns `https://<i5-name>.<tailnet>.ts.net`
- [ ] **C3e.** Verified externally : `curl https://<i5-name>.<tailnet>.ts.net/` (404 OK; means TLS works)

---

## D. Supabase Auth Verification

- [ ] **D1.** Supabase project URL known
  - Match the `https://<project>.supabase.co` used by genia.social
  - bui1 can find it in Supabase dashboard → Settings → API

- [ ] **D2.** Supabase JWKS endpoint reachable from i5
  - On i5 : `curl https://<project>.supabase.co/auth/v1/keys`
  - Should return JSON with `keys` array

- [ ] **D3.** Test JWT token available
  - Log into genia.social as a user, copy the JWT from browser localStorage / cookies
  - Used in Task 12 smoke test to verify Seed.Server validates correctly

---

## E. Process Lifecycle Management

Choose one (Windows assumed; adapt if Linux):

- [ ] **E1.** NSSM available for "run as Windows service"
  - Download : https://nssm.cc/download
  - Place at `C:\Tools\nssm.exe` (or in PATH)

OR

- [ ] **E2.** Task Scheduler entry to launch on boot
  - Trigger : at startup
  - Action : `C:\seed-server\Seed.Server.exe`
  - Run with highest privileges + whether user is logged on or not

OR

- [ ] **E3.** systemd unit (Linux)
  - Standard `[Service]` block with `Restart=always`

---

## F. Logging & Observability

- [ ] **F1.** Log directory writable
  - `C:\seed-server\logs\` (or `/var/log/seed-server/`)
  - Pre-create : `mkdir C:\seed-server\logs`

- [ ] **F2.** Log rotation strategy
  - Serilog with daily rolling files configured in `appsettings.json` (Serilog.Sinks.File)
  - Or rely on Windows Event Log if running as service

---

## G. Backup Strategy

- [ ] **G1.** `C:\seed-data\` backup target exists
  - User's existing backup tool (Backblaze, Synology Hyper Backup, Duplicati, ...) covers this path
  - OR scheduled robocopy to a second disk
  - Without backup, a disk failure loses all user projects

---

## H. Security Sanity Checks

- [ ] **H1.** Windows Defender / antivirus exclusion for `Seed.Server.exe`
  - Avoids file-locks during writes

- [ ] **H2.** Firewall rule : inbound 5001 ONLY from `cloudflared` / Tailscale interface
  - DO NOT expose 5001 to the public internet directly without TLS in front

- [ ] **H3.** `appsettings.json` permissions
  - Read-only for the service account
  - No secrets committed to git (Supabase JWT secret stays in env vars or User Secrets)

---

## Quick Diagnostic Commands

To run on i5 :

```powershell
# B1
dotnet --info

# C2 (Cloudflare Tunnel)
cloudflared tunnel list

# C3 (Tailscale Funnel)
tailscale funnel status

# E1 (NSSM)
nssm version

# F1 (log dir)
Test-Path C:\seed-server\logs

# A4 (disk free)
Get-PSDrive C | Select-Object Used,Free

# G1 (backup target)
Get-ScheduledTask -TaskName "*backup*" -ErrorAction SilentlyContinue
```

---

## Outcome Reporting

Once the checklist is run, report back to next-session-Claude with this format :

```
i5 verification report — 2026-04-26
====================================

A. Identity      : ALL GREEN  (Always-on Windows 11 mini-PC, RDP available)
B. .NET runtime  : ALL GREEN  (.NET 8.0.412 SDK installed)
C. Network       : Option C2 — ALL GREEN  (Cloudflare Tunnel at seed.bui1.dev)
D. Supabase      : ALL GREEN  (JWKS reachable, test token valid)
E. Lifecycle     : E1 GREEN   (NSSM 2.24 installed)
F. Logging       : F1 GREEN   (C:\seed-server\logs created and writable)
G. Backup        : G1 GREEN   (Backblaze covers C:\seed-data)
H. Security      : ALL GREEN  (firewall scoped to cloudflared interface only)

GO / NO-GO : GO

Decided endpoint URL : https://seed.bui1.dev
Storage root path     : C:\seed-data
Process manager       : NSSM (Windows service)
```

If any item is RED or partial, **document what's missing + the planned fix** before next session dispatches Task 2.
