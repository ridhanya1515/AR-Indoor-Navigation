import sys, unittest, tempfile, json, threading, urllib.request, urllib.error, sqlite3, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Server'))
from server import Store,CampusServer,validate_map
class ServerTests(unittest.TestCase):
 def setUp(self):
  self.temp=tempfile.TemporaryDirectory();self.store=Store(Path(self.temp.name)/'test.db')
  self.map=json.loads((ROOT/'Server/map.json').read_text());self.store.set_map(self.map)
  self.store.add_user('student','test-only-password')
  self.server=CampusServer(('127.0.0.1',0),self.store)
  self.thread=threading.Thread(target=self.server.serve_forever,daemon=True);self.thread.start()
  self.url='http://127.0.0.1:'+str(self.server.server_port)
 def tearDown(self):
  self.server.shutdown();self.server.server_close();self.thread.join();self.temp.cleanup()
 def req(self,path,data=None,token=None):
  headers={'Content-Type':'application/json'}
  if token:headers['Authorization']='Bearer '+token
  request=urllib.request.Request(self.url+path,data=json.dumps(data).encode() if data is not None else None,headers=headers)
  try:
   with urllib.request.urlopen(request) as r:return r.status,json.load(r)
  except urllib.error.HTTPError as e:return e.code,json.load(e)
 def test_login_and_protected_map(self):
  self.assertEqual(self.req('/api/map')[0],401)
  status,response=self.req('/api/login',{'username':'student','password':'test-only-password'})
  self.assertEqual(status,200);self.assertGreater(len(response['token']),30)
  status,data=self.req('/api/map',token=response['token']);self.assertEqual(status,200);self.assertEqual(data,self.map)
 def test_wrong_password(self):self.assertEqual(self.req('/api/login',{'username':'student','password':'wrong'})[0],401)
 def test_unknown_user(self):self.assertEqual(self.req('/api/login',{'username':'unknown','password':'wrong'})[0],401)
 def test_expired_token(self):
  token=self.store.login('student','test-only-password')
  with self.store.connect() as db:db.execute('UPDATE sessions SET expires=?',(time.time()-1,))
  self.assertEqual(self.req('/api/map',token=token)[0],401)
 def test_password_not_plaintext(self):
  with self.store.connect() as db:row=db.execute('SELECT * FROM users').fetchone()
  self.assertNotIn('test-only-password',row);self.assertEqual(len(row[2]),64)
 def test_reset_revokes_tokens(self):
  token=self.store.login('student','test-only-password');self.store.add_user('student','replacement-password')
  self.assertFalse(self.store.authorized(token))
 def test_invalid_map(self):
  self.map['edges'][0]['a']='missing'
  with self.assertRaises(ValueError):validate_map(self.map)
 def test_bad_login_payload(self):self.assertEqual(self.req('/api/login',{'password':'x'})[0],400)
 def test_rate_limit(self):
  for i in range(10):self.req('/api/login',{})
  self.assertEqual(self.req('/api/login',{})[0],429)
if __name__=='__main__':unittest.main(verbosity=2)
