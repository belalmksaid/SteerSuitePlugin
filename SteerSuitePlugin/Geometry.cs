using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteerSuitePlugin
{
    public static class Geometry
    {
        public enum CylinderDirection
        {
            BasisX,
            BasisY,
            BasisZ
        };

        public static void CreateCenterbasedCylinder(Document doc, XYZ center, double bottomradius, double height, CylinderDirection cylinderdirection, String name)
        {
            double halfheight = height / 2.0;
            XYZ bottomcenter = new XYZ(
               cylinderdirection == CylinderDirection.BasisX ? center.X - halfheight : center.X,
               cylinderdirection == CylinderDirection.BasisY ? center.Y - halfheight : center.Y,
               cylinderdirection == CylinderDirection.BasisZ ? center.Z - halfheight : center.Z);
            XYZ topcenter = new XYZ(
               cylinderdirection == CylinderDirection.BasisX ? center.X + halfheight : center.X,
               cylinderdirection == CylinderDirection.BasisY ? center.Y + halfheight : center.Y,
               cylinderdirection == CylinderDirection.BasisZ ? center.Z + halfheight : center.Z);

            CurveLoop sweepPath = new CurveLoop();
            sweepPath.Append(Line.CreateBound(bottomcenter,
               topcenter));

            List<CurveLoop> profileloops = new List<CurveLoop>();
            CurveLoop profileloop = new CurveLoop();
            Ellipse cemiEllipse1 = Ellipse.Create(bottomcenter, bottomradius, bottomradius,
               cylinderdirection == CylinderDirection.BasisX ? Autodesk.Revit.DB.XYZ.BasisY : Autodesk.Revit.DB.XYZ.BasisX,
               cylinderdirection == CylinderDirection.BasisZ ? Autodesk.Revit.DB.XYZ.BasisY : Autodesk.Revit.DB.XYZ.BasisZ,
               -Math.PI, 0);
            Ellipse cemiEllipse2 = Ellipse.Create(bottomcenter, bottomradius, bottomradius,
               cylinderdirection == CylinderDirection.BasisX ? Autodesk.Revit.DB.XYZ.BasisY : Autodesk.Revit.DB.XYZ.BasisX,
               cylinderdirection == CylinderDirection.BasisZ ? Autodesk.Revit.DB.XYZ.BasisY : Autodesk.Revit.DB.XYZ.BasisZ,
               0, Math.PI);
            profileloop.Append(cemiEllipse1);
            profileloop.Append(cemiEllipse2);
            profileloops.Add(profileloop);
            Solid cyl = GeometryCreationUtilities.CreateSweptGeometry(sweepPath, 0, 0, profileloops);
            using (Transaction t = new Transaction(doc, "Create cylinder direct shape"))
            {
                t.Start();
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new GeometryObject[] { cyl });
                ds.Name = name;
                t.Commit();
            }
        }

        public static void CreateSphereDirectShape(Document doc, XYZ center, float radius, string name)
        {
            List<Curve> profile = new List<Curve>();

            // first create sphere with 2' radius
            XYZ profile00 = center;
            XYZ profilePlus = center + new XYZ(0, radius, 0);
            XYZ profileMinus = center - new XYZ(0, radius, 0);

            profile.Add(Line.CreateBound(profilePlus, profileMinus));
            profile.Add(Arc.Create(profileMinus, profilePlus, center + new XYZ(radius, 0, 0)));

            CurveLoop curveLoop = CurveLoop.Create(profile);
            SolidOptions options = new SolidOptions(ElementId.InvalidElementId, ElementId.InvalidElementId);

            Frame frame = new Frame(center, XYZ.BasisX, -XYZ.BasisZ, XYZ.BasisY);
            Solid sphere = GeometryCreationUtilities.CreateRevolvedGeometry(frame, new CurveLoop[] { curveLoop }, 0, 2 * Math.PI, options);
            using (Transaction t = new Transaction(doc, "Create sphere direct shape"))
            {
                t.Start();
                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.SetShape(new GeometryObject[] { sphere });
                ds.Name = name;
                t.Commit();
            }

        }

        public static void CreateTessellatedShape(Document doc, ElementId materialId, string name)
        {
            List<XYZ> loopVertices = new List<XYZ>(4);

            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();

            builder.OpenConnectedFaceSet(true);
            // create a pyramid with a square base 4' x 4' and 5' high
            double length = 4.0;
            double height = 5.0;

            XYZ basePt1 = XYZ.Zero;
            XYZ basePt2 = new XYZ(length, 0, 0);
            XYZ basePt3 = new XYZ(length, length, 0);
            XYZ basePt4 = new XYZ(0, length, 0);
            XYZ apex = new XYZ(length / 2, length / 2, height);

            loopVertices.Add(basePt1);
            loopVertices.Add(basePt2);
            loopVertices.Add(basePt3);
            loopVertices.Add(basePt4);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt1);
            loopVertices.Add(apex);
            loopVertices.Add(basePt2);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt2);
            loopVertices.Add(apex);
            loopVertices.Add(basePt3);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt3);
            loopVertices.Add(apex);
            loopVertices.Add(basePt4);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(basePt4);
            loopVertices.Add(apex);
            loopVertices.Add(basePt1);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            builder.CloseConnectedFaceSet();

            builder.Build();
            TessellatedShapeBuilderResult result = builder.GetBuildResult();

            using (Transaction t = new Transaction(doc, "Create tessellated direct shape"))
            {
                t.Start();

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.Name = name;
                ds.SetShape(result.GetGeometricalObjects());
                t.Commit();
            }
        }

        public static void CreateCube(Document doc, XYZ min, XYZ max, string name)
        {
            List<XYZ> loopVertices = new List<XYZ>(4);

            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();

            builder.OpenConnectedFaceSet(true);

            XYZ topLeftBack = new XYZ(min.X, max.Y, min.Z);
            XYZ topRightBack = new XYZ(max.X, max.Y, min.Z);
            XYZ bottomLeftBack = new XYZ(min.X, min.Y, min.Z); //min
            XYZ bottomRightBack = new XYZ(max.X, min.Y, min.Z);

            XYZ topLeftFront = new XYZ(min.X, max.Y, max.Z);
            XYZ topRightFront = new XYZ(max.X, max.Y, max.Z);  //max  
            XYZ bottomLeftFront = new XYZ(min.X, min.Y, max.Z);
            XYZ bottomRightFront = new XYZ(max.X, min.Y, max.Z);


            //Create the material
            ElementId materialId = ElementId.InvalidElementId;
            loopVertices.Add(topLeftBack);
            loopVertices.Add(topRightBack);
            loopVertices.Add(topRightFront);
            loopVertices.Add(topLeftFront);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(bottomLeftBack);
            loopVertices.Add(bottomRightBack);
            loopVertices.Add(bottomRightFront);
            loopVertices.Add(bottomLeftFront);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(topLeftBack);
            loopVertices.Add(topLeftFront);
            loopVertices.Add(bottomLeftFront);
            loopVertices.Add(bottomLeftBack);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(topLeftBack);
            loopVertices.Add(topLeftFront);
            loopVertices.Add(bottomRightFront);
            loopVertices.Add(bottomRightBack);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(topLeftBack);
            loopVertices.Add(topRightBack);
            loopVertices.Add(bottomRightBack);
            loopVertices.Add(bottomLeftBack);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            loopVertices.Clear();
            loopVertices.Add(topLeftFront);
            loopVertices.Add(topRightFront);
            loopVertices.Add(bottomRightFront);
            loopVertices.Add(bottomLeftFront);
            builder.AddFace(new TessellatedFace(loopVertices, materialId));

            builder.CloseConnectedFaceSet();

            builder.Build();
            TessellatedShapeBuilderResult result = builder.GetBuildResult();

            using (Transaction t = new Transaction(doc, "Create tessellated direct shape"))
            {
                t.Start();

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.Name = name;
                ds.SetShape(result.GetGeometricalObjects());
                t.Commit();
            }
        }


    }
}
