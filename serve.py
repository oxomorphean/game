from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from functools import partial

root = Path(__file__).resolve().parent / "WebBuild"
if not (root / "index.html").exists():
    raise SystemExit("Chưa có bản web. Build scene CartoonArtTrial trong Unity trước.")
server = ThreadingHTTPServer(("127.0.0.1", 8788), partial(SimpleHTTPRequestHandler, directory=str(root)))
print("Mở http://127.0.0.1:8788/ — Ctrl+C để dừng", flush=True)
try:
    server.serve_forever()
except KeyboardInterrupt:
    server.server_close()
