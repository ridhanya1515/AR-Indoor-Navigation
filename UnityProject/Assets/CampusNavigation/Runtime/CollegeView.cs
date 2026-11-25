using System.Collections.Generic;
using UnityEngine;
namespace CampusNavigation
{
    // Retains the supplied scene's geometry; upper floors are hidden for a cutaway view.
    public sealed class CollegeView : MonoBehaviour
    {
        public ARPoseSource pose;
        readonly List<Renderer> renderers=new List<Renderer>();
        readonly List<bool> original=new List<bool>();
        void Start()
        {
            renderers.AddRange(GetComponentsInChildren<Renderer>(true));
            foreach(var r in renderers)original.Add(r.enabled);
        }
        void LateUpdate()
        {
            if(pose==null)return;
            float level=pose.Floor*3.5f;
            for(int i=0;i<renderers.Count;i++)
            {
                var r=renderers[i];if(r==null)continue;
                var role=r.GetComponentInParent<CampusVisualRole>();
                bool visible=role==null?original[i]:(!role.replaced&&(role.floor<0||role.floor==pose.Floor));
                if(r.bounds.min.y>level+2.6f)visible=false;
                r.enabled=visible;
            }
        }
    }
}
