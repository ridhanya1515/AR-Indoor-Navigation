using System;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;

namespace CampusNavigation
{
    [Serializable] public class Credentials {public string username,password;}
    [Serializable] public class LoginReply {public string token,error;}

    public sealed class CampusApplication : MonoBehaviour
    {
        public TextAsset bundledMap;public Material routeMaterial;public GameObject collegeGeometry;
        GameObject collegeInstance;
        public bool simulateInEditor=true;
        public ARPoseSource pose {get;private set;}
        public NavigationSession nav {get;private set;}
        public SensorClient sensors {get;private set;}
        public CampusMap map {get;private set;}
        public NavGraph graph {get;private set;}
        public string Error="",Mode="",Username="",Password="",ServerUrl="http://192.168.1.10:8080";
        public string SensorUrl="http://192.168.4.1/status",SensorKey="YOUR_SENSOR_KEY";
        public bool UseServerLogin=false; public bool CameraStarted {get;private set;}
        GameObject sessionObject; ARCameraManager cameraManager; ARCameraBackground cameraBackground;
        public bool Busy {get;private set;} public bool LoggedIn {get;private set;}
        Transform mapRoot;string token;bool simulation;

        void Awake()
        {
            Application.targetFrameRate=30;Screen.sleepTimeout=SleepTimeout.NeverSleep;
            simulation=Application.isEditor&&simulateInEditor;

            mapRoot=new GameObject("Map coordinates (metres)").transform;
            var cameraGo=new GameObject("AR Camera");cameraGo.tag="MainCamera";
            var camera=cameraGo.AddComponent<Camera>();camera.nearClipPlane=.05f;camera.farClipPlane=100;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.07f,.12f);
            cameraGo.AddComponent<AudioListener>();
            pose=gameObject.AddComponent<ARPoseSource>();pose.viewCamera=camera;pose.mapRoot=mapRoot;pose.simulation=simulation;

            if(!simulation)
            {
                var sessionGo=new GameObject("AR Session");sessionGo.SetActive(false);sessionObject=sessionGo;pose.session=sessionGo.AddComponent<ARSession>();sessionGo.AddComponent<ARInputManager>();
                var origin=new GameObject("XR Origin").AddComponent<XROrigin>();
                var offset=new GameObject("Camera Offset");offset.transform.SetParent(origin.transform,false);
                cameraGo.transform.SetParent(offset.transform,false);origin.Camera=camera;origin.CameraFloorOffsetObject=offset;origin.CameraYOffset=0;
                origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Device;
                cameraGo.SetActive(false);
                cameraManager=cameraGo.AddComponent<ARCameraManager>();cameraManager.enabled=false;cameraBackground=cameraGo.AddComponent<ARCameraBackground>();cameraBackground.enabled=false;
                var driver=cameraGo.AddComponent<TrackedPoseDriver>();
                driver.trackingType=TrackedPoseDriver.TrackingType.RotationAndPosition;driver.updateType=TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
                var position=new InputAction("AR position",InputActionType.Value,expectedControlType:"Vector3");
                position.AddBinding("<HandheldARInputDevice>/devicePosition");position.AddBinding("<XRHMD>/centerEyePosition");
                var rotation=new InputAction("AR rotation",InputActionType.Value,expectedControlType:"Quaternion");
                rotation.AddBinding("<HandheldARInputDevice>/deviceRotation");rotation.AddBinding("<XRHMD>/centerEyeRotation");
                driver.positionInput=new InputActionProperty(position);driver.rotationInput=new InputActionProperty(rotation);
                driver.ignoreTrackingState=true;cameraGo.SetActive(true);
            }

            nav=gameObject.AddComponent<NavigationSession>();nav.pose=pose;
            sensors=gameObject.AddComponent<SensorClient>();
            var visual=gameObject.AddComponent<RouteVisual>();visual.nav=nav;visual.mapRoot=mapRoot;visual.material=routeMaterial;
            gameObject.AddComponent<NavigationScreen>().app=this;
        }

        public void Login()
        {
            if(Busy)return;
            if(UseServerLogin){StartCoroutine(LoginAndMap());return;}
            if(!ReviewLogin.Valid(Username,Password)){Error="Incorrect username or password.";Password="";return;}

            try
            {
                LoadMap(bundledMap.text);Mode="LOCAL REVIEW ACCOUNT";LoggedIn=true;Error="";Password="";
                ConfigureSensor(true);
            }
            catch(Exception e){Error=e.Message;}
        }

        public void BeginCamera()
        {
            if(CameraStarted)return;
            CameraStarted=true;
#if UNITY_ANDROID && !UNITY_EDITOR
            if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
#endif
            if(!simulation){sessionObject.SetActive(true);cameraManager.enabled=true;cameraBackground.enabled=true;}
        }

        IEnumerator LoginAndMap()
        {
            Busy=true;Error="";token=null;
            var payload=JsonUtility.ToJson(new Credentials{username=Username,password=Password});

            using(var request=new UnityWebRequest(ServerUrl.TrimEnd('/')+"/api/login","POST"))
            {
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));request.downloadHandler=new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type","application/json");request.timeout=10;
                yield return request.SendWebRequest();

                if(request.result!=UnityWebRequest.Result.Success){Error="Login failed. Check server address, connection and credentials.";Busy=false;yield break;}
                LoginReply reply=null;try{reply=JsonUtility.FromJson<LoginReply>(request.downloadHandler.text);}catch(Exception){ }
                if(reply==null||string.IsNullOrEmpty(reply.token)){Error="Invalid login response.";Busy=false;yield break;}token=reply.token;
            }

            Password="";

            using(var request=UnityWebRequest.Get(ServerUrl.TrimEnd('/')+"/api/map"))
            {
                request.SetRequestHeader("Authorization","Bearer "+token);request.timeout=10;yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success){Error="Map could not be loaded. Sign in again after checking server.";Busy=false;yield break;}

                try{LoadMap(request.downloadHandler.text);Mode="SERVER MAP - "+map.version;LoggedIn=true;ConfigureSensor(true);}
                catch(Exception e){Error="Map rejected: "+e.Message;}
            }

            Busy=false;
        }

        void LoadMap(string json)
        {
            var loaded=JsonUtility.FromJson<CampusMap>(json);var g=new NavGraph(loaded);
            if(!loaded.nodes.Any(n=>n.checkpoint)||!loaded.nodes.Any(n=>n.destination))throw new ArgumentException("Map needs checkpoints and destinations.");
            map=loaded;graph=g;nav.Initialize(g);pose.Invalidate();sensors.Initialize(g);

            if(simulation)
            {
                if(collegeGeometry!=null)
                {
                    if(collegeInstance!=null)Destroy(collegeInstance);
                    collegeInstance=Instantiate(collegeGeometry,mapRoot,false);
                    collegeInstance.AddComponent<CollegeView>().pose=pose;
                }
                else CreateSimulationFloor();
            }
        }

        void CreateSimulationFloor()
        {
            var old=GameObject.Find("Simulation corridors");if(old!=null)Destroy(old);
            var root=new GameObject("Simulation corridors");root.transform.SetParent(mapRoot,false);

            foreach(var edge in map.edges)
            {
                if(edge.stairs)continue;
                Vector3 a=ARPoseSource.ToVector(graph.nodes[edge.a].Position),b=ARPoseSource.ToVector(graph.nodes[edge.b].Position);
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=edge.id;go.transform.SetParent(root.transform,false);
                go.transform.localPosition=(a+b)*.5f-Vector3.up*.08f;go.transform.localRotation=Quaternion.LookRotation(b-a);
                go.transform.localScale=new Vector3(1.8f,.1f,Vector3.Distance(a,b));
                var mat=new Material(routeMaterial);mat.color=new Color(.15f,.21f,.27f);go.GetComponent<Renderer>().material=mat;
            }
        }

        public void ConfigureSensor(bool enable)
        {
            if(map.sensors==null||map.sensors.Length==0){Error="This map has no sensor mapping.";return;}
            Uri parsed;if(enable&&(!Uri.TryCreate(SensorUrl,UriKind.Absolute,out parsed)||(parsed.Scheme!="http"&&parsed.Scheme!="https"))){Error="Use a full HTTP/HTTPS sensor URL.";return;}
            map.sensors[0].enabled=enable;map.sensors[0].url=SensorUrl;sensors.key=SensorKey;sensors.Initialize(graph);Error="";
        }

        public void Logout()
        {
            if(sessionObject!=null)sessionObject.SetActive(false);
            if(cameraManager!=null)cameraManager.enabled=false;
            if(cameraBackground!=null)cameraBackground.enabled=false;
            CameraStarted=false;nav.Stop();pose.Invalidate();LoggedIn=false;token=null;Password="";Error="";sensors.Shutdown();
        }
    }
}