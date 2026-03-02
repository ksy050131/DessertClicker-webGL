import sys
import os
import subprocess
import threading

import msvcrt
msvcrt.setmode(sys.stdin.fileno(), os.O_BINARY)
msvcrt.setmode(sys.stdout.fileno(), os.O_BINARY)

def forward_stdin(proc):
    try:
        while True:
            line = sys.stdin.buffer.readline()
            if not line:
                break
            proc.stdin.write(line)
            proc.stdin.flush()
    except:
        pass

def convert_stdout(proc):
    try:
        while True:
            line = proc.stdout.readline()
            if not line:
                break
            # Convert \r\n to \n by removing all \r
            cleaned = line.replace(b'\r', b'')
            sys.stdout.buffer.write(cleaned)
            sys.stdout.buffer.flush()
    except:
        pass

cmd = [
  "C:\\Users\\admin\\.local\\bin\\uvx.exe",
    "--prerelease",
    "explicit",
    "--from",
    "mcpforunityserver>=0.0.0a0",
    "mcp-for-unity",
    "--transport",
    "stdio"
]

proc = subprocess.Popen(
    cmd,
    stdin=subprocess.PIPE,
    stdout=subprocess.PIPE,
    stderr=subprocess.DEVNULL,
    creationflags=subprocess.CREATE_NO_WINDOW if sys.platform == 'win32' else 0
)

t1 = threading.Thread(target=forward_stdin, args=(proc,), daemon=True)
t2 = threading.Thread(target=convert_stdout, args=(proc,), daemon=True)
t1.start()
t2.start()

try:
    sys.exit(proc.wait())
except:
    proc.terminate()