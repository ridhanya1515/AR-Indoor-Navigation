using System;
using System.Linq;
using UnityEngine;
namespace CampusNavigation
{
    public sealed class NavigationSession : MonoBehaviour
    {
        public ARPoseSource pose;public NavGraph graph;
        public RoutePlan Route {get;private set;}
        public string Destination {get;private set;}
        public string Instruction {get;private set;}="Choose a destination.";
        public string Notice {get;private set;}="";
        public bool Arrived {get;private set;}
        public bool Active {get{return !string.IsNullOrEmpty(Destination)&&!Arrived;}}
        public int Next {get;private set;}
        public int Reroutes {get;private set;}
        public int Version {get;private set;}
        public float Remaining {get;private set;}
        public MapNode PendingLanding {get;private set;}
        public event Action Changed;
        int revision;float awayTime,lastPlan=-10,noticeUntil;bool previouslyReady;
        public void Initialize(NavGraph value){Stop();graph=value;revision=graph.Revision;}
        public void StartRoute(string id)
        {
            if(!pose.Ready){Notify("Align at a checkpoint first.");return;}
            if(!graph.nodes.ContainsKey(id)||!graph.nodes[id].destination){Notify("Select a destination.");return;}
            Destination=id;Arrived=false;Reroutes=0;Calculate(false);
        }
        public void Stop(){Destination=null;Route=null;Arrived=false;Remaining=0;PendingLanding=null;Next=0;Version++;Changed?.Invoke();}
        public void Notify(string text){Notice=text;noticeUntil=Time.unscaledTime+5;}
        void Calculate(bool reroute)
        {
            if(!pose.Ready||string.IsNullOrEmpty(Destination))return;
            Route=graph.Plan(pose.Position,pose.Floor,Destination);Next=Route.points.Count>1?1:0;
            Remaining=0;revision=graph.Revision;awayTime=0;lastPlan=Time.unscaledTime;PendingLanding=null;
            if(reroute)Reroutes++;
            Instruction=Route.Found?"Continue towards the route.":Route.error;
            if(reroute)Notify(Route.Found?"Route updated. Destination unchanged.":Route.error);
            Version++;Changed?.Invoke();
        }
        public void Realigned(){if(Active)Calculate(true);}
        public void ConfirmFloor()
        {
            if(PendingLanding==null)return;
            var node=PendingLanding;pose.ConfirmLanding(node);PendingLanding=null;Calculate(false);
        }
        void Update()
        {
            if(graph==null||pose==null)return;
            if(Time.unscaledTime>noticeUntil)Notice="";
            if(!pose.Ready){Instruction=pose.Message;previouslyReady=false;return;}
            if(!Active)return;
            if(!previouslyReady){previouslyReady=true;Calculate(false);}
            if(revision!=graph.Revision){Notify("Corridor status changed. Checking route...");Calculate(true);}
            if(Route==null||!Route.Found)
            {
                if(Time.unscaledTime-lastPlan>3)Calculate(false);
                return;
            }
            Point p=pose.Position;var goal=graph.nodes[Destination];
            if(goal.floor==pose.Floor&&Point.Horizontal(p,goal.Position)<.65f)
            {
                Arrived=true;Remaining=0;Instruction="Destination reached!";Notify("You have reached "+goal.label+".");Version++;Changed?.Invoke();return;
            }
            while(Next<Route.points.Count-1&&Route.floors[Next]==pose.Floor&&Point.Horizontal(p,Route.points[Next])<.55f)Next++;
            PendingLanding=null;
            if(Route.floors[Next]!=pose.Floor)
            {
                var target=Route.points[Next];
                // Actual landing must be near the surveyed top/bottom landing in horizontal space.
                if(Point.Horizontal(p,target)<.9f)
                    PendingLanding=graph.nodes.Values.Where(n=>n.floor==Route.floors[Next]&&Point.Distance(n.Position,target)<.1f).FirstOrDefault();
                Instruction="Use the stairs to floor "+Route.floors[Next]+". Confirm only after reaching the landing.";
                Remaining=DistanceLeft(p);return;
            }
            float nearest=float.PositiveInfinity;int nearIndex=Next;
            for(int i=Math.Max(1,Next);i<Route.points.Count;i++)
            {
                if(Route.floors[i]!=pose.Floor||Route.floors[i-1]!=pose.Floor)continue;
                float d=Point.Horizontal(p,Point.Project(p,Route.points[i-1],Route.points[i]));
                if(d<nearest){nearest=d;nearIndex=i;}
            }
            if(nearest<.65f&&nearIndex>Next)Next=nearIndex;
            awayTime=nearest>1f?awayTime+Time.unscaledDeltaTime:0;
            if(awayTime>1.2f&&Time.unscaledTime-lastPlan>2){Calculate(true);return;}
            Remaining=DistanceLeft(p);
            Vector3 delta=ARPoseSource.ToVector(Route.points[Next]-p);delta.y=0;
            Vector3 forward=Vector3.ProjectOnPlane(pose.Forward,Vector3.up);
            if(forward.sqrMagnitude<.1f){Instruction="Hold the phone upright to read the turn direction.";return;}
            float angle=Vector3.SignedAngle(forward,delta,Vector3.up);
            Instruction=Mathf.Abs(angle)>145?"Turn around":angle>30?"Turn right":angle< -30?"Turn left":"Continue straight";
        }
        float DistanceLeft(Point p)
        {
            float sum=Point.Distance(p,Route.points[Next]);
            for(int i=Next+1;i<Route.points.Count;i++)sum+=Point.Distance(Route.points[i-1],Route.points[i]);return sum;
        }
    }
}
