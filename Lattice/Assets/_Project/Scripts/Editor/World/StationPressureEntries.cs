using TMPro;
using UnityEditor;
using UnityEngine;

namespace Lattice.EditorTools
{
    // Show the closed inner face of the same pressure boundary used outside.
    // The three affected scenes are rebuilt from their maintained station builders.
    public static class StationPressureEntries
    {
        public static void Refine()=>BatchTools.Run(()=>
        {
            StationRedesign.BuildDecks();TallowRedesign.Build();
            AssetDatabase.SaveAssets();Debug.Log("STATION_PRESSURE_ENTRIES_OK");
        });
        public static void SealCinder()=>BatchTools.Run(()=>
        {
            StationRedesign.BuildDecks();StationRedesign.BuildExterior();
            AssetDatabase.SaveAssets();Debug.Log("CINDER_DECK_SEAMS_OK");
        });
        public static void Cinder()
        {
            foreach(float x in StationRedesign.HullX)
                Hatch("Dock pressure hatch "+x,new Vector3(x,1.6f,-20),new Vector3(4,3.2f,.6f));
        }
        public static void Tallow()
        {
            Hatch("Dock pressure lift hatch",new Vector3(0,1.6f,-12),new Vector3(3,3.2f,.5f));
            foreach(var label in Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                if(label.text=="ARRIVALS / PRESSURE HATCH")label.text="ARRIVALS / PRESSURE LIFT";
        }
        static void Hatch(string name,Vector3 position,Vector3 size)
        {
            if(GameObject.Find(name)!=null)return;
            // The builders leave a real opening for the closed panel: there is
            // no continuous wall crossing in front of the hatch.
            var frame=WorldBuilder.Piece("DeckDoorway",position,size,"Rock",false);
            StationSurfaces.Fit(frame,size);frame.name=name;
            StationSurfaces.ClosedHatch(frame,0);
        }
    }
}
