using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfs123;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Projections;

using GeoAPI.Geometries;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using DHI.Generic.MikeZero;

namespace DHI.Services.ARRWebPortal
{
    public class GridProcess
    {
        public static Stream CreateIndexFile(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            Dfs2File dfs2 = DfsFileFactory.Dfs2FileOpenEdit(filePath);

            float landValue;

            if (dfs2.FileInfo.CustomBlocks[0].ToArray().Length >= 4)
            {
                landValue = (float)dfs2.FileInfo.CustomBlocks[0][3];
            }
            else if (dfs2.FileInfo.CustomBlocks.ToArray().Length >= 2 && dfs2.FileInfo.CustomBlocks[1].ToArray().Length >= 4)
            {
                landValue = (float)dfs2.FileInfo.CustomBlocks[1][3];
            }
            else if (dfs2.FileInfo.CustomBlocks.ToArray().Length >= 3 && dfs2.FileInfo.CustomBlocks[1].ToArray().Length >= 4)
            {
                landValue = (float)dfs2.FileInfo.CustomBlocks[2][3];
            }
            else
            {
                throw new Exception("Did not get land value from custom blocks!");
            }

            IDfsItemData2D<float> bathyData = (IDfsItemData2D<float>)dfs2.ReadItemTimeStepNext();

            // Modify bathymetry data based on land value
            for (int i = 0; i < bathyData.Data.Length; i++)
            {
                if (bathyData.Data[i] >= landValue)
                {
                    bathyData.Data[i] = 0;
                }
                else
                {
                    bathyData.Data[i] = 1;
                }
            }

            // Modify bathymetry data based on boundaries
            int width = dfs2.SpatialAxis.SizeOfDimension(1);
            int height = dfs2.SpatialAxis.SizeOfDimension(2);

            foreach (int xIndex in new List<int>() { 0, 1, width - 2, width - 1 })
            {
                for (int yIndex = 0; yIndex < height; yIndex++)
                {
                    bathyData.Data[xIndex + (height - yIndex - 1) * (width)] = 0;
                }
            }
            for (int xIndex = 0; xIndex < width; xIndex++)
            {
                foreach (int yIndex in new List<int>() { 0, 1, height - 2, height - 1 })
                {
                    bathyData.Data[xIndex + (height - yIndex - 1) * (width)] = 0;
                }
            }
            // Write back bathymetry data
            dfs2.WriteItemTimeStep(1, 0, 0, bathyData.Data);
            dfs2.Close();

            byte[] byteArray = File.ReadAllBytes(filePath);

            File.Delete(filePath);

            return new MemoryStream(byteArray);
        }

        public static string CreateCsv(Stream stream, int timeStep = 0)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            
            string filePath = Path.Combine(Definition.FolderPrefix, "plot.dfs2");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            Dfs2File dfs2 = DfsFileFactory.Dfs2FileOpen(filePath);
            
            IDfsItemData2D<float> bathyData = (IDfsItemData2D<float>)dfs2.ReadItemTimeStep(1, timeStep % dfs2.FileInfo.TimeAxis.NumberOfTimeSteps );

            int width = dfs2.SpatialAxis.SizeOfDimension(1);
            int height = dfs2.SpatialAxis.SizeOfDimension(2);

            float deleteValue = dfs2.FileInfo.DeleteValueFloat;

            string path = Definition.PlotPath;
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            int everyNthCell = width > 1000 || height > 1000 ? 10 : 1;

            StreamWriter writer = new StreamWriter(path);

            string line = string.Empty;
            for (var x = 0; x < width; x++)
            {
                if (x % everyNthCell == 0)
                {
                    line = line + "," + (x / everyNthCell);
                }
            }
            //writer.WriteLine(line.TrimEnd(new char[] { ',' }));
            writer.WriteLine(line);

            for (var y = 0; y < height; y++)
            {
                if (y % everyNthCell == 0)
                {
                    line = (y / everyNthCell).ToString();
                    for (var x = 0; x < width; x++)
                    {
                        if (x % everyNthCell == 0)
                        {
                            line = line + "," + bathyData[x, y];
                        }
                    }
                    writer.WriteLine(line);
                }
            }
            writer.Close();

            dfs2.Close();
            
            JObject jObject = new JObject();
            jObject.Add("Message", Definition.IndexFileType.dfs2.ToString());
            return jObject.ToString();
        }

        public static List<double> GetZones(Stream stream)
        {
            List<double> result = new List<double>();

            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            Dfs2File dfs2 = DfsFileFactory.Dfs2FileOpen(filePath);

            try
            {
                IDfsItemData2D<float> bathyData = (IDfsItemData2D<float>)dfs2.ReadItemTimeStepNext();

                // Modify bathymetry data
                for (int i = 0; i < bathyData.Data.Length; i++)
                {
                    result.Add((double)bathyData.Data[i]);
                }
                
                return result;
            }
            finally
            {
                dfs2.Close();
                File.Delete(filePath);
            }
        }

        public static List<double> GetDistinctZones(Stream stream)
        {
            List<double> result = GetZones(stream);

            return result.Distinct().OrderBy(p => p).ToList();
        }

        public static void UpdateGrid(string filePath, double oldValue, object value, List<double> zoneList, int? zone = null)
        {
            Dfs2File dfs2 = DfsFileFactory.Dfs2FileOpenEdit(filePath);

            try
            {
                IDfsItemData2D<float> bathyData = (IDfsItemData2D<float>)dfs2.ReadItemTimeStepNext();

                // Modify bathymetry data
                for (int i = 0; i < bathyData.Data.Length; i++)
                {
                    if (zone == null)
                    {
                        if (bathyData.Data[i] == (float)oldValue && zoneList[i] != 0)
                        {
                            bathyData.Data[i] = (value is List<double> ? Convert.ToSingle(((List<double>)value)[i]) : Convert.ToSingle(value));
                        }
                    }
                    else
                    {
                        if (zoneList[i] == zone)
                        {
                            bathyData.Data[i] = bathyData.Data[i] + (value is List<double> ? Convert.ToSingle(((List<double>)value)[i]) : Convert.ToSingle(value));
                        }
                    }
                }

                // Write back bathymetry data
                dfs2.WriteItemTimeStep(1, 0, 0, bathyData.Data);
            }
            finally
            {
                dfs2.Close();
            }
        }

        public static Stream GetIndexMapImage(byte[] byteArray)
        {
            List<double> result = new List<double>();
            
            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, byteArray);
        
            List<double> dfs2ValueList = GetZones(new MemoryStream(byteArray));

            List<string> extentList = GetSpatialExtent(new MemoryStream(byteArray));

            Bitmap bitmap1 = new Bitmap(Convert.ToInt32(extentList[0]), Convert.ToInt32(extentList[1]));

            for (int xIndex = 0; xIndex < bitmap1.Width; xIndex++)
            {
                for (int yIndex = 0; yIndex < bitmap1.Height; yIndex++)
                {
                    double value = dfs2ValueList[xIndex + (bitmap1.Height - yIndex - 1) * (bitmap1.Width)];
                    Color color;
                    if (value == 0)
                    {
                        color = Color.Blue;
                    }
                    else if (value == 1)
                    {
                        color = Color.Red;
                    }
                    else if (value == 2)
                    {
                        color = Color.Green;
                    }
                    else if (value == 3)
                    {
                        color = Color.Yellow;
                    }
                    else if (value == 4)
                    {
                        color = Color.Purple;
                    }
                    else if (value == 5)
                    {
                        color = Color.Brown;
                    }
                    else
                    {
                        color = Color.DarkGray;
                    }
                    bitmap1.SetPixel(xIndex, yIndex, color);
                }
            }
            Stream resultStream = new MemoryStream();
            bitmap1.Save(resultStream, System.Drawing.Imaging.ImageFormat.Png);
            resultStream.Position = 0;
            return resultStream;
        }

        public static JObject GetGeoJSON(byte[] byteArray)
        {
            List<double> result = new List<double>();

            string filePath = Path.GetTempFileName();
            File.WriteAllBytes(filePath, byteArray);

            List<double> dfs2ValueList = GetZones(new MemoryStream(byteArray));
            List<string> extentList = GetSpatialExtent(new MemoryStream(byteArray));

            JObject jObject = new JObject();
            JArray featureCollection = new JArray();

            jObject.Add("type", "FeatureCollection");

            int width = Convert.ToInt32(extentList[0]);
            int height = Convert.ToInt32(extentList[1]);
            double deltaX = Convert.ToDouble(extentList[2]);
            double deltaY = Convert.ToDouble(extentList[3]);
            double originX = Convert.ToDouble(extentList[4]);
            double originY = Convert.ToDouble(extentList[5]);

            Cartography cartography = new Cartography(extentList[6], Convert.ToDouble(extentList[7]), Convert.ToDouble(extentList[8]), Convert.ToDouble(extentList[9]));

            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            double lon, lat;
            for (int yIndex = 0; yIndex <= height; yIndex++)
            {
                cartography.Xy2Geo(originX, originY + (deltaY * yIndex) - (deltaY / 2), out lon, out lat);
                yList.Add(lat);
            }
            for (int xIndex = 0; xIndex <= width; xIndex++)
            {
                cartography.Xy2Geo(originX + (deltaX * xIndex) - (deltaX / 2), originY, out lon, out lat);
                xList.Add(lon);
            }

            //hack
            xList = new List<double>() { xList.Min(), xList.Max() };
            yList = new List<double>() { yList.Min(), yList.Max() };
            for (int yIndex = 0; yIndex < yList.Count - 1; yIndex++)
            {
                for (int xIndex = 0; xIndex < xList.Count - 1; xIndex++)
                {
            //for (int yIndex = 0; yIndex < yList.Count - 1; yIndex++)
            //{
            //    for (int xIndex = 0; xIndex < xList.Count - 1; xIndex++)
            //    {
                    //double value = dfs2ValueList[xIndex + (bitmap1.Height - yIndex - 1) * (bitmap1.Width)];
                    JObject featureObject = new JObject();
                    featureObject.Add("type", "Feature");

                    JObject geometryObject = new JObject();
                    geometryObject.Add("type", "Polygon");
                    double x = originX + (deltaX * xIndex);
                    double y = originY + (deltaY * yIndex);

                    string coordinates = "[[";
                    coordinates = coordinates + "[" + xList[xIndex] + ", " + yList[yIndex] + "]";
                    coordinates = coordinates + ",[" + xList[xIndex] + ", " + yList[yIndex + 1] + "]";
                    coordinates = coordinates + ",[" + xList[xIndex + 1] + ", " + yList[yIndex + 1] + "]";
                    coordinates = coordinates + ",[" + xList[xIndex + 1] + ", " + yList[yIndex] + "]";
                    coordinates = coordinates + ",[" + xList[xIndex] + ", " + yList[yIndex] + "]]]";
                    geometryObject.Add("coordinates", JToken.Parse(coordinates));
                    featureObject.Add("geometry", geometryObject);

                    JObject propertiesObject = new JObject();
                    propertiesObject.Add("trykzoneom", "Zone 1");
                    propertiesObject.Add("ids", "1.406");
                    propertiesObject.Add("associations", JToken.Parse("[]"));
                    featureObject.Add("properties", propertiesObject);

                    featureCollection.Add(featureObject);
                }
            }
            jObject.Add("features", featureCollection);
                
            return jObject;
        }

        public static void Stream2File(Stream stream, string filePath)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            File.WriteAllBytes(filePath, memoryStream.ToArray());
        }

        public static bool CompareSpatialExtent(Dictionary<string, string> queryParameters, Stream stream)
        {
            List<string> resultNew = GetSpatialExtent(stream);

            string dataBase64String = Dfs0SqlCache.GetDfs0(queryParameters["user"], queryParameters["filename"], queryParameters["type"]);
            List<string> resultOld = GridProcess.GetSpatialExtent(dataBase64String);
                        
            for (int i = 0; i < resultNew.Count; i++)
            {
                if (resultNew[i] != resultOld[i])
                {
                    return false;
                }
            }
            if (resultNew.Count != resultOld.Count)
            {
                return false;
            }

            return true;
        }

        public static List<string> GetSpatialExtent(string base64String)
        {
            string filePath = Path.ChangeExtension(Path.GetTempFileName(), ".dfs2");
            byte[] byteArray = Convert.FromBase64String(base64String);
            File.WriteAllBytes(filePath, byteArray);
            
            try
            {
                return GetSpatialExtentCool(filePath);
            }
            finally
            {
                File.Delete(filePath);
            }
        }
        public static List<string> GetSpatialExtentCool(string filePath)
        {
            Dfs2File dfs2 = DfsFileFactory.Dfs2FileOpen(filePath);

            try
            {
                List<string> result = new List<string>();

                IDfsAxisEqD2 axisEqD2 = ((IDfsAxisEqD2)dfs2.SpatialAxis);
                result.Add(axisEqD2.XCount.ToString());
                result.Add(axisEqD2.YCount.ToString());

                result.Add(axisEqD2.Dx.ToString());
                result.Add(axisEqD2.Dy.ToString());
                result.Add(axisEqD2.X0.ToString());
                result.Add(axisEqD2.Y0.ToString());

                IDfsProjection projection = dfs2.FileInfo.Projection;
                result.Add(projection.WKTString);
                result.Add(projection.Longitude.ToString());
                result.Add(projection.Latitude.ToString());
                result.Add(projection.Orientation.ToString());

                return result;
            }
            finally
            {
                dfs2.Close();
            }
        }
        public static List<string> GetSpatialExtent(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            string newBase64String = Convert.ToBase64String(memoryStream.ToArray());
            return GetSpatialExtent(newBase64String);
        }

        public static void CreateHazardFile(string sourceFilePath, string destinationFilePath, string newItem1, string newItem2, bool zeroDelete, string newItem3 = "")
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new Exception("File does not exist: " + sourceFilePath);
            }

            if (File.Exists(destinationFilePath))
            {
                File.Delete(destinationFilePath);
            }

            var sourceFile = DfsFileFactory.Dfs2FileOpen(sourceFilePath);
            Dfs2Builder builder = Dfs2Builder.Create("", destinationFilePath, 0);

            builder.SetDataType(1);
            builder.SetGeographicalProjection(sourceFile.FileInfo.Projection);
            builder.SetTemporalAxis(sourceFile.FileInfo.TimeAxis);
            builder.SetSpatialAxis(sourceFile.SpatialAxis);
            builder.DeleteValueFloat = sourceFile.FileInfo.DeleteValueFloat;

            builder.AddDynamicItem(newItem1, eumQuantity.Create(eumItem.eumIFlowVelocity), DfsSimpleType.Float, DataValueType.Instantaneous);
            builder.AddDynamicItem(newItem2, eumQuantity.Create(eumItem.eumIFlowVelocity), DfsSimpleType.Float, DataValueType.Instantaneous);
            if (!string.IsNullOrEmpty(newItem3))
            {
                builder.AddDynamicItem(newItem3, eumQuantity.Create(eumItem.eumIFlowVelocity), DfsSimpleType.Float, DataValueType.Instantaneous);
            }

            builder.CreateFile(destinationFilePath);
            Dfs2File file = builder.GetFile();

            sourceFile.Close();
            file.Close();
        }

        public static void WriteAscii(string filePath, string itemName, int timeStepIndex, float deleteValue, string outputFolder)
        {
            string outputFile = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(filePath) + "_" + itemName + "_"+ timeStepIndex + ".asc");

            List<string> extentList = GetSpatialExtentCool(filePath);

            Dfs2File dfs2File = DfsFileFactory.Dfs2FileOpen(filePath);

            int itemIndex;
            if (!int.TryParse(itemName, out itemIndex))
            {
                itemIndex = dfs2File.ItemInfo.Select(r => r.Name).ToList().IndexOf(itemName) + 1;
            }
            if (itemIndex == 0)
            {
                throw new Exception("The item '" + itemIndex + "' does not exist. Options are " + string.Join(", ", dfs2File.ItemInfo.Select(r => r.Name)));
            }
            
            IDfsItemData2D<float> data = (IDfsItemData2D<float>)dfs2File.ReadItemTimeStep(itemIndex, timeStepIndex);
            var dfs2ValueList = data.Data;
            dfs2File.Close();
            
            int width = Convert.ToInt32(extentList[0]);
            int height = Convert.ToInt32(extentList[1]);
            double dx = Convert.ToDouble(extentList[2]);
            double dy = Convert.ToDouble(extentList[3]);

            double x0 = 0; // Convert.ToDouble(extentList[7]);
            double y0 = 0; // Convert.ToDouble(extentList[8]);
            Cartography cartography = new Cartography(extentList[6], Convert.ToDouble(extentList[7]), Convert.ToDouble(extentList[8]), Convert.ToDouble(extentList[9]));
            cartography.Geo2Proj(Convert.ToDouble(extentList[7]), Convert.ToDouble(extentList[8]), out x0, out y0);

            List<string> result = new List<string>();

            result.Add("ncols " + width);
            result.Add("nrows " + height);
            result.Add("xllcorner " + (x0 - (dx  /2)));
            result.Add("yllcorner " + (y0 - (dx / 2)));
            result.Add("cellsize " + dx);
            result.Add("NODATA_value -9999");

            for (int yIndex = 0; yIndex < height; yIndex++)
            {
                string row = string.Empty;
                for (int xIndex = 0; xIndex < width; xIndex++)
                {
                    float value = dfs2ValueList[xIndex + (height - yIndex - 1) * (width)];
                    if (value == deleteValue)
                    {
                        value = -9999;
                    }
                    row = row + " " + value;        
                }
                result.Add(row.TrimStart(new char[] { ','}));
            }

            File.WriteAllLines(outputFile, result);
        }

        public static float[] GetSpeed(string filePath, string pName, string qName, string hName, int timeStepIndex)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("File does not exist: " + filePath);
            }

            var file = DfsFileFactory.Dfs2FileOpen(filePath);

            var pItemIndex = file.ItemInfo.Select(r => r.Name).ToList().IndexOf(pName) + 1;
            if (pItemIndex == 0)
            {
                throw new Exception("The item '" + pItemIndex + "' does not exist. Options are " + string.Join(", ", file.ItemInfo.Select(r => r.Name)));
            }
            var qItemIndex = file.ItemInfo.Select(r => r.Name).ToList().IndexOf(qName) + 1;
            if (qItemIndex == 0)
            {
                throw new Exception("The item '" + qItemIndex + "' does not exist. Options are " + string.Join(", ", file.ItemInfo.Select(r => r.Name)));
            }
            var hItemIndex = file.ItemInfo.Select(r => r.Name).ToList().IndexOf(hName) + 1;
            if (hItemIndex == 0)
            {
                throw new Exception("The item '" + hItemIndex + "' does not exist. Options are " + string.Join(", ", file.ItemInfo.Select(r => r.Name)));
            }
            
            float deleteValue = file.FileInfo.DeleteValueFloat;

            IDfsItemData2D<float> pData = (IDfsItemData2D<float>)file.ReadItemTimeStep(pItemIndex, timeStepIndex);
            IDfsItemData2D<float> qData = (IDfsItemData2D<float>)file.ReadItemTimeStep(qItemIndex, timeStepIndex);
            IDfsItemData2D<float> hData = (IDfsItemData2D<float>)file.ReadItemTimeStep(hItemIndex, timeStepIndex);
            
            // Modify bathymetry data based on boundaries
            int width = file.SpatialAxis.SizeOfDimension(1);
            int height = file.SpatialAxis.SizeOfDimension(2);

            IDfsItemData2D<float> resultTemporal = new DfsItemData2D<float>(width, height);

            for (var x = 1; x < width - 1; x++)
            {
                for (var y = 1; y < height - 1; y++)
                {
                    //float uValue = (pData[x, y] == deleteValue || pData[x - 1, y] == deleteValue) ? deleteValue : (pData[x, y] / (hData[x + 1, y] + hData[x, y])) + (pData[x-1,y] / (hData[x,y] + hData[x - 1,y]));
                    //float vValue = (qData[x, y] == deleteValue || qData[x, y - 1] == deleteValue) ? deleteValue :  (qData[x, y] / (hData[x, y + 1] + hData[x, y])) + (qData[x, y - 1] / (hData[x, y] + hData[x, y - 1]));

                    //if (uValue == deleteValue || vValue == deleteValue)
                    //{
                    //    //resultTemporal[x + (height - y - 1) * (width)] = deleteValue;
                    //    resultTemporal[x, y] = deleteValue;
                    //}
                    //else
                    //{
                    //    //resultTemporal[x + (height - y - 1) * (width)] = Convert.ToSingle((Math.Sqrt(uValue * uValue + vValue * vValue)));
                    //    resultTemporal[x, y] = Convert.ToSingle((Math.Sqrt(uValue * uValue + vValue * vValue)));
                    //}

                    /////////////////
                    //dont treat delete value differently
                    //also set speed to 0 if h is zero
                    float uValue = pData[x, y] / (hData[x + 1, y] + hData[x, y]) + pData[x - 1, y] / (hData[x, y] + hData[x - 1, y]);
                    float vValue = qData[x, y] / (hData[x, y + 1] + hData[x, y]) + qData[x, y - 1] / (hData[x, y] + hData[x, y - 1]);
                    resultTemporal[x, y] = hData[x, y] == 0 || hData[x, y] == deleteValue ? 0 : Convert.ToSingle((System.Math.Sqrt(uValue * uValue + vValue * vValue)));

                    //    //////////////////
                    //    //change delete value to zero
                    //    float uValue = (pData[x, y] == deleteValue || pData[x - 1, y] == deleteValue) ? 0 : (pData[x, y] / (hData[x + 1, y] + hData[x, y])) + (pData[x - 1, y] / (hData[x, y] + hData[x - 1, y]));
                    //    float vValue = (qData[x, y] == deleteValue || qData[x, y - 1] == deleteValue) ? 0 : (qData[x, y] / (hData[x, y + 1] + hData[x, y])) + (qData[x, y - 1] / (hData[x, y] + hData[x, y - 1]));
                    //    resultTemporal[x, y] = Convert.ToSingle((Math.Sqrt(uValue * uValue + vValue * vValue)));

                    }
                }
            
            file.Close();

            return resultTemporal.Data;
        }

        public static void MakeShapeFile(string filePath, string itemName, int timeStepIndex, string outputFolder)
        {
            string outputFile = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(filePath) + "_" + itemName + "_" + timeStepIndex + ".shp");

            if (!File.Exists(filePath))
            {
                throw new Exception("File does not exist: " + filePath);
            }
            
            List<string> extentList = GetSpatialExtentCool(filePath);

            Dfs2File dfs2File = DfsFileFactory.Dfs2FileOpen(filePath);

            var itemIndex = dfs2File.ItemInfo.Select(r => r.Name).ToList().IndexOf(itemName) + 1;
            if (itemIndex == 0)
            {
                throw new Exception("The item '" + itemIndex + "' does not exist. Options are " + string.Join(", ", dfs2File.ItemInfo.Select(r => r.Name)));
            }

            IDfsItemData2D<float> data = (IDfsItemData2D<float>)dfs2File.ReadItemTimeStep(itemIndex, timeStepIndex);
            
            int countX = Convert.ToInt32(extentList[0]);
            int countY = Convert.ToInt32(extentList[1]);
            double deltaX = Convert.ToDouble(extentList[2]);
            double deltaY = Convert.ToDouble(extentList[3]);
            double originX = Convert.ToDouble(extentList[4]);
            double originY = Convert.ToDouble(extentList[5]);
            
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
            var features = new List<IFeature>();

            int index = 0;
            for (int j = 0; j < countY; j++)
            {
                for (int i = 0; i < countX; i++)
                {
                    double x = originX + (deltaX * i);
                    double y = originY + (deltaY * j);

                    float value = data[i, j];

                    var coordinates = new List<Coordinate>();
                    coordinates.Add(new Coordinate(x - (deltaX / 2), y - (deltaY / 2)));
                    coordinates.Add(new Coordinate(x - (deltaX / 2), y + (deltaY / 2)));
                    coordinates.Add(new Coordinate(x + (deltaX / 2), y + (deltaY / 2)));
                    coordinates.Add(new Coordinate(x + (deltaX / 2), y - (deltaY / 2)));
                    coordinates.Add(new Coordinate(x - (deltaX / 2), y - (deltaY / 2)));

                    var attributesTable = new AttributesTable();
                    attributesTable.AddAttribute("Index", index);
                    attributesTable.AddAttribute(itemName.Substring(0, System.Math.Min(itemName.Length, 11)), value);

                    IGeometry geometry = geometryFactory.CreatePolygon(coordinates.ToArray());
                    features.Add(new Feature(geometry, attributesTable));

                    index += 1;
                }
            }

            dfs2File.Close();
            _WriteShapeFile(outputFile, features);
        }

        private static void _WriteShapeFile(string fileName, List<IFeature> features)
        {
            var writer = new ShapefileDataWriter(fileName);
            var header = new DbaseFileHeader();
            header.NumRecords = features.Count;
            writer.Header = header;
            writer.Write(features);
        }
    }
}
