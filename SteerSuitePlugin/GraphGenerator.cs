using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using SteerSuitePlugin.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteerSuitePlugin
{
    public class ExitData
    {
        public XYZ Position { get; set; }
        public XYZ Normal { get; set; }
        public List<RoomData> Rooms = new List<RoomData>();
        public ExitData(XYZ pos, params RoomData[] rooms)
        {
            Position = pos;
            Rooms.AddRange(rooms);
        }
        public float Weight1 = 1.0f;
        public float Weight2 = 1.0f;
    }

    public class RoomData
    {
        public string Number { get; set; }
        public XYZ Position { get; set; }
        public double Area { get; set; }
        public List<ExitData> Exits = new List<ExitData>();
        public List<XYZ> Vertices = new List<XYZ>();
        public Room Original { get; set; }

        public static bool operator ==(RoomData a, RoomData b)
        {
            if ((object)a == null || (object)b == null) return false;
            return a.Number == b.Number && a.Area == b.Area && GraphGenerator.posEquals(a.Position, b.Position);
        }

        public static bool operator !=(RoomData a, RoomData b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is RoomData)) return false;
            return this == (RoomData)obj;
        }
    }

    public class GraphGenerator
    {
        public List<ExitData> Exits = new List<ExitData>();
        public List<RoomData> Rooms = new List<RoomData>();
        Document document = null;

        public GraphGenerator(Document doc)
        {
            document = doc;
        }

        public void AddRoomExit(XYZ normal, XYZ exitpos, RoomData room)
        {
            bool contains = false;
            for (int i = 0; i < Rooms.Count; i++)
            {
                if (Rooms[i] == room)
                {
                    room = Rooms[i];
                    contains = true;
                    break;
                }
            }
            if (!contains) Rooms.Add(room);
            for (int i = 0; i < Exits.Count; i++)
            {
                if (posEquals(Exits[i].Position, exitpos))
                {
                    Exits[i].Rooms.Add(room);
                    room.Exits.Add(Exits[i]);
                    return;
                }
            }
            var e = new ExitData(exitpos, room);
            e.Normal = normal;
            Exits.Add(e);
            room.Exits.Add(e);
        }

        public void Final()
        {
            int count = 1;
            foreach (ExitData ed in Exits)
            {
                if(document.GetRoomAtPoint(ed.Position + (ed.Normal * 2.0)) != null && document.GetRoomAtPoint(ed.Position + (ed.Normal * 2.0)).Number == ed.Rooms[0].Number)
                {
                    ed.Normal = ed.Normal * -1.0;
                }

                if(ed.Rooms.Count == 2)
                {
                    ed.Weight1 = (float)(ed.Rooms[0].Area / (ed.Rooms[0].Area + ed.Rooms[1].Area));
                    ed.Weight2 = (float)(ed.Rooms[1].Area / (ed.Rooms[0].Area + ed.Rooms[1].Area));
                }
                if (ed.Rooms.Count == 1)
                {
                    ed.Weight1 = 0;
                    ed.Weight2 = 1;
                    RoomData rd = new RoomData();
                    rd.Number = "Outside" + count; ;
                    rd.Position = ed.Position * 1.0;
                    rd.Area = -1;
                    rd.Exits.Add(ed);
                    ed.Rooms.Add(rd);
                    this.Rooms.Add(rd);
                    count++;
                }
            }
        }

        public static bool posEquals(XYZ a, XYZ b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }
    }
}
