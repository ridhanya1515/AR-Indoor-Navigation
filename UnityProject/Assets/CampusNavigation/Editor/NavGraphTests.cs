using System;
using System.Linq;
using CampusNavigation;
public static class NavGraphTests
{
 static int count;
 static void Check(bool condition,string name){if(!condition)throw new Exception(name);count++;}
 public static void Run()
 {
  var m=new CampusMap{nodes=new[]{
   new MapNode{id="A",x=0,z=0,checkpoint=true},new MapNode{id="B",x=0,z=4},
   new MapNode{id="C",x=4,z=4,destination=true},new MapNode{id="D",x=4,z=0}},
   edges=new[]{new MapEdge{id="AB",a="A",b="B"},new MapEdge{id="BC",a="B",b="C"},new MapEdge{id="AD",a="A",b="D",multiplier=2},new MapEdge{id="DC",a="D",b="C"}}};
  var g=new NavGraph(m);float cost;var p=g.FindNodes("A","C",out cost);
  Check(p.SequenceEqual(new[]{"A","B","C"})&&Math.Abs(cost-8)<.01,"Shortest route");
  g.SetBlocked("BC",true);p=g.FindNodes("A","C",out cost);
  Check(p.SequenceEqual(new[]{"A","D","C"})&&Math.Abs(cost-12)<.01,"Blocked edge reroutes");
  Check(g.Plan(new Point(0,0,0),0,"C").Found,"Position attached at endpoint");
  g.SetBlocked("DC",true);Check(g.FindNodes("A","C",out cost)==null,"Disconnected goal");
  g.SetBlocked("BC",false);g.SetBlocked("DC",false);
  Check(!g.Plan(new Point(30,0,30),0,"C").Found,"Off-map rejected");
  g.SetBlocked("AB",true);Check(!g.Plan(new Point(0,0,2),0,"C").Found,"Cannot bypass blocked current corridor");
  int r=g.Revision;g.SetBlocked("AB",true);Check(g.Revision==r,"No repeated revision");
  g.SetBlocked("AB",false);Check(g.FindNodes("A","C",out cost).Contains("B"),"Restored route");
  Check(!g.Plan(new Point(0,0,0),0,"missing").Found,"Unknown destination");
  m.edges[0].multiplier=.5f;bool rejected=false;try{new NavGraph(m);}catch(ArgumentException){rejected=true;}Check(rejected,"Invalid heuristic costs rejected");
  Console.WriteLine("NavGraph: "+count+" checks passed");
 }
 public static void Main(){Run();}
}
