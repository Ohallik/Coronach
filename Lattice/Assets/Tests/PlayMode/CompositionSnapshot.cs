using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice.Tests.PlayMode
{
    // Optional background-editor rendering of the real controlled fixture.
    // These images cannot replace ordinary arrival or continuous player review.
    static class CompositionSnapshot
    {
        public static void Write(string hero)
        {
            string requested=Environment.GetEnvironmentVariable("CORONACH_COMPOSITION_CAPTURE");
            if(string.IsNullOrEmpty(requested))return;
            Assert.AreNotEqual(GraphicsDeviceType.Null,SystemInfo.graphicsDeviceType,"composition capture requires a graphics-enabled test runner");
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/quality"))+Path.DirectorySeparatorChar;
            string folder=Path.GetFullPath(requested);
            Assert.IsTrue((folder+Path.DirectorySeparatorChar).StartsWith(root,StringComparison.OrdinalIgnoreCase));
            Directory.CreateDirectory(folder);
            var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;
            bool orthographic=camera.orthographic;float size=camera.orthographicSize;var target=camera.targetTexture;
            try
            {
                Save(camera,folder,hero+"-preview");
                camera.orthographic=true;camera.orthographicSize=48;
                camera.transform.SetPositionAndRotation(new Vector3(0,95,805),Quaternion.Euler(90,0,0));
                Save(camera,folder,hero+"-overhead");
                camera.orthographicSize=24;camera.transform.position=new Vector3(75,20,790);camera.transform.LookAt(new Vector3(10,0,790));
                Save(camera,folder,hero+"-section");
            }
            finally
            {
                camera.transform.SetPositionAndRotation(position,rotation);camera.orthographic=orthographic;
                camera.orthographicSize=size;camera.targetTexture=target;
            }
        }
        static void Save(Camera camera,string folder,string name)
        {
            string file=Path.Combine(folder,name+".png");Assert.IsFalse(File.Exists(file),"preserve earlier images");
            var texture=new Texture2D(1920,1080,TextureFormat.RGB24,false);var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
            var active=RenderTexture.active;var previous=camera.targetTexture;
            try
            {
                Assert.IsTrue(rt.Create());camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
                texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
                var bytes=texture.EncodeToPNG();Assert.Greater(bytes.Length,30000,"missing or implausibly empty composition image");
                File.WriteAllBytes(file,bytes);Debug.Log("CONTROLLED_COMPOSITION_IMAGE "+file);
            }
            finally
            {
                camera.targetTexture=previous;RenderTexture.active=active;rt.Release();
                UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
