using System;
using Lattice.Combat;
using UnityEditor;
using UnityEngine;
namespace Lattice.EditorTools
{
    public static class FlightSocketSetup
    {
        public static void Install()=>BatchTools.Run(()=>
        {
            foreach(string id in new[]{"TarenFlight","SelaFlight"})
            {
                string path="Assets/_Project/Prefabs/Characters/"+id+".prefab",guid=AssetDatabase.AssetPathToGUID(path);
                var root=PrefabUtility.LoadPrefabContents(path);
                try{Configure(root,id);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
                if(AssetDatabase.AssetPathToGUID(path)!=guid)throw new InvalidOperationException("Hull GUID changed: "+id);
            }
            AssetDatabase.SaveAssets();Debug.Log("FLIGHT_SOCKETS_OK");
        });
        public static void Configure(GameObject root,string id)
        {
            bool taren=id=="TarenFlight";
            if(!taren&&id!="SelaFlight")throw new InvalidOperationException("Uncalibrated flight hull: "+id);
            var sockets=root.GetComponent<FlightSockets>();if(sockets==null)sockets=root.AddComponent<FlightSockets>();
            Transform Socket(string name,Vector3 point,bool aft)
            {
                var socket=root.transform.Find(name);if(socket==null){socket=new GameObject(name).transform;socket.SetParent(root.transform,false);}
                socket.localPosition=point;socket.localRotation=Quaternion.Euler(0,aft?180:0,0);return socket;
            }
            sockets.muzzle=Socket("Hull muzzle",new Vector3(0,taren?.205f:.114f,1.63f),false);
            var points=taren?new[]{new Vector3(-.135f,.29f,-1.54f),new Vector3(.135f,.29f,-1.54f),new Vector3(-.40f,.305f,-1.45f),new Vector3(.40f,.305f,-1.45f)}:
                new[]{new Vector3(0,.22f,-.95f),new Vector3(-.285f,.145f,-1.12f),new Vector3(.285f,.145f,-1.12f)};
            sockets.engines=new Transform[points.Length];for(int i=0;i<points.Length;i++)sockets.engines[i]=Socket("Hull engine "+i,points[i],true);
        }
    }
}
