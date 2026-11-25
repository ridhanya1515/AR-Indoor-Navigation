using System;
using System.Linq;
using UnityEngine;
namespace CampusNavigation
{
    public sealed class NavigationScreen : MonoBehaviour
    {
        public CampusApplication app;string page="select",selected="",checkpoint="",search="";
        Vector2 scroll;GUIStyle title,body,button,small,field;bool styled,showMap;int mapFloor;float width=540,height;
        readonly Color blue=new Color(.02f,.35f,.95f),navy=new Color(.035f,.075f,.15f,.95f);
        void Styles()
        {
            if(styled)return;styled=true;
            title=new GUIStyle(GUI.skin.label){fontSize=27,wordWrap=true,fontStyle=FontStyle.Bold};title.normal.textColor=Color.white;
            body=new GUIStyle(GUI.skin.label){fontSize=21,wordWrap=true};body.normal.textColor=Color.white;
            small=new GUIStyle(body){fontSize=17};
            button=new GUIStyle(GUI.skin.button){fontSize=22,wordWrap=true};
            field=new GUIStyle(GUI.skin.textField){fontSize=22};
        }
        void Box(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        bool Button(float x,float y,float w,string text){return GUI.Button(new Rect(x,y,w,49),text,button);}
        void Label(float y,string s,GUIStyle style=null,float h=65){GUI.Label(new Rect(22,y,496,h),s,style??body);}
        void OnGUI()
        {
            if(app==null)return;app.pose.allowSimulationInput=app.LoggedIn&&page=="navigate";Styles();float scale=Mathf.Min(Screen.safeArea.width/width,Screen.safeArea.height/900f);
            height=Screen.safeArea.height/scale;GUI.matrix=Matrix4x4.TRS(new Vector3(Screen.safeArea.x+(Screen.safeArea.width-width*scale)*.5f,Screen.height-Screen.safeArea.yMax,0),Quaternion.identity,new Vector3(scale,scale,1));
            if(!app.LoggedIn){Login();return;}
            if(app.map==null)return;
            if(page=="navigate"){Navigate();return;}
            if(page=="align"){Align();return;}
            Box(new Rect(0,0,width,height),navy);
            Label(12,"CAMPUS AR",title,40);Label(53,app.Mode,small,40);
            if(page=="settings"){Settings();return;}
            SelectDestination();
        }
        void Login()
        {
            Box(new Rect(0,0,width,height),navy);
            scroll=GUI.BeginScrollView(new Rect(0,0,width,height),scroll,new Rect(0,0,518,850));
            Label(40,"AR INDOOR NAVIGATION",title,80);Label(120,"Camera guidance with ESP32 obstacle rerouting",body,75);
            app.UseServerLogin=GUI.Toggle(new Rect(22,203,486,35),app.UseServerLogin,"Use optional laptop login server",button);
            if(app.UseServerLogin)app.ServerUrl=GUI.TextField(new Rect(22,242,486,46),app.ServerUrl,field);
            Label(306,"Username",small,30);app.Username=GUI.TextField(new Rect(22,338,486,46),app.Username,field);
            Label(402,"Password",small,30);app.Password=GUI.PasswordField(new Rect(22,434,486,46),app.Password,'*',field);
            GUI.enabled=!app.Busy;
            if(Button(22,507,486,app.Busy?"Connecting...":"Sign in and load map"))app.Login();

            GUI.enabled=true;
            Label(650,"Review account: student / Campus123!\nWorks without internet. This local demonstration account is not production security.",small,100);
            Label(754,app.Error,small,80);GUI.EndScrollView();
        }
        void SelectDestination()
        {
            Label(99,app.map.title,title,65);
            if(!app.map.surveyed)Label(165,"Your college layout - site measurements still need verification.",small,30);
            for(int f=0;f<3;f++)if(GUI.Button(new Rect(22+f*166,195,162,28),(mapFloor==f?"Selected: ":"")+"Floor "+f,small))mapFloor=f;
            Map(new Rect(22,227,496,200));
            search=GUI.TextField(new Rect(22,438,496,43),search,field);
            GUI.Label(new Rect(27,484,490,30),app.map.nodes.Count(n=>n.destination)+" destinations - scroll or search by room/block",small);
            var rooms=app.map.nodes.Where(n=>n.destination&&(string.IsNullOrEmpty(search)||(n.label??n.id).IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
            float listHeight=Mathf.Max(100,height-705);
            scroll=GUI.BeginScrollView(new Rect(22,520,496,listHeight),scroll,new Rect(0,0,470,rooms.Length*56));
            for(int i=0;i<rooms.Length;i++)if(GUI.Button(new Rect(0,i*56,469,50),(selected==rooms[i].id?"SELECTED: ":"")+rooms[i].label+"  (F"+rooms[i].floor+")",button)){selected=rooms[i].id;OpenDestination();}
            GUI.EndScrollView();
            if(Button(22,height-168,496,app.pose.Ready?"Start navigation":"Align location, then start"))
            {
                if(string.IsNullOrEmpty(selected)){app.Error="Select a room first.";}
                else OpenDestination();
            }
            if(Button(22,height-109,240,"Location / align")){app.BeginCamera();page="align";scroll=Vector2.zero;}
            if(Button(278,height-109,240,"Settings")){page="settings";scroll=Vector2.zero;}
            Label(height-51,string.IsNullOrEmpty(app.Error)?app.sensors.Status:app.Error,small,48);
        }
        void OpenDestination()
        {
            app.BeginCamera();app.Error="";scroll=Vector2.zero;
            if(!app.pose.Ready){page="align";return;}
            app.nav.StartRoute(selected);page="navigate";
        }
        void Align()
        {
            Box(new Rect(12,12,516,120),navy);
            Label(20,"Align your starting position",title,45);
            Label(70,app.pose.Message,small,60);
            float top=height*.39f;
            Box(new Rect(0,top,width,height-top),navy);
            var points=app.map.nodes.Where(n=>n.checkpoint).ToArray();
            float contentHeight=410+points.Length*57;
            scroll=GUI.BeginScrollView(new Rect(0,top,width,height-top-65),scroll,new Rect(0,0,518,contentHeight));
            Label(8,"Stand on a checkpoint and face its marked forward direction. The camera tracks movement after you align.",small,90);
            float y=108;
            foreach(var n in points)
            {if(Button(22,y,486,(checkpoint==n.id?"SELECTED: ":"")+n.label+" (F"+n.floor+")"))checkpoint=n.id;y+=57;}
            Label(y,"Phone height: "+app.pose.phoneHeight.ToString("F2")+" m",small,40);y+=45;
            if(Button(22,y,235,"Height - 5 cm"))app.pose.phoneHeight=Mathf.Max(.7f,app.pose.phoneHeight-.05f);
            if(Button(272,y,236,"Height + 5 cm"))app.pose.phoneHeight=Mathf.Min(2.2f,app.pose.phoneHeight+.05f);y+=65;
            if(Button(22,y,486,"Confirm position and direction"))
            {
                if(!app.graph.nodes.ContainsKey(checkpoint))app.Error="Select your actual starting checkpoint.";
                else
                {
                    app.pose.Align(app.graph.nodes[checkpoint]);
                    if(app.pose.Ready)
                    {
                        app.Error="";
                        if(!string.IsNullOrEmpty(selected)){app.nav.StartRoute(selected);page="navigate";}
                        else page="select";
                    }
                }
            }
            Label(y+60,app.Error,small,60);GUI.EndScrollView();
            if(Button(22,height-59,496,"Back to map"))page="select";
        }
        void Settings()
        {
            scroll=GUI.BeginScrollView(new Rect(0,99,width,height-160),scroll,new Rect(0,0,518,1040));
            Label(0,"ESP32 connection",title,55);Label(60,"Connect the phone to ESP32 Wi-Fi, or put both on the same router.",body,85);
            Label(151,"Sensor endpoint",small,30);app.SensorUrl=GUI.TextField(new Rect(22,185,486,46),app.SensorUrl,field);
            Label(242,"Sensor key (same as firmware)",small,35);app.SensorKey=GUI.PasswordField(new Rect(22,279,486,46),app.SensorKey,'*',field);
            if(Button(22,344,236,"Enable ESP32"))app.ConfigureSensor(true);
            if(Button(272,344,236,"Disable ESP32"))app.ConfigureSensor(false);
            Label(410,app.sensors.Status,small,100);
            Label(524,"DEMO CONTROLS",title,45);
            var e=app.map.sensors!=null&&app.map.sensors.Length>0?app.map.sensors[0].edgeId:app.map.edges[0].id;
            if(Button(22,582,486,"Toggle simulated blockage: "+e))app.sensors.ToggleDemo(e);
            if(Button(22,645,486,"Clear simulated blockage"))app.sensors.ClearDemo();
            Label(710,"Real hardware and simulated blockage are labelled separately. One fixed sensor monitors only its configured corridor edge.",small,90);
            Label(810,app.Error,small,70);
            if(Button(22,900,486,"Log out")){app.Logout();page="select";selected="";checkpoint="";}
            GUI.EndScrollView();if(Button(22,height-59,496,"Back"))page="select";
        }
        void Navigate()
        {
            var n=app.nav;Box(new Rect(12,12,516,184),blue);
            Label(22,n.Instruction,title,85);
            Label(111,(n.Destination!=null?app.graph.nodes[n.Destination].label:"")+"  |  "+(n.Route!=null&&n.Route.Found?n.Remaining.ToString("F1")+" m":"No route"),body,47);
            Label(158,"Floor "+app.pose.Floor+"  |  Reroutes "+n.Reroutes,small,30);
            if(!string.IsNullOrEmpty(n.Notice)){Box(new Rect(12,207,516,90),navy);Label(219,n.Notice,body,75);}
            if(showMap)Map(new Rect(22,310,496,Mathf.Min(230,height-610)));
            Box(new Rect(0,height-260,540,260),navy);
            Point p=app.pose.Position;Label(height-250,"X "+p.x.ToString("F2")+"  Z "+p.z.ToString("F2")+"  |  "+(app.pose.Ready?"Tracking":"Alignment required"),small,35);
            Label(height-211,app.sensors.Status,small,55);
            if(n.PendingLanding!=null)
            {
                if(Button(22,height-152,496,"I reached floor "+n.PendingLanding.floor+" landing"))n.ConfirmFloor();
            }
            else if(Button(22,height-152,240,"Show / hide map"))showMap=!showMap;
            if(n.PendingLanding==null&&Button(278,height-152,240,"Help"))n.Notify(app.map.help);
            if(Button(22,height-91,240,"Destinations")){page="select";scroll=Vector2.zero;}
            if(Button(278,height-91,240,"Realign")){app.BeginCamera();app.pose.Invalidate();page="align";scroll=Vector2.zero;}
            Label(height-35,app.pose.simulation?"EDITOR: W/A/S/D move; Q/E turn":"Step to change X/Z. Rotation changes heading.",small,34);
        }
        void Map(Rect rect)
        {
            int floor=page=="navigate"?app.pose.Floor:mapFloor;
            Box(rect,new Color(.89f,.94f,1));
            var nodes=app.map.nodes.Where(n=>n.floor==floor).ToArray();if(nodes.Length==0)return;
            float minX=nodes.Min(n=>n.x)-1,maxX=nodes.Max(n=>n.x)+1,minZ=nodes.Min(n=>n.z)-1,maxZ=nodes.Max(n=>n.z)+1;
            Func<Point,Vector2> pt=p=>new Vector2(rect.x+12+(p.x-minX)/(maxX-minX)*(rect.width-24),rect.yMax-12-(p.z-minZ)/(maxZ-minZ)*(rect.height-24));
            if(app.map.solids!=null)foreach(var solid in app.map.solids)
            {
                if(solid.floor!=floor&&!(floor==0&&solid.floor<0))continue;
                if(solid.color=="grass"||solid.sy>4||solid.y>floor*3.5f+2.5f)continue;
                Vector2 lo=pt(new Point(solid.x-solid.sx/2,0,solid.z+solid.sz/2));
                Vector2 hi=pt(new Point(solid.x+solid.sx/2,0,solid.z-solid.sz/2));
                Rect footprint=Rect.MinMaxRect(Mathf.Max(rect.x,lo.x),Mathf.Max(rect.y,lo.y),Mathf.Min(rect.xMax,hi.x),Mathf.Min(rect.yMax,hi.y));
                if(footprint.width>0&&footprint.height>0)Box(footprint,solid.color=="road"?new Color(.75f,.79f,.82f):new Color(.64f,.73f,.77f));
            }
            foreach(var e in app.map.edges)
            {
                var a=app.graph.nodes[e.a];var b=app.graph.nodes[e.b];if(a.floor!=floor||b.floor!=floor)continue;
                Line(pt(a.Position),pt(b.Position),app.graph.IsBlocked(e.id)?Color.red:new Color(.36f,.47f,.62f),3);
            }
            var route=app.nav.Route;
            if(route!=null&&route.Found&&app.nav.Active)for(int i=1;i<route.points.Count;i++)if(route.floors[i]==floor&&route.floors[i-1]==floor)Line(pt(route.points[i-1]),pt(route.points[i]),blue,5);
            foreach(var n in nodes.Where(n=>n.destination)){var v=pt(n.Position);Box(new Rect(v.x-4,v.y-4,8,8),n.id==selected?Color.magenta:Color.gray);}
            var labelStyle=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=true};labelStyle.normal.textColor=new Color(.03f,.09f,.17f);
            if(floor==0&&app.map.labels!=null)foreach(var label in app.map.labels)
            {
                if(!new[]{"PA Polytechnic","AI Block","Canteen","Temple","PA Educational Institutions","Chairman Room","Girls Restroom"}.Contains(label.text))continue;
                Vector2 v=pt(new Point(label.x,0,label.z));GUI.Label(new Rect(Mathf.Clamp(v.x-43,rect.x,rect.xMax-86),Mathf.Clamp(v.y-14,rect.y,rect.yMax-28),86,28),label.text,labelStyle);
            }
            if(floor>0)foreach(var n in nodes.Where(n=>n.destination))
            {Vector2 v=pt(n.Position);GUI.Label(new Rect(Mathf.Clamp(v.x-38,rect.x,rect.xMax-76),Mathf.Clamp(v.y-25,rect.y,rect.yMax-28),76,28),n.label,labelStyle);}
            if(app.pose.Ready&&app.pose.Floor==floor){var v=pt(app.pose.Position);Box(new Rect(v.x-6,v.y-6,12,12),new Color(1,.65f,0));}
        }
        void Line(Vector2 a,Vector2 b,Color color,float thickness)
        {
            Matrix4x4 save=GUI.matrix;GUI.matrix=save*Matrix4x4.TRS(new Vector3(a.x,a.y,0),Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg),Vector3.one);
            Box(new Rect(0,-thickness/2,(b-a).magnitude,thickness),color);GUI.matrix=save;
        }
    }
}
