"""Debug: Check backend startup logs and DB connection"""
import subprocess
import time
import sys

print("[1] Starting backend and capturing logs...")

proc = subprocess.Popen(
    ["dotnet", "run", "--project", "src/FuelTrack.API", "--no-launch-profile"],
    cwd=r"C:\Users\User\Desktop\FuelTrack-Rebuild",
    stdout=subprocess.PIPE,
    stderr=subprocess.STDOUT,
    text=True,
    encoding="utf-8",
    errors="replace"
)

logs = []
start_time = time.time()
port_ready = False

while time.time() - start_time < 45:
    line = proc.stdout.readline()
    if line:
        line = line.rstrip()
        logs.append(line)
        # Print interesting lines
        if any(kw in line.lower() for kw in ["error", "exception", "fail", "listening", "started", "database", "migration", "warn"]):
            print(f"  [{int(time.time()-start_time):>3}s] {line[:200]}")
        if "listening" in line.lower() or "now listening" in line.lower():
            port_ready = True
    if port_ready:
        time.sleep(3)
        break

# Print last 50 lines of logs
print(f"\n[2] Last 50 log lines:")
for log in logs[-50:]:
    print(f"  {log[:200]}")

proc.terminate()
proc.wait(timeout=5)
