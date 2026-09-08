"""Serve the generated WebGL build locally: python Tools/serve_web.py [port]."""
from functools import partial
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import sys

build = Path(__file__).resolve().parent.parent / 'Builds/WebGL'
if not (build / 'index.html').is_file():
    raise SystemExit('Build missing. In Unity, choose Garden Snake > Build WebGL first.')
port = int(sys.argv[1]) if len(sys.argv) > 1 else 8080
handler = partial(SimpleHTTPRequestHandler, directory=str(build))
print(f'Garden Snake: http://localhost:{port}', flush=True)
with ThreadingHTTPServer(('127.0.0.1', port), handler) as server:
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
