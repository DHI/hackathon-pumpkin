using DHI.Projections;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using DHI.Mike1D.ResultDataAccess;
using DHI.Mike1D.Generic;
using DHI.TimeSeries;

namespace DHI.Services.ARRWebPortal
{
    public static class NetworkProcess
    {
        public static Stream GetNetworkMapImage(byte[] byteArray)
        {
            Stream resultStream = new MemoryStream();
            GetBitMap(byteArray).Save(resultStream, System.Drawing.Imaging.ImageFormat.Png);
            resultStream.Position = 0;
            return resultStream;
        }

        public static Bitmap GetBitMap(byte[] byteArray)
        {
            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, byteArray);

            List<string> branchPoints = PFS.GetBranchPoints(filePath);
            List<string> points = new List<string>();
            try
            {
                points = PFS.GetPoints(filePath);
            }
            catch (Exception e)
            { }

            int xMin, yMin, xMax, yMax;

            List<int> xList, yList;
            if (points.Count > 0)
            {
                xList = points.Select(p => (int)Convert.ToDouble(p.Split(new char[] { ',' })[1])).ToList();
                yList = points.Select(p => (int)Convert.ToDouble(p.Split(new char[] { ',' })[2])).ToList();
                
            }
            else
            {
                double result1;
                List<string> pointString = String.Join(",", branchPoints).Split(new char[] { ',' }).Where(p => p.Split(new char[] { ' ' }).Length == 2 && Double.TryParse(p.Split(new char[] { ' ' })[0].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out result1) && Double.TryParse(p.Split(new char[] { ' ' })[1].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out result1)).ToList();
                xList = pointString.Select(p => Convert.ToDouble(p.Split(new char[] { ' ' })[0].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty))).Select(p => (int)p).ToList();
                yList = pointString.Select(p => Convert.ToDouble(p.Split(new char[] { ' ' })[1].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty))).Select(p => (int)p).ToList();
            }

            xMin = xList.Min();
            yMin = yList.Min();
            xMax = xList.Max();
            yMax = yList.Max();

            Bitmap bitmap1 = new Bitmap(xMax - xMin, yMax - yMin);

            using (Graphics g = Graphics.FromImage(bitmap1))
            {                
                for (int k = 0; k < branchPoints.Count; k++)
                {
                    List<Point> pointList = new List<Point>();
                    if (points.Count > 0)
                    {
                        List<int> pointNumbers = branchPoints[k].Split(new char[] { ',' }).Skip(1).Select(p => (int)Convert.ToDouble(p)).ToList();
                        for (int i = 0; i < pointNumbers.Count; i++)
                        {
                            string point = points.First(p => (int)Convert.ToDouble(p.Split(new char[] { ',' })[0]) == pointNumbers[i]);
                            int x = (int)Convert.ToDouble(point.Split(new char[] { ',' })[1]);
                            int y = (int)Convert.ToDouble(point.Split(new char[] { ',' })[2]);
                            pointList.Add(new Point(x - xMin, yMax - y));
                        }
                    }
                    else
                    {
                        List<string> pointString = branchPoints[k].Split(new char[] { ',' }).ToList();
                        for (int i = 0; i < pointString.Count; i++)
                        {
                            string point = pointString[i];
                            double xDouble, yDouble;
                            if (point.Split(new char[] { ' ' }).Length == 2 && Double.TryParse(point.Split(new char[] { ' ' })[0].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out xDouble) && Double.TryParse(point.Split(new char[] { ' ' })[1].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out yDouble))
                            {
                                int x = (int)xDouble;
                                int y = (int)yDouble;
                                pointList.Add(new Point(x - xMin, yMax - y));
                            }
                        }
                    }

                    Brush brush;
                    if (k % 6 == 0)
                    {
                        brush = Brushes.Blue;
                    }
                    else if (k % 6 == 1)
                    {
                        brush = Brushes.Red;
                    }
                    else if (k % 6 == 2)
                    {
                        brush = Brushes.Green;
                    }
                    else if (k % 6 == 3)
                    {
                        brush = Brushes.Yellow;
                    }
                    else if (k % 6 == 4)
                    {
                        brush = Brushes.Purple;
                    }
                    else if (k % 6 == 5)
                    {
                        brush = Brushes.Brown;
                    }
                    else
                    {
                        brush = Brushes.DarkGray;
                    }

                    g.DrawLines(new Pen(brush), pointList.ToArray());
                }
            }
            
            File.Delete(filePath);

            return bitmap1;
        }

        //public static JObject GetGeoJSON(byte[] byteArray)
        //{
        //    string filePath = Path.GetTempFileName();

        //    File.WriteAllBytes(filePath, byteArray);

        //    List<string> branchPoints = PFS.GetBranchPoints(filePath);
        //    List<string> points = new List<string>();
        //    try
        //    {
        //        points = PFS.GetPoints(filePath);
        //    }
        //    catch (Exception e)
        //    { }

        //    List<int> xList, yList;
        //    if (points.Count > 0)
        //    {
        //        xList = points.Select(p => (int)Convert.ToDouble(p.Split(new char[] { ',' })[1])).ToList();
        //        yList = points.Select(p => (int)Convert.ToDouble(p.Split(new char[] { ',' })[2])).ToList();
        //    }
        //    else
        //    {
        //        double result1;
        //        List<string> pointString = String.Join(",", branchPoints).Split(new char[] { ',' }).Where(p => p.Split(new char[] { ' ' }).Length == 2 && Double.TryParse(p.Split(new char[] { ' ' })[0].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out result1) && Double.TryParse(p.Split(new char[] { ' ' })[1].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out result1)).ToList();
        //        xList = pointString.Select(p => Convert.ToDouble(p.Split(new char[] { ' ' })[0].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty))).Select(p => (int)p).ToList();
        //        yList = pointString.Select(p => Convert.ToDouble(p.Split(new char[] { ' ' })[1].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty))).Select(p => (int)p).ToList();
        //    }

        //    for (int k = 0; k < branchPoints.Count; k++)
        //    {
        //        if (points.Count > 0)
        //        {
        //            List<int> pointNumbers = branchPoints[k].Split(new char[] { ',' }).Skip(1).Select(p => (int)Convert.ToDouble(p)).ToList();
        //            for (int i = 0; i < pointNumbers.Count; i++)
        //            {
        //                string point = points.First(p => (int)Convert.ToDouble(p.Split(new char[] { ',' })[0]) == pointNumbers[i]);
        //                int x = (int)Convert.ToDouble(point.Split(new char[] { ',' })[1]);
        //                int y = (int)Convert.ToDouble(point.Split(new char[] { ',' })[2]);
        //                x, y;
        //            }
        //        }
        //        else
        //        {
        //            List<string> pointString = branchPoints[k].Split(new char[] { ',' }).ToList();
        //            for (int i = 0; i < pointString.Count; i++)
        //            {
        //                string point = pointString[i];
        //                double xDouble, yDouble;
        //                if (point.Split(new char[] { ' ' }).Length == 2 && Double.TryParse(point.Split(new char[] { ' ' })[0].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out xDouble) && Double.TryParse(point.Split(new char[] { ' ' })[1].Replace("LINESTRING(", string.Empty).Replace(")", string.Empty), out yDouble))
        //                {
        //                    int x = (int)xDouble;
        //                    int y = (int)yDouble;
        //                    x, y;
        //                }
        //            }
        //        }

        //        //todo
        //    }

        //    File.Delete(filePath);

        //    return bitmap1;
        //}

        //public static string GetResultStatictics(byte[] byteArray)
        //{
        //    string filePath = Path.GetTempFileName();

        //    File.WriteAllBytes(filePath, byteArray);

        //    string result = string.Empty;

        //    TSObject tsObject = new TSObject();

        //    tsObject.Connection.Bridge = "res11 Timeseries Bridge";
        //    tsObject.Connection.FilePath = filePath;

        //    if (!tsObject.Connection.Open())
        //    {

        //    }

        //    for (int i = 1; i <= tsObject.Count; i++)
        //    {
        //        result = result + "," + tsObject.Item(i).Name + ";" + ((float[])tsObject.Item(i).GetData()).Max() + ";" + tsObject.Item(i).Origin.x + ";" + tsObject.Item(i).Origin.y;
        //    }

        //    if (tsObject != null)
        //    {
        //        Marshal.ReleaseComObject(tsObject);
        //    }

        //    GC.Collect();
        //    GC.WaitForPendingFinalizers();

        //    File.Delete(filePath);

        //    return result;
        //}

        public static JObject GetPointGeoJson(byte[] byteArray)
        {
            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, byteArray);

            Cartography cartography = new Cartography("PROJCS[\"GDA_1994_MGA_Zone_55\",GEOGCS[\"GCS_GDA_1994\",DATUM[\"D_GDA_1994\",SPHEROID[\"GRS_1980\",6378137.0,298.257222101]],PRIMEM[\"Greenwich\",0.0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"False_Easting\",500000.0],PARAMETER[\"False_Northing\",10000000.0],PARAMETER[\"Central_Meridian\",147.0],PARAMETER[\"Scale_Factor\",0.9996],PARAMETER[\"Latitude_Of_Origin\",0.0],UNIT[\"Meter\",1.0]]");

            TSObject tsObject = new TSObject();

            tsObject.Connection.Bridge = "res11 Timeseries Bridge";
            tsObject.Connection.FilePath = filePath;

            JObject jObject = new JObject();
            JArray featureCollection = new JArray();
            jObject.Add("type", "FeatureCollection");
            double lon, lat;

            try
            {
                if (tsObject.Connection.Open())
                {
                    for (int i = 1; i <= tsObject.Count; i++)
                    {
                        JObject featureObject = new JObject();
                        featureObject.Add("type", "Feature");

                        JObject geometryObject = new JObject();
                        geometryObject.Add("type", "Point");

                        string coordinates = "[";
                        cartography.Proj2Geo(tsObject.Item(i).Origin.x, tsObject.Item(i).Origin.y, out lon, out lat);
                        coordinates = coordinates + "[" + lon + ", " + lat + "],";
                        coordinates = coordinates + "]";

                        geometryObject.Add("coordinates", JToken.Parse(coordinates));
                        featureObject.Add("geometry", geometryObject);

                        JObject propertiesObject = new JObject();
                        propertiesObject.Add("Max Value", ((float[])tsObject.Item(i).GetData()).Max());
                        propertiesObject.Add("Name", tsObject.Item(i).Name);
                        propertiesObject.Add("associations", JToken.Parse("[]"));
                        propertiesObject.Add("markerType", "star");
                        JObject styleObject = new JObject();
                        styleObject.Add("width", "30px");
                        propertiesObject.Add("style", styleObject);
                        featureObject.Add("properties", propertiesObject);

                        featureCollection.Add(featureObject);
                    }

                    if (tsObject != null)
                    {
                        Marshal.ReleaseComObject(tsObject);
                    }
                }
            }
            catch (Exception e)
            {
                ResultData resultData = new ResultData();
                Cartography cart = new Cartography("PROJCS[\"UTM - 30\",GEOGCS[\"Unused\",DATUM[\"UTM Projections\",SPHEROID[\"WGS 1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.0174532925199433]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"False_Easting\",500000],PARAMETER[\"False_Northing\",0],PARAMETER[\"Central_Meridian\",-3],PARAMETER[\"Scale_Factor\",0.9996],PARAMETER[\"Latitude_Of_Origin\",0],UNIT[\"Meter\",1]]");
                resultData.Connection.BridgeName = "res1d";
                resultData.Connection.FilePath = new FilePath(filePath);
                resultData.Load(new Diagnostics("MIKE 1D Timeseries"));
                List<IDataItem> dataItems = resultData.DataItems.ToList();

                List<IRes1DNode> nodeList = resultData.Nodes.ToList();
                for (int i = 0; i < nodeList.Count; i++)
                {
                    IRes1DNode node = nodeList[i];
                    ITimeData timeData = node.DataItems[0].TimeData;
                    List<double> valueList = new List<double>();
                    for (int j = 0; j < timeData.NumberOfTimeSteps; j++)
                    {
                        valueList.Add(timeData.GetValues(j).Max());
                    }
                    double maxValue = valueList.Max();

                    JObject featureObject = new JObject();
                    featureObject.Add("type", "Feature");

                    JObject geometryObject = new JObject();
                    geometryObject.Add("type", "Point");

                    string coordinates = "[";
                    cart.Xy2Geo(node.XCoordinate, node.YCoordinate, out lon, out lat);
                    coordinates = coordinates + lon + ", " + lat;
                    coordinates = coordinates + "]";

                    geometryObject.Add("coordinates", JToken.Parse(coordinates));
                    featureObject.Add("geometry", geometryObject);

                    JObject propertiesObject = new JObject();
                    propertiesObject.Add("Max Value", maxValue);
                    propertiesObject.Add("Name", node.Id);
                    propertiesObject.Add("associations", JToken.Parse("[]"));
                    propertiesObject.Add("markerType", "star");
                    JObject styleObject = new JObject();
                    styleObject.Add("width", "30px");
                    propertiesObject.Add("style", styleObject);
                    featureObject.Add("properties", propertiesObject);

                    featureCollection.Add(featureObject);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            File.Delete(filePath);

            jObject.Add("features", featureCollection);

            return jObject;
        }
    }
}
