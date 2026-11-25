using System;
using System.Collections.Generic;
using System.Linq;

namespace CampusNavigation
{
    [Serializable] public struct Point
    {
        public float x,y,z;
        public Point(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Point operator +(Point a,Point b){return new Point(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Point operator -(Point a,Point b){return new Point(a.x-b.x,a.y-b.y,a.z-b.z);}
        public static Point operator *(Point a,float b){return new Point(a.x*b,a.y*b,a.z*b);}
        public float Length {get{return (float)Math.Sqrt(x*x+y*y+z*z);}}
        public static float Distance(Point a,Point b){return (a-b).Length;}
        public static float Horizontal(Point a,Point b){return (float)Math.Sqrt((a.x-b.x)*(a.x-b.x)+(a.z-b.z)*(a.z-b.z));}
        public static Point Project(Point p,Point a,Point b)
        {
            Point d=b-a;float l=d.x*d.x+d.y*d.y+d.z*d.z;
            float t=l<.00001f?0:((p.x-a.x)*d.x+(p.y-a.y)*d.y+(p.z-a.z)*d.z)/l;
            return a+d*Math.Max(0,Math.Min(1,t));
        }
    }
    [Serializable] public class MapNode
    {
        public string id,label;public float x,y,z,yaw;public int floor;public bool destination,checkpoint;
        public Point Position {get{return new Point(x,y,z);}}
    }
    [Serializable] public class MapEdge
    {
        public string id,a,b;public float multiplier=1;public bool stairs,closed;
    }
    [Serializable] public class SensorConfig
    {
        public string id,edgeId,url; public bool enabled; public bool failClosed=true;
    }
    [Serializable] public class MapSolid {public string name,color;public float x,y,z,sx,sy,sz,rx;public int floor;}
    [Serializable] public class MapLabel {public string text;public float x,y,z;public int floor;}
    [Serializable] public class CampusMap
    {
        public string id,title,version,help;public bool surveyed;
        public MapSolid[] solids;public MapLabel[] labels;
        public MapNode[] nodes;public MapEdge[] edges;public SensorConfig[] sensors;
    }
    public class RoutePlan
    {
        public readonly List<Point> points=new List<Point>();
        public readonly List<int> floors=new List<int>();
        public readonly List<string> edgeIds=new List<string>();
        public string error;public float cost;
        public bool Found {get{return error==null && points.Count>0;}}
    }
    /// <summary>Pure C# graph and A*. Costs are metres times multiplier >= 1.</summary>
    public sealed class NavGraph
    {
        public readonly CampusMap map;
        public readonly Dictionary<string,MapNode> nodes=new Dictionary<string,MapNode>();
        public readonly Dictionary<string,MapEdge> edges=new Dictionary<string,MapEdge>();
        readonly Dictionary<string,List<MapEdge>> adjacent=new Dictionary<string,List<MapEdge>>();
        readonly HashSet<string> blocked=new HashSet<string>();
        public int Revision {get;private set;}
        public NavGraph(CampusMap map)
        {
            this.map=map;Validate(map);
            foreach(var n in map.nodes){nodes.Add(n.id,n);adjacent.Add(n.id,new List<MapEdge>());}
            foreach(var e in map.edges){edges.Add(e.id,e);adjacent[e.a].Add(e);adjacent[e.b].Add(e);}
        }
        public static void Validate(CampusMap map)
        {
            if(map==null||map.nodes==null||map.edges==null||map.nodes.Length<2)throw new ArgumentException("Map needs nodes and edges.");
            var ids=new HashSet<string>();var byId=new Dictionary<string,MapNode>();
            foreach(var n in map.nodes)
            {
                if(n==null||string.IsNullOrEmpty(n.id)||!ids.Add(n.id))throw new ArgumentException("Missing/duplicate node ID.");
                if(float.IsNaN(n.x)||float.IsInfinity(n.x)||float.IsNaN(n.y)||float.IsInfinity(n.y)||float.IsNaN(n.z)||float.IsInfinity(n.z))throw new ArgumentException("Invalid coordinates: "+n.id);
                byId.Add(n.id,n);
            }
            ids.Clear();
            foreach(var e in map.edges)
            {
                if(e==null||string.IsNullOrEmpty(e.id)||!ids.Add(e.id)||!byId.ContainsKey(e.a??"")||!byId.ContainsKey(e.b??"")||e.a==e.b)throw new ArgumentException("Invalid edge.");
                if(e.multiplier<1||float.IsNaN(e.multiplier)||float.IsInfinity(e.multiplier))throw new ArgumentException("Edge multiplier must be >=1: "+e.id);
                if(Point.Distance(byId[e.a].Position,byId[e.b].Position)<.01f)throw new ArgumentException("Zero-length edge: "+e.id);
                if(byId[e.a].floor!=byId[e.b].floor&&!e.stairs)throw new ArgumentException("Cross-floor edge must be stairs: "+e.id);
            }
            var sensorIds=new HashSet<string>();
            foreach(var s in map.sensors??new SensorConfig[0])
                if(s==null||string.IsNullOrEmpty(s.id)||!sensorIds.Add(s.id)||!ids.Contains(s.edgeId??""))throw new ArgumentException("Invalid sensor mapping.");
        }
        public bool IsBlocked(string id){return edges[id].closed||blocked.Contains(id);}
        public void SetBlocked(string id,bool value)
        {
            if(!edges.ContainsKey(id))throw new ArgumentException("Unknown edge "+id);
            bool changed=value?blocked.Add(id):blocked.Remove(id);if(changed)Revision++;
        }
        string Other(MapEdge e,string n){return e.a==n?e.b:e.a;}
        float Cost(MapEdge e){return Point.Distance(nodes[e.a].Position,nodes[e.b].Position)*e.multiplier;}
        public List<string> FindNodes(string start,string goal,out float cost)
        {
            cost=float.PositiveInfinity;if(!nodes.ContainsKey(start)||!nodes.ContainsKey(goal))return null;
            var open=new HashSet<string>{start};var done=new HashSet<string>();
            var g=new Dictionary<string,float>{{start,0}};var parent=new Dictionary<string,string>();
            while(open.Count>0)
            {
                string cur=open.OrderBy(n=>g[n]+Point.Distance(nodes[n].Position,nodes[goal].Position)).ThenBy(n=>n,StringComparer.Ordinal).First();
                if(cur==goal){cost=g[cur];var p=new List<string>{cur};while(parent.ContainsKey(cur)){cur=parent[cur];p.Add(cur);}p.Reverse();return p;}
                open.Remove(cur);done.Add(cur);
                foreach(var e in adjacent[cur])
                {
                    if(IsBlocked(e.id))continue;string next=Other(e,cur);if(done.Contains(next))continue;
                    float ng=g[cur]+Cost(e);
                    if(!g.ContainsKey(next)||ng<g[next]){g[next]=ng;parent[next]=cur;open.Add(next);}
                }
            }
            return null;
        }
        public MapEdge EdgeBetween(string a,string b)
        {return adjacent[a].Where(e=>Other(e,a)==b&&!IsBlocked(e.id)).OrderBy(Cost).FirstOrDefault();}
        public RoutePlan Plan(Point position,int floor,string goal,float snapLimit=1.25f)
        {
            if(!nodes.ContainsKey(goal))return new RoutePlan{error="Choose a valid destination."};
            // Attach to the geometrically nearest corridor, even if blocked. Never snap through a wall to a farther corridor.
            var candidates=edges.Values.Where(e=>!e.stairs&&nodes[e.a].floor==floor&&nodes[e.b].floor==floor)
                .Select(e=>new {edge=e,point=Point.Project(position,nodes[e.a].Position,nodes[e.b].Position)})
                .OrderBy(v=>Point.Distance(position,v.point)).ThenBy(v=>v.edge.id,StringComparer.Ordinal).ToList();
            if(candidates.Count==0||Point.Distance(position,candidates[0].point)>snapLimit)return new RoutePlan{error="Outside the mapped corridor. Return to a known checkpoint and align."};
            var chosen=candidates[0];float best=float.PositiveInfinity;RoutePlan answer=null;
            foreach(string start in new[]{chosen.edge.a,chosen.edge.b})
            {
                // A blocked corridor cannot be traversed to reach a distant endpoint.
                if(IsBlocked(chosen.edge.id)&&Point.Horizontal(position,nodes[start].Position)>.65f)continue;
                float cost;var path=FindNodes(start,goal,out cost);if(path==null)continue;
                float lead=Point.Distance(position,chosen.point)+Point.Distance(chosen.point,nodes[start].Position)*chosen.edge.multiplier;
                if(cost+lead>=best)continue;
                best=cost+lead;var r=new RoutePlan{cost=best};
                Add(r,position,floor);Add(r,chosen.point,floor);Add(r,nodes[start].Position,floor);
                if(!IsBlocked(chosen.edge.id)&&Point.Distance(chosen.point,nodes[start].Position)>.01f)r.edgeIds.Add(chosen.edge.id);
                for(int i=1;i<path.Count;i++){r.edgeIds.Add(EdgeBetween(path[i-1],path[i]).id);Add(r,nodes[path[i]].Position,nodes[path[i]].floor);}
                answer=r;
            }
            return answer??new RoutePlan{error="No available route. Stay in a clear area and ask for assistance."};
        }
        static void Add(RoutePlan r,Point p,int floor)
        {if(r.points.Count==0||Point.Distance(r.points[r.points.Count-1],p)>.001f){r.points.Add(p);r.floors.Add(floor);}}
    }
}
