"""Local campus demonstration server. Python 3.10+, standard library only.
Run behind HTTPS for real accounts; built-in HTTP is for a trusted private demo LAN.
"""
from __future__ import annotations
import argparse, getpass, hashlib, hmac, json, math, secrets, sqlite3, ssl, time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from threading import Lock

ROOT = Path(__file__).resolve().parent
ITERATIONS = 310_000

def validate_map(data):
    nodes = data.get('nodes', []); edges = data.get('edges', [])
    if len(nodes) < 2 or not edges: raise ValueError('Map needs nodes and edges')
    by_id = {}
    for n in nodes:
        if not n.get('id') or n['id'] in by_id: raise ValueError('Duplicate/missing node ID')
        if any(not math.isfinite(n.get(k, float('nan'))) for k in ('x','y','z')): raise ValueError('Invalid position')
        by_id[n['id']] = n
    edge_ids = set()
    for e in edges:
        if not e.get('id') or e['id'] in edge_ids or e.get('a') not in by_id or e.get('b') not in by_id or e['a']==e['b']: raise ValueError('Invalid edge')
        if not math.isfinite(e.get('multiplier',1)) or e.get('multiplier',1)<1: raise ValueError('Invalid cost')
        if by_id[e['a']]['floor']!=by_id[e['b']]['floor'] and not e.get('stairs'): raise ValueError('Unmarked stairs')
        edge_ids.add(e['id'])
    if not any(n.get('checkpoint') for n in nodes) or not any(n.get('destination') for n in nodes): raise ValueError('Missing checkpoint or destination')
    for s in data.get('sensors',[]):
        if s.get('edgeId') not in edge_ids: raise ValueError('Unknown sensor edge')
    return data

class Store:
    def __init__(self, path):
        self.path = str(path)
        with self.connect() as db:
            db.executescript('''CREATE TABLE IF NOT EXISTS users(username TEXT PRIMARY KEY,salt TEXT NOT NULL,digest TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS sessions(digest TEXT PRIMARY KEY,username TEXT NOT NULL,expires REAL NOT NULL);
            CREATE TABLE IF NOT EXISTS maps(id INTEGER PRIMARY KEY CHECK(id=1),payload TEXT NOT NULL);''')
    def connect(self): return sqlite3.connect(self.path, timeout=10)
    def set_map(self, data):
        validate_map(data)
        with self.connect() as db: db.execute('INSERT OR REPLACE INTO maps VALUES(1,?)',(json.dumps(data),))
    def get_map(self):
        with self.connect() as db:
            row = db.execute('SELECT payload FROM maps WHERE id=1').fetchone()
        return json.loads(row[0]) if row else None
    def add_user(self, username, password):
        if not 1<=len(username)<=80 or len(password)<10: raise ValueError('Username 1-80 characters; password at least 10 characters')
        salt = secrets.token_hex(16)
        digest = hashlib.pbkdf2_hmac('sha256', password.encode(), bytes.fromhex(salt), ITERATIONS).hex()
        with self.connect() as db:
            db.execute('INSERT OR REPLACE INTO users VALUES(?,?,?)',(username,salt,digest))
            db.execute('DELETE FROM sessions WHERE username=?',(username,))
    def login(self, username, password):
        with self.connect() as db: row=db.execute('SELECT salt,digest FROM users WHERE username=?',(username,)).fetchone()
        salt,digest=row if row else ('00'*16,'00'*32)
        candidate=hashlib.pbkdf2_hmac('sha256',password.encode(),bytes.fromhex(salt),ITERATIONS).hex()
        if not hmac.compare_digest(candidate,digest) or not row:return None
        token=secrets.token_urlsafe(32)
        with self.connect() as db:
            db.execute('DELETE FROM sessions WHERE expires<?',(time.time(),))
            db.execute('INSERT INTO sessions VALUES(?,?,?)',(hashlib.sha256(token.encode()).hexdigest(),username,time.time()+8*3600))
        return token
    def authorized(self, token):
        with self.connect() as db:
            row=db.execute('SELECT expires FROM sessions WHERE digest=?',(hashlib.sha256(token.encode()).hexdigest(),)).fetchone()
        return bool(row and row[0]>time.time())

class CampusServer(ThreadingHTTPServer):
    daemon_threads=True
    def __init__(self,address,store):
        super().__init__(address,Handler);self.store=store;self.attempts={};self.lock=Lock()
class Handler(BaseHTTPRequestHandler):
    def log_message(self, fmt, *args): pass  # Do not log credentials or tokens.
    def setup(self): super().setup();self.connection.settimeout(10)
    def reply(self,status,data):
        body=json.dumps(data).encode();self.send_response(status)
        self.send_header('Content-Type','application/json');self.send_header('Content-Length',str(len(body)))
        self.send_header('Cache-Control','no-store');self.end_headers();self.wfile.write(body)
    def do_GET(self):
        if self.path=='/health':return self.reply(200,{'status':'ok'})
        if self.path!='/api/map':return self.reply(404,{'error':'not found'})
        auth=self.headers.get('Authorization','')
        if not auth.startswith('Bearer ') or not self.server.store.authorized(auth[7:]):return self.reply(401,{'error':'unauthorized'})
        data=self.server.store.get_map()
        return self.reply(200,data) if data else self.reply(503,{'error':'map not configured'})
    def do_POST(self):
        if self.path!='/api/login':return self.reply(404,{'error':'not found'})
        ip=self.client_address[0];now=time.time()
        with self.server.lock:
            # Prune inactive clients and limit failed attempts to ten per minute/IP.
            self.server.attempts={k:[t for t in v if t>now-60] for k,v in self.server.attempts.items() if any(t>now-60 for t in v)}
            recent=self.server.attempts.setdefault(ip,[])
            if len(recent)>=10:return self.reply(429,{'error':'try later'})
            recent.append(now)
        try:
            size=int(self.headers.get('Content-Length','0'))
            if not 1<=size<=4096:raise ValueError()
            data=json.loads(self.rfile.read(size));user=data['username'];password=data['password']
            if not isinstance(user,str) or not isinstance(password,str) or len(user)>80 or len(password)>512:raise ValueError()
        except (ValueError,KeyError,TypeError):return self.reply(400,{'error':'invalid request'})
        token=self.server.store.login(user,password)
        if not token:return self.reply(401,{'error':'invalid credentials'})
        with self.server.lock:self.server.attempts.pop(ip,None)
        self.reply(200,{'token':token,'expiresIn':28800})

def main():
    p=argparse.ArgumentParser();p.add_argument('--db',default=str(ROOT/'campus.db'));p.add_argument('--host',default='0.0.0.0');p.add_argument('--port',type=int,default=8080)
    p.add_argument('--map',default=str(ROOT/'map.json'));p.add_argument('--add-user');p.add_argument('--cert');p.add_argument('--key');a=p.parse_args()
    store=Store(a.db)
    if a.add_user:
        pw=getpass.getpass('New password (at least 10 characters): ')
        if pw!=getpass.getpass('Repeat password: '):p.error('Passwords differ')
        store.add_user(a.add_user,pw);print('User saved.');return
    store.set_map(json.loads(Path(a.map).read_text(encoding='utf-8')))
    server=CampusServer((a.host,a.port),store)
    if a.cert and a.key:
        context=ssl.SSLContext(ssl.PROTOCOL_TLS_SERVER);context.load_cert_chain(a.cert,a.key);server.socket=context.wrap_socket(server.socket,server_side=True)
    print(f'Campus server listening on {a.host}:{a.port}. Use this computer LAN IP in the phone app.')
    try:server.serve_forever()
    except KeyboardInterrupt:pass
    finally:server.server_close()
if __name__=='__main__':main()
