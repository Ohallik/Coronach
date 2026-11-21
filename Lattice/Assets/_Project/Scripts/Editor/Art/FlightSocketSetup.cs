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
            // -flight-hulls IdraFlight re-seats one hull's sockets without re-saving the others.
            string named=Lattice.Core.DevArgs.Value("-flight-hulls");
            foreach(string id in string.IsNullOrEmpty(named)?new[]{"TarenFlight","SelaFlight","IdraFlight"}:named.Split(','))
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
            // Idra's rescue cutter (3.6 m, the tug of the three): its prow tip measured by tools/blender/hull_sockets.py, as
            // Taren's reads back to within a centimetre of his. Its two engines are round wells sunk in the stern deck, open upward:
            // FlightHullMeasure finds them (centres -0.67/+0.70, z -1.14/-1.17) and puts each socket on its well's stern lip, 3 cm
            // over the highest plating along the flame's path aft, so the exhaust leaves the well in the open. Sockets read off by
            // eye sat low against the wells' rear outer walls, where the flame ran into the hull (IdraBodyTests).
            bool taren=id=="TarenFlight",idra=id=="IdraFlight";
            if(!taren&&!idra&&id!="SelaFlight")throw new InvalidOperationException("Uncalibrated flight hull: "+id);
            var sockets=root.GetComponent<FlightSockets>();if(sockets==null)sockets=root.AddComponent<FlightSockets>();
            Transform Socket(string name,Vector3 point,bool aft)
            {
                var socket=root.transform.Find(name);if(socket==null){socket=new GameObject(name).transform;socket.SetParent(root.transform,false);}
                socket.localPosition=point;socket.localRotation=Quaternion.Euler(0,aft?180:0,0);return socket;
            }
            sockets.muzzle=Socket("Hull muzzle",idra?new Vector3(0,.75f,1.78f):new Vector3(0,taren?.205f:.114f,1.63f),false);
            var points=taren?new[]{new Vector3(-.135f,.29f,-1.54f),new Vector3(.135f,.29f,-1.54f),new Vector3(-.40f,.305f,-1.45f),new Vector3(.40f,.305f,-1.45f)}:
                idra?new[]{new Vector3(-.673f,1.029f,-1.392f),new Vector3(.699f,1.054f,-1.416f)}:
                new[]{new Vector3(0,.22f,-.95f),new Vector3(-.285f,.145f,-1.12f),new Vector3(.285f,.145f,-1.12f)};
            sockets.engines=new Transform[points.Length];for(int i=0;i<points.Length;i++)sockets.engines[i]=Socket("Hull engine "+i,points[i],true);
        }
    }
}
