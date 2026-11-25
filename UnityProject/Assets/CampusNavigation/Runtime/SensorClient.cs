using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
namespace CampusNavigation
{
    [Serializable] public class SensorPacket
    {public string sensorId,edgeId,bootId;public long sequence;public bool valid,blocked;public float distanceCm;public int ageMs;}
    public sealed class SensorClient : MonoBehaviour
    {
        public NavGraph graph;public string key="";public string Status {get;private set;}="Hardware disabled in map.";
        public bool Demo {get;private set;}
        class State{public SensorConfig config;public bool blocked,known;public float seen=-100;public string boot;public long seq=-1;public string detail="Waiting";}
        readonly List<State> states=new List<State>();
        readonly HashSet<string> demoBlocked=new HashSet<string>();
        public void Initialize(NavGraph value)
        {
            enabled=true;StopAllCoroutines();states.Clear();demoBlocked.Clear();graph=value;Demo=false;
            foreach(var c in graph.map.sensors??new SensorConfig[0])if(c.enabled)states.Add(new State{config=c});
            Aggregate();foreach(var s in states)StartCoroutine(Poll(s));
        }
        IEnumerator Poll(State s)
        {
            while(true)
            {
                using(var req=UnityWebRequest.Get(s.config.url))
                {
                    req.timeout=2;if(!string.IsNullOrEmpty(key))req.SetRequestHeader("X-Sensor-Key",key);
                    yield return req.SendWebRequest();
                    if(req.result==UnityWebRequest.Result.Success)
                    {
                        try
                        {
                            var p=JsonUtility.FromJson<SensorPacket>(req.downloadHandler.text);
                            if(p==null||p.sensorId!=s.config.id||p.edgeId!=s.config.edgeId||string.IsNullOrEmpty(p.bootId)||p.sequence<0||p.ageMs<0||p.ageMs>2000)throw new Exception("Invalid sensor identity or stale sample");
                            if(s.boot==p.bootId&&p.sequence<=s.seq)throw new Exception("Repeated sample");
                            s.boot=p.bootId;s.seq=p.sequence;s.seen=Time.unscaledTime;s.known=p.valid;
                            s.blocked=p.blocked;s.detail=p.valid?(p.blocked?"BLOCKED":"CLEAR")+" "+p.distanceCm.ToString("F0")+" cm":"Sensor has no valid echo";
                        }
                        catch(Exception e){s.detail=e.Message;}
                    }
                    else s.detail="Offline / key rejected";
                }
                Aggregate();yield return new WaitForSecondsRealtime(.5f);
            }
        }
        void Update(){if(graph!=null)Aggregate();}
        void Aggregate()
        {
            if(graph==null)return;
            foreach(var edge in graph.edges.Keys)
            {
                bool block=demoBlocked.Contains(edge);
                foreach(var s in states.Where(s=>s.config.edgeId==edge))
                {
                    bool fresh=s.known&&Time.unscaledTime-s.seen<4;
                    block|=fresh?s.blocked:s.config.failClosed;
                }
                graph.SetBlocked(edge,block);
            }
            Status=states.Count==0?"No hardware enabled. Use Settings to enable ESP32.":string.Join(" | ",states.Select(s=>s.config.id+": "+(Time.unscaledTime-s.seen>=4?"STALE (excluded)":s.detail)));
            if(Demo)Status="SIMULATED BLOCKAGE | "+Status;
        }
        public void ToggleDemo(string edge){Demo=true;if(!demoBlocked.Add(edge))demoBlocked.Remove(edge);Aggregate();}
        public void Shutdown(){StopAllCoroutines();enabled=false;}
        public void ClearDemo(){Demo=false;demoBlocked.Clear();Aggregate();}
    }
}
