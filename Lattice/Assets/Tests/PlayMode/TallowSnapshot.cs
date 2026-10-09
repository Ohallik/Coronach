using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.Tests.PlayMode
{
    static class TallowSnapshot
    {
        static string Folder()
        {
            string requested=Environment.GetEnvironmentVariable("CORONACH_TALLOW_CAPTURE");if(string.IsNullOrEmpty(requested))return null;
            Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"Tallow capture requires a graphics-enabled test runner");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality"))+Path.DirectorySeparatorChar;
            string folder=Path.GetFullPath(requested);Assert.IsTrue((folder+Path.DirectorySeparatorChar).StartsWith(root,StringComparison.OrdinalIgnoreCase));Directory.CreateDirectory(folder);return folder;
        }
        public static void View(string name){var folder=Folder();if(folder!=null)Save(Camera.main,folder,name);}
        public static void Construction(string hero)
        {
            var folder=Folder();if(folder==null)return;var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
            bool orthographic=camera.orthographic;float size=camera.orthographicSize;
            try
            {
                camera.orthographic=true;camera.orthographicSize=40;camera.transform.SetPositionAndRotation(new Vector3(-7,95,6),Quaternion.Euler(90,0,0));Save(camera,folder,hero+"-overhead");
                camera.orthographicSize=23;camera.transform.position=new Vector3(-42,10,-45);camera.transform.LookAt(new Vector3(-12,-3,7));Save(camera,folder,hero+"-section");
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.orthographic=orthographic;camera.orthographicSize=size;}
        }
        static void Save(Camera camera,string folder,string name)
        {
            string file=Path.Combine(folder,name+".png");Assert.IsFalse(File.Exists(file),"preserve earlier images");
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var target=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                var bytes=texture.EncodeToPNG();Assert.Greater(bytes.Length,30000,"missing or implausibly empty Tallow image");File.WriteAllBytes(file,bytes);Debug.Log("CONTROLLED_TALLOW_IMAGE "+file);
            }
            finally{camera.targetTexture=target;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
