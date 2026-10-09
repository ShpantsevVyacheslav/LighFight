using System.Collections.Generic;
using UnityEngine;

namespace LighFight.Runtime
{
    public enum OfficeLocation { Meeting, Kitchen, OpenSpace, Washroom, Yard }
    public sealed class OfficeArena : MonoBehaviour
    {
        private readonly List<Material> materials = new List<Material>();
        [SerializeField] private bool ownsMaterials = true;
        public void UseSharedMaterials() => ownsMaterials = false;
        private Material wood, beige, white, black, metal, glass, leaf;
        public static OfficeArena Create(OfficeLocation location)
        {
            var arena = new GameObject("Office arena " + location).AddComponent<OfficeArena>();
            arena.Build(location); return arena;
        }
        private Material Mat(Color color, float smooth=.2f)
        {
            var m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",smooth);
            materials.Add(m);return m;
        }
        private Transform Box(string name, Vector3 position, Vector3 size, Material m)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;
            obj.transform.SetParent(transform,false);obj.transform.localPosition=position;obj.transform.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=m;
            // Decoration is outside the combat plane; core owns collision bounds.
            if(Application.isPlaying)Destroy(obj.GetComponent<Collider>());else DestroyImmediate(obj.GetComponent<Collider>());
            return obj.transform;
        }
        private void Table(float x, float z, float width, float depth)
        {
            Box("Wood desk",new Vector3(x,.76f,z),new Vector3(width,.09f,depth),wood);
            for(int a=-1;a<=1;a+=2)for(int b=-1;b<=1;b+=2)
                Box("Desk leg",new Vector3(x+a*(width/2-.10f),.36f,z+b*(depth/2-.10f)),new Vector3(.06f,.72f,.06f),metal);
        }
        private void Chair(float x,float z)
        {
            Box("Seat",new Vector3(x,.43f,z),new Vector3(.48f,.10f,.48f),black);
            Box("Mesh back",new Vector3(x,.78f,z+.20f),new Vector3(.46f,.64f,.07f),black);
            Box("Chrome stem",new Vector3(x,.22f,z),new Vector3(.06f,.43f,.06f),metal);
            Box("Chair foot",new Vector3(x,.06f,z),new Vector3(.58f,.06f,.08f),metal);
        }
        private void Plant(float x,float z)
        {
            Box("Terracotta pot",new Vector3(x,.22f,z),new Vector3(.30f,.40f,.30f),wood);
            for(int i=0;i<5;i++)Box("Plant leaves",new Vector3(x+(i-2)*.06f,.60f+Mathf.Sin(i)*.12f,z),new Vector3(.07f,.60f,.18f),leaf).rotation=Quaternion.Euler(0,i*35,(i-2)*16);
        }
        private void Window(float x,bool blinds)
        {
            Box("Window frame",new Vector3(x,1.85f,3.85f),new Vector3(2.35f,2.15f,.12f),white);
            Box("Daylight glass",new Vector3(x,1.85f,3.75f),new Vector3(2.15f,1.95f,.08f),glass);
            Box("Windowsill",new Vector3(x,.78f,3.65f),new Vector3(2.60f,.08f,.42f),white);
            if(blinds)for(int i=0;i<14;i++)
                Box("Vertical blind",new Vector3(x-1.05f+i*.16f,1.88f,3.64f),new Vector3(.10f,1.90f,.035f),white);
            for(int i=0;i<10;i++)Box("Radiator rib",new Vector3(x-.80f+i*.18f,.38f,3.70f),new Vector3(.10f,.60f,.16f),white);
        }
        private void Build(OfficeLocation location)
        {
            wood=Mat(new Color(.39f,.27f,.16f));beige=Mat(new Color(.68f,.59f,.44f));
            white=Mat(new Color(.89f,.87f,.81f));black=Mat(new Color(.065f,.075f,.08f));
            metal=Mat(new Color(.45f,.48f,.50f),.75f);glass=Mat(new Color(.57f,.74f,.82f),.6f);leaf=Mat(new Color(.16f,.29f,.11f));
            Box("Floor",new Vector3(0,-.10f,0),new Vector3(14,.2f,9),wood);
            for(int i=0;i<35;i++)Box("Floorboard seam",new Vector3(-7+i*.4f,.002f,0),new Vector3(.007f,.006f,9),beige);
            if(location==OfficeLocation.Yard)
            {
                Box("Courtyard wall",new Vector3(0,1.5f,4),new Vector3(14,3,.25f),beige);
                Box("Red door",new Vector3(3,1.05f,3.82f),new Vector3(1.3f,2.1f,.15f),Mat(new Color(.35f,.12f,.11f)));
                for(int i=-6;i<=6;i+=2){Plant(i,3);Box("Tree trunk",new Vector3(i,1.6f,4.4f),new Vector3(.25f,3.2f,.3f),wood);Box("Tree canopy",new Vector3(i,3.7f,4.4f),new Vector3(2.6f,2.2f,1.5f),leaf);}
                return;
            }
            Box("Beige office wall",new Vector3(0,1.7f,4),new Vector3(14,3.4f,.25f),location==OfficeLocation.Washroom?white:beige);
            Box("Left wall",new Vector3(-7,1.7f,1),new Vector3(.20f,3.4f,6),beige);
            Box("Right wall",new Vector3(7,1.7f,1),new Vector3(.20f,3.4f,6),beige);
            if(location==OfficeLocation.Washroom)
            {
                for(int i=-6;i<=6;i++)for(int j=0;j<7;j++)Box("Tile grout",new Vector3(i,j*.48f,3.84f),new Vector3(.01f,.48f,.02f),metal);
                Box("Mirror",new Vector3(-2,1.6f,3.72f),new Vector3(1.35f,.90f,.05f),metal);
                Box("Sink pedestal",new Vector3(-2,.42f,3.2f),new Vector3(.30f,.84f,.30f),white);
                Box("Sink",new Vector3(-2,.85f,3.2f),new Vector3(.85f,.18f,.55f),white);
                Box("Toilet tank",new Vector3(1.7f,.68f,3.45f),new Vector3(.50f,.65f,.25f),white);
                Box("Toilet seat",new Vector3(1.7f,.40f,3),new Vector3(.55f,.15f,.70f),white);return;
            }
            Window(-3.9f,true);Window(3.9f,true);
            Box("Suspended ceiling edge",new Vector3(0,3.35f,3.7f),new Vector3(14,.10f,.7f),white);
            Box("Air conditioner",new Vector3(-3.9f,3.0f,3.7f),new Vector3(1.5f,.32f,.28f),white);
            if(location==OfficeLocation.Kitchen)
            {
                Box("Fridge",new Vector3(-.8f,1.02f,3.1f),new Vector3(.86f,2.04f,.80f),white);
                Box("Fridge handle",new Vector3(-.44f,1.30f,2.65f),new Vector3(.04f,.40f,.06f),metal);
                Box("Coffee cabinet",new Vector3(-5,.45f,2.8f),new Vector3(1.2f,.90f,.7f),wood);
                Box("Coffee machine",new Vector3(-5,1.18f,2.8f),new Vector3(.50f,.55f,.45f),black);
                Table(2,2.7f,3.5f,.9f);Chair(1,2);Chair(3,2);
            }
            else if(location==OfficeLocation.Meeting)
            {
                Table(0,2.8f,4.4f,1.3f);for(int x=-2;x<=2;x+=2)Chair(x,1.8f);
                Box("Whiteboard",new Vector3(0,1.95f,3.72f),new Vector3(1.8f,1.0f,.08f),white);
                for(int i=0;i<5;i++)Box("Whiteboard notes",new Vector3(-.65f+i*.28f,2.1f,3.66f),new Vector3(.13f,.045f,.01f),leaf);
                Plant(1.5f,3.2f);
            }
            else
            {
                for(int x=-5;x<=5;x+=5){Table(x,2.6f,2.8f,1);Chair(x,1.7f);
                    Box("Laptop base",new Vector3(x,.84f,2.6f),new Vector3(.7f,.035f,.45f),metal);
                    Box("Laptop screen",new Vector3(x,1.08f,2.8f),new Vector3(.70f,.47f,.035f),black);
                    Box("Display",new Vector3(x,1.08f,2.77f),new Vector3(.63f,.40f,.01f),glass);}
                Plant(6,3);Plant(-6,3);
            }
        }
        private void OnDestroy(){if(!ownsMaterials)return;foreach(var material in materials)if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}
    }
}
