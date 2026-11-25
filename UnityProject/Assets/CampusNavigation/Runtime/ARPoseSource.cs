using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
namespace CampusNavigation
{
    public sealed class ARPoseSource : MonoBehaviour
    {
        public Camera viewCamera; public Transform mapRoot;public ARSession session;
        public bool simulation; public bool allowSimulationInput; public float phoneHeight=1.4f;
        public bool Aligned {get;private set;} public int Floor {get;private set;}
        public string Message {get;private set;}="Move the camera slowly to find visual features.";
        public float LastTranslation {get;private set;}
        Vector3 lastCamera;Vector3 simPosition;float simYaw;bool wasTracking;
        public bool Tracking {get{return simulation||ARSession.state==ARSessionState.SessionTracking;}}
        public bool Ready {get{return Aligned&&Tracking;}}
        public Point Position
        {get{Vector3 p=simulation?simPosition:mapRoot.InverseTransformPoint(viewCamera.transform.position-Vector3.up*phoneHeight);return new Point(p.x,p.y,p.z);}}
        public Vector3 Forward {get{return simulation?Quaternion.Euler(0,simYaw,0)*Vector3.forward:mapRoot.InverseTransformDirection(viewCamera.transform.forward);}}
        public void Align(MapNode checkpoint)
        {
            if(!Tracking){Message="AR tracking is not ready. Check camera permission and lighting.";return;}
            if(simulation){simPosition=ToVector(checkpoint.Position);simYaw=checkpoint.yaw;viewCamera.transform.position=simPosition+Vector3.up*phoneHeight;viewCamera.transform.rotation=Quaternion.Euler(18,simYaw,0);}
            else
            {
                Vector3 direction=Vector3.ProjectOnPlane(viewCamera.transform.forward,Vector3.up);
                if(direction.sqrMagnitude<.15f){Message="Hold the phone upright, facing the checkpoint's marked forward direction.";return;}
                float heading=Quaternion.LookRotation(direction).eulerAngles.y;
                mapRoot.rotation=Quaternion.Euler(0,heading-checkpoint.yaw,0);
                mapRoot.position=viewCamera.transform.position-Vector3.up*phoneHeight-mapRoot.rotation*ToVector(checkpoint.Position);
            }
            Floor=checkpoint.floor;Aligned=true;LastTranslation=Time.unscaledTime;
            Message="Aligned at "+checkpoint.label;
        }
        public void Invalidate(){Aligned=false;Message="Alignment required. Stand at a known checkpoint.";}
        public void ConfirmLanding(MapNode node)
        {
            if(simulation)simPosition=ToVector(node.Position);
            else
            {
                // Correct only height. Horizontal pose remains measured, never teleported between stairs.
                float delta=node.y-Position.y;mapRoot.position-=Vector3.up*delta;
            }
            Floor=node.floor;Message="Floor confirmed: "+Floor;
        }
        void OnApplicationPause(bool paused){if(paused&&!simulation)Invalidate();}
        void Update()
        {
            if(!simulation)
            {
                if(wasTracking&&!Tracking)Invalidate();wasTracking=Tracking;
                if(!Tracking)Message="Tracking: "+ARSession.state+". "+ARSession.notTrackingReason;
                else if(!Aligned)Message="Tracking ready. Align at a known checkpoint.";
                if(Vector3.Distance(lastCamera,viewCamera.transform.position)>.015f){LastTranslation=Time.unscaledTime;lastCamera=viewCamera.transform.position;}
            }
            else if(Aligned&&allowSimulationInput&&Keyboard.current!=null)
            {
                var k=Keyboard.current;float dt=Time.unscaledDeltaTime;
                if(k.qKey.isPressed)simYaw-=80*dt;if(k.eKey.isPressed)simYaw+=80*dt;
                Vector3 local=new Vector3((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),0,(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                simPosition+=Quaternion.Euler(0,simYaw,0)*local.normalized*1.3f*dt;
                viewCamera.transform.position=simPosition+Vector3.up*phoneHeight;
                viewCamera.transform.rotation=Quaternion.Euler(18,simYaw,0);
            }
        }
        public static Vector3 ToVector(Point p){return new Vector3(p.x,p.y,p.z);}
    }
}
