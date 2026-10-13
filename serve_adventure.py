"""Serve the art-only preview on this Mac only."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

root = Path(__file__).resolve().parent / "WebBuildV1"
if not (root / "index.html").is_file():
    raise SystemExit("Chưa build ArtPreview. Xem Architecture/CODE_ARCHITECTURE.md.")
server = ThreadingHTTPServer(("127.0.0.1", 8791), partial(SimpleHTTPRequestHandler, directory=str(root)))
print("Mẫu mỹ thuật: http://127.0.0.1:8791/", flush=True)
try:
    server.serve_forever()
except KeyboardInterrupt:
    pass
finally:
    server.server_close()
