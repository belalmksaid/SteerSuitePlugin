using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Reflection;
using Autodesk.Revit.UI.Selection;

using static SteerSuitePlugin.SteerSuiteRibbon;
using static SteerSuitePlugin.Geometry;
using static SteerSuitePlugin.Property;
using Autodesk.Revit.DB.Architecture;
using System.IO;
using System.Diagnostics;

namespace SteerSuitePlugin
{
    public class SteerSuiteRibbon : IExternalApplication
    {
        public static string assembly = "";

        public Result OnStartup(UIControlledApplication application)
        {
            assembly = Assembly.GetExecutingAssembly().Location;
            RibbonPanel ribbonPanel = application.CreateRibbonPanel("Crowd Simulation");
            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            SplitButtonData pbd = new SplitButtonData("evacData", "Add Evacuation Data");
            SplitButton sb = ribbonPanel.AddItem(pbd) as SplitButton;
            addArrowButton(sb);
            addCrowdButton(sb);
            addStartButton(sb);
            return Result.Succeeded;
        }
        private void addArrowButton(SplitButton sb)
        {
            PushButtonData pbd = new PushButtonData("addarrow", "Add Evacuation Tragets", assembly, "SteerSuitePlugin.AddArrow");
            Uri uriImage = new Uri(@"C:\Users\Belal\Documents\Expression\Expression Design\arrowExit.png");
            BitmapImage largeImage = new BitmapImage(uriImage);
            pbd.LargeImage = largeImage;
            sb.AddPushButton(pbd);
        }
        private void addCrowdButton(SplitButton sb)
        {
            PushButtonData pbd = new PushButtonData("addcrowds", "Add Crowds", assembly, "SteerSuitePlugin.AddCrowd");
            Uri uriImage = new Uri(@"C:\Users\Belal\Documents\Expression\Expression Design\arrowExit.png");
            BitmapImage largeImage = new BitmapImage(uriImage);
            pbd.LargeImage = largeImage;
            sb.AddPushButton(pbd);
        }
        private void addStartButton(SplitButton sb)
        {
            PushButtonData pbd = new PushButtonData("addstart", "Start Simulation", assembly, "SteerSuitePlugin.StartSimulation");
            Uri uriImage = new Uri(@"C:\Users\Belal\Documents\Expression\Expression Design\arrowExit.png");
            BitmapImage largeImage = new BitmapImage(uriImage);
            pbd.LargeImage = largeImage;
            sb.AddPushButton(pbd);
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AddArrow : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIApplication uiapp = commandData.Application;
            Selection sel = uiapp.ActiveUIDocument.Selection;
            XYZ center = sel.PickPoint("Selection location to place flag.");
            CreateCenterbasedCylinder(doc, new XYZ(center.X, center.Y, 0), 0.2, 2, CylinderDirection.BasisZ, "flagPoint");
            //CreateSphereDirectShape(doc, center, 2, "flagPoint");
            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AddCrowd : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIApplication uiapp = commandData.Application;
            Selection sel = uiapp.ActiveUIDocument.Selection;
            XYZ center = sel.PickPoint("Selection location to place a crowd.");
            //PickedBox p = sel.PickBox(PickBoxStyle.Enclosing);
            PropertiesWindow w = new PropertiesWindow("Crowd", new Property("Crowd Size", typeof(int), 4.ToString()), new Property("Crowd Area Width", typeof(double), 5.0.ToString()), new Property("Crowd Area Breadth", typeof(double), 5.0.ToString()));
            Repeat:
            if (w.ShowDialog() != null)
            {
                var dic = w.GetValues();
                int count = dic["Crowd Size"];
                double width = dic["Crowd Area Width"] / 2.0;
                double breadth = dic["Crowd Area Breadth"] / 2.0;
                if (count / (width * breadth) > 0.2)
                {
                    TaskDialog.Show("Error", "Crowd is too dense, expand area or decrease size. Ideal density is 0.2.");
                    goto Repeat;
                }
                XYZ min = new XYZ(center.X - width, center.Y - breadth, 0);
                XYZ max = new XYZ(center.X + width, center.Y + breadth, 2);
                CreateCube(doc, min, max, "crowdPoint" + count);
            }
            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class StartSimulation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIApplication uiapp = commandData.Application;
            FilteredElementCollector finalCollector = new FilteredElementCollector(doc);
            FilteredElementCollector finalCollector2 = new FilteredElementCollector(doc);
            FilteredElementCollector finalCollector3 = new FilteredElementCollector(doc);
            IList<Element> walls = finalCollector.WherePasses(new ElementCategoryFilter(BuiltInCategory.OST_Walls)).WhereElementIsNotElementType().ToElements();
            IList<Element> elem = finalCollector2.OfCategory(BuiltInCategory.OST_GenericModel).ToElements();
            IList<Element> rooms = finalCollector3.OfCategory(BuiltInCategory.OST_Rooms).ToElements();
            var crowdPoints = elem.Where<Element>(o => o.Name.Contains("crowdPoint"));
            var flagPoints = elem.Where<Element>(o => o.Name.Contains("flagPoint"));
            GraphGenerator gg = roomstuff(rooms, doc);
            RoomRelationship rr = new RoomRelationship(gg);
            if (rr.ShowDialog() == true)
            {
                string file = exportFile(walls, crowdPoints, flagPoints, gg, doc, rr.GetGraphData());
                start(file);
            }
            return Result.Succeeded;
        }

        GraphGenerator roomstuff(IList<Element> rooms, Document doc)
        {
            GraphGenerator gg = new GraphGenerator(doc);
            foreach (Element e in rooms)
            {
                Room room = e as Room;
                SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
                options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
                IList<IList<BoundarySegment>> segments = room.GetBoundarySegments(options);
                foreach (IList<BoundarySegment> list in segments)
                {
                    foreach (BoundarySegment element in list)
                    {
                        Element sep = doc.GetElement(element.ElementId);
                        Categories cats = doc.Settings.Categories;
                        ElementId rsp = cats.get_Item(BuiltInCategory.OST_RoomSeparationLines).Id;
                        ElementId door = cats.get_Item(BuiltInCategory.OST_Doors).Id;
                        if (sep.Category.Id.Equals(rsp) || sep.Category.Id.Equals(door))
                        {
                            BoundingBoxXYZ bb = sep.get_BoundingBox(null);
                            XYZ n = bb.Max - bb.Min;
                            XYZ nn = new XYZ(n.Y, -n.X, n.Z).Normalize();
                            XYZ exitp = floorCenter(bb);
                            gg.AddRoomExit(nn, exitp, new RoomData { Number = room.Number, Area = room.Area, Position = floorCenter(room.get_BoundingBox(null)), Vertices = GetPolygon(room, doc), Original = room });
                        }
                    }
                }
            }
            gg.Final();
            return gg;
        }

        public static List<XYZ> GetPolygon(Room room, Document doc)
        {
            var vertices = new List<XYZ>();
            SpatialElementBoundaryOptions options = new SpatialElementBoundaryOptions();
            options.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
            IList<IList<BoundarySegment>> segments = room.GetBoundarySegments(options);
            foreach (IList<BoundarySegment> list in segments)
            {
                foreach (BoundarySegment bs in list)
                {
                    var revitWall = doc.GetElement(bs.ElementId) as Wall;
                    if (revitWall == null) continue; // ignore non-walls here
                    var X = bs.GetCurve().GetEndPoint(0).X;
                    var Y = bs.GetCurve().GetEndPoint(0).Y;

                    vertices.Add(new XYZ(X, Y, 0));
                }
            }
            return vertices;
        }

        XYZ floorCenter(BoundingBoxXYZ bb)
        {
            var d = 0.5 * (bb.Min + bb.Max);
            return new XYZ(d.X, d.Y, 0);
        }

        string exportFile(IList<Element> walls, IEnumerable<Element> crowdPoints, IEnumerable<Element> flagPoints, GraphGenerator data, Document doc, string graphData)
        {
            string xmlfile = "";
            BoundingBoxXYZ worldbound = getWorldBounds(walls, crowdPoints, flagPoints, doc);
            xmlfile = "<SteerBenchTestCase xmlns=\"http://www.magix.ucla.edu/steerbench\" xmlns:xsi = \"http://www.w3.org/2001/XMLSchema-instance\" xsi:schemaLocation = \"http://www.magix.ucla.edu/steerbenchTestCaseSchema.xsd\">";
            xmlfile += $@"<header>
    <version>1.0</version>
    <name>bottleneck-squeeze</name>
    <worldBounds>
      <xmin>{worldbound.Min.Y - 5}</xmin>
      <xmax>{worldbound.Max.Y + 5}</xmax>
      <ymin>0</ymin>
      <ymax>0</ymax>
      <zmin>{worldbound.Min.X - 5}</zmin>
      <zmax>{worldbound.Max.X + 5}</zmax>
    </worldBounds>
  </header>";
            XYZ worldCenter = floorCenter(worldbound);
            xmlfile += $@"<suggestedCameraView>
    <position> <x>44</x> <y>30</y> <z>0</z> </position>
    <lookat> <x>{worldCenter.X}</x> <y>0</y> <z>{worldCenter.Y}</z> </lookat>
    <up> <x>0</x> <y>1</y> <z>0</z> </up>
    <fovy>45</fovy></suggestedCameraView>";
            for (int i = 0; i < walls.Count; i++)
            {
                BoundingBoxXYZ bb = doc.GetElement(walls[i].Id).get_BoundingBox(doc.ActiveView);
                xmlfile += "<obstacle>";
                xmlfile += "<xmin>" + bb.Min.Y + "</xmin><xmax>" + bb.Max.Y + "</xmax>";
                xmlfile += "<ymin>" + bb.Min.Z + "</ymin><ymax>" + bb.Max.Z + "</ymax>";
                xmlfile += "<zmin>" + bb.Min.X + "</zmin><zmax>" + bb.Max.X + "</zmax>";
                xmlfile += "</obstacle>";

            }
            foreach (Element e in crowdPoints)
            {
                int count = Int32.Parse(e.Name.Replace("crowdPoint", ""));
                BoundingBoxXYZ bb = doc.GetElement(e.Id).get_BoundingBox(null);
                XYZ target = getTarget(data, floorCenter(bb));
                xmlfile += $@"<agentRegion>
    <numAgents>{count}</numAgents>
    <regionBounds>
      <xmin>{bb.Min.Y}</xmin>
      <xmax>{bb.Max.Y}</xmax>
      <ymin>{bb.Min.Z}</ymin>
      <ymax>{bb.Max.Z}</ymax>
      <zmin>{bb.Min.X}</zmin>
      <zmax>{bb.Max.X}</zmax>
    </regionBounds>
    <initialConditions>
      <direction> <random>true</random> </direction>
      <radius>0.5</radius>
      <speed>0</speed>
    </initialConditions>
    <goalSequence>
      <seekStaticTarget>
        <targetLocation> <x>{target.Y}</x> <y>{target.Z}</y> <z>{target.X}</z> </targetLocation>
        <desiredSpeed>1.3</desiredSpeed>
        <timeDuration>1000.0</timeDuration>
      </seekStaticTarget>
    </goalSequence>

  </agentRegion>";
            }
            xmlfile += graphData;
            xmlfile += getEvacuationTarget(flagPoints, doc);
            xmlfile += "</SteerBenchTestCase>";
            string filepath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\";
            string filename = "";
            Random rand = new Random(DateTime.Now.Millisecond);
            do
            {
                filename = "";
                for (int i = 0; i < 20; i++)
                {
                    filename += rand.Next(0, 10);
                }
            } while (File.Exists(filepath + filename + ".xml"));
            File.WriteAllText(filepath + filename + ".xml", xmlfile);
            return filepath + @"\" + filename + ".xml";
        }

        XYZ getTarget(GraphGenerator gg, XYZ crowd)
        {
            foreach (ExitData e in gg.Exits)
            {
                foreach (RoomData rd in e.Rooms)
                {
                    if (rd.Number.ToLower().Contains("outside")) return e.Position;
                }
            }
            throw new Exception("Not implemented.");
        }

        BoundingBoxXYZ getWorldBounds(IList<Element> walls, IEnumerable<Element> crowdPoints, IEnumerable<Element> flagPoints, Document doc)
        {
            BoundingBoxXYZ bb = new BoundingBoxXYZ();
            bb.Min = new XYZ(0, 0, 0);
            bb.Max = new XYZ(0, 0, 0);
            foreach (Element e in walls)
            {
                BoundingBoxXYZ eleb = doc.GetElement(e.Id).get_BoundingBox(null);
                if (eleb.Min.X < bb.Min.X) bb.Min = new XYZ(eleb.Min.X, bb.Min.Y, bb.Min.Z);
                if (eleb.Min.Y < bb.Min.Y) bb.Min = new XYZ(bb.Min.X, eleb.Min.Y, bb.Min.Z);
                if (eleb.Min.Z < bb.Min.Z) bb.Min = new XYZ(bb.Min.X, bb.Min.Y, eleb.Min.Z);
                if (eleb.Max.X > bb.Max.X) bb.Max = new XYZ(eleb.Max.X, bb.Max.Y, bb.Max.Z);
                if (eleb.Max.Y > bb.Max.Y) bb.Max = new XYZ(bb.Max.X, eleb.Max.Y, bb.Max.Z);
                if (eleb.Max.Z > bb.Max.Z) bb.Max = new XYZ(bb.Max.X, bb.Max.Y, eleb.Max.Z);
            }
            foreach (Element e in crowdPoints)
            {
                BoundingBoxXYZ eleb = doc.GetElement(e.Id).get_BoundingBox(null);
                if (eleb.Min.X < bb.Min.X) bb.Min = new XYZ(eleb.Min.X, bb.Min.Y, bb.Min.Z);
                if (eleb.Min.Y < bb.Min.Y) bb.Min = new XYZ(bb.Min.X, eleb.Min.Y, bb.Min.Z);
                if (eleb.Min.Z < bb.Min.Z) bb.Min = new XYZ(bb.Min.X, bb.Min.Y, eleb.Min.Z);
                if (eleb.Max.X > bb.Max.X) bb.Max = new XYZ(eleb.Max.X, bb.Max.Y, bb.Max.Z);
                if (eleb.Max.Y > bb.Max.Y) bb.Max = new XYZ(bb.Max.X, eleb.Max.Y, bb.Max.Z);
                if (eleb.Max.Z > bb.Max.Z) bb.Max = new XYZ(bb.Max.X, bb.Max.Y, eleb.Max.Z);
            }
            foreach (Element e in flagPoints)
            {
                BoundingBoxXYZ eleb = doc.GetElement(e.Id).get_BoundingBox(null);
                if (eleb.Min.X < bb.Min.X) bb.Min = new XYZ(eleb.Min.X, bb.Min.Y, bb.Min.Z);
                if (eleb.Min.Y < bb.Min.Y) bb.Min = new XYZ(bb.Min.X, eleb.Min.Y, bb.Min.Z);
                if (eleb.Min.Z < bb.Min.Z) bb.Min = new XYZ(bb.Min.X, bb.Min.Y, eleb.Min.Z);
                if (eleb.Max.X > bb.Max.X) bb.Max = new XYZ(eleb.Max.X, bb.Max.Y, bb.Max.Z);
                if (eleb.Max.Y > bb.Max.Y) bb.Max = new XYZ(bb.Max.X, eleb.Max.Y, bb.Max.Z);
                if (eleb.Max.Z > bb.Max.Z) bb.Max = new XYZ(bb.Max.X, bb.Max.Y, eleb.Max.Z);
            }
            return bb;
        }

        void start(string file)
        {
            String execPath = "C:\\Users\\Belal\\Documents\\GitHub\\SteerSuite\\build\\bin\\steersim.exe";
            ProcessStartInfo psi = new ProcessStartInfo(execPath, @"-testcase " + file + " -ai sfAI")
            {
                WorkingDirectory = Path.GetDirectoryName(execPath),
                //CreateNoWindow = true,
                //WindowStyle = ProcessWindowStyle.Hidden,
                //UseShellExecute = false,
                //RedirectStandardOutput = true
            };
            Process.Start(psi);
        }

        string getEvacuationTarget(IEnumerable<Element> flagPoints, Document doc) {
            string res = "<EvacuationTargets>";
            foreach(Element e in flagPoints)
            {
                BoundingBoxXYZ eleb = doc.GetElement(e.Id).get_BoundingBox(null);
                XYZ xyz = floorCenter(eleb);
                res += $@"<EvacuationTarget><Location>
<x>{xyz.Y}</x>
<y>{xyz.Z}</y>
<z>{xyz.X}</z>
</Location>
</EvacuationTarget>";
            }
            res += "</EvacuationTargets>";
            return res;
        }
    }
}