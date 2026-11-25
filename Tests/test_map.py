"""Independent map topology checks; does not claim to execute Unity's C# runtime."""
import json,math,heapq,unittest
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
m=json.loads((ROOT/'Server/map.json').read_text());nodes={n['id']:n for n in m['nodes']}
def path(start,goal,blocked=()):
 q=[(0,start,[])];visited=set()
 while q:
  cost,n,route=heapq.heappop(q)
  if n in visited:continue
  visited.add(n)
  if n==goal:return cost,route+[n]
  for e in m['edges']:
   if e['id'] in blocked or e.get('closed'):continue
   if n not in (e['a'],e['b']):continue
   other=e['b'] if n==e['a'] else e['a']
   d=math.dist([nodes[n][k] for k in ('x','y','z')],[nodes[other][k] for k in ('x','y','z')])
   heapq.heappush(q,(cost+d*e['multiplier'],other,route+[n]))
 return None
class MapTests(unittest.TestCase):
 def test_every_destination(self):
  for n in m['nodes']:
   if n.get('destination'):self.assertIsNotNone(path('Entrance',n['id']),n['id'])
 def test_hardware_reroute(self):
  normal=path('Entrance','Office');reroute=path('Entrance','Office',{'Junction_MainNorth'})
  self.assertIsNotNone(reroute);self.assertGreater(reroute[0],normal[0]);self.assertNotEqual(normal[1],reroute[1])
 def test_no_route(self):self.assertIsNone(path('Entrance','Office',{e['id'] for e in m['edges'] if 'Office' in (e['a'],e['b'])}))
 def test_two_staircases(self):
  for side in ('Left','Right'):
   blocked={e['id'] for e in m['edges'] if e['stairs'] and (side in e['a'] or side in e['b'])}
   self.assertIsNotNone(path('Entrance','FOCLab',blocked))
 def test_sensor_mapping(self):
  ids={e['id'] for e in m['edges']}
  for s in m['sensors']:self.assertIn(s['edgeId'],ids)
 def test_bundle_matches_server(self):
  u=json.loads((ROOT/'UnityProject/Assets/CampusNavigation/Resources/DemoMap.json').read_text());self.assertEqual(m,u)
if __name__=='__main__':unittest.main(verbosity=2)
