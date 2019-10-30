using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfs123;
using DHI.Generic.MikeZero.DFS.dfsu;
using DHI.Generic.MikeZero.DFS.mesh;
using DHI.Projections;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GeoAPI.Geometries;
using NetTopologySuite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace DHI.Services.ARRWebPortal
{
    public class MeshProcess
    {
        public static Stream CreateIndexFile(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            MeshFile mesh = MeshFile.ReadMesh(filePath);

            DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);
            // Setup header and geometry, copy from source file
            builder.SetNodes(mesh.X, mesh.Y, Array.ConvertAll<double, float>(mesh.Z, p => (float)p), mesh.Code);
            builder.SetElements(mesh.ElementTable);

            DfsFactory factory = new DfsFactory();
            builder.SetProjection(factory.CreateProjection(mesh.ProjectionString));
            builder.SetTimeInfo(new DateTime(2018, 1, 1), 1);

            builder.SetZUnit(eumUnit.eumUmeter);
            builder.AddDynamicItem("Bathymetry", eumQuantity.Create(eumItem.eumIBathymetry));

            string dfsuFilePath = Path.GetTempFileName();
            DfsuFile file = builder.CreateFile(dfsuFilePath);

            int[] codes = mesh.Code;

            float[] dfsuData = new float[mesh.NumberOfElements];
            for (int i = 0; i < mesh.NumberOfElements; i++)
            {
                int zoneValue = 1;
                int[] nodeNumbers = mesh.ElementTable[i];
                for (int j = 0; j < nodeNumbers.Length; j++)
                {
                    int nodeIndex = nodeNumbers[j] - 1;
                    if (codes[nodeIndex] >= 2)
                    {
                        zoneValue = 0;
                    }
                }

                dfsuData[i] = zoneValue;
            }

            file.WriteItemTimeStepNext(0, dfsuData);

            file.Close();

            byte[] byteArray = File.ReadAllBytes(dfsuFilePath);

            File.Delete(filePath);
            File.Delete(dfsuFilePath);

            return new MemoryStream(byteArray);
        }

        public static Stream CreateIndexFile(string filePath)
        {
            IDfsuFile dfsuFile = DfsuFile.Open(filePath);
            
            DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);
            // Setup header and geometry, copy from source file
            builder.SetNodes(dfsuFile.X, dfsuFile.Y, dfsuFile.Z, dfsuFile.Code);
            builder.SetElements(dfsuFile.ElementTable);

            DfsFactory factory = new DfsFactory();
            builder.SetProjection(factory.CreateProjection(dfsuFile.Projection.WKTString));
            builder.SetTimeInfo(new DateTime(2018, 1, 1), 1);

            builder.SetZUnit(eumUnit.eumUmeter);
            builder.AddDynamicItem("Bathymetry", eumQuantity.Create(eumItem.eumIBathymetry));

            string dfsuFilePath = Path.GetTempFileName();
            DfsuFile file = builder.CreateFile(dfsuFilePath);

            int[] codes = dfsuFile.Code;

            float[] dfsuData = new float[dfsuFile.NumberOfElements];
            for (int i = 0; i < dfsuFile.NumberOfElements; i++)
            {
                int zoneValue = 1;
                int[] nodeNumbers = dfsuFile.ElementTable[i];
                for (int j = 0; j < nodeNumbers.Length; j++)
                {
                    int nodeIndex = nodeNumbers[j] - 1;
                    if (codes[nodeIndex] >= 2)
                    {
                        zoneValue = 0;
                    }
                }

                dfsuData[i] = zoneValue;
            }

            file.WriteItemTimeStepNext(0, dfsuData);

            file.Close();

            byte[] byteArray = File.ReadAllBytes(dfsuFilePath);

            File.Delete(dfsuFilePath);

            return new MemoryStream(byteArray);
        }

        public static string CreateCsv(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            int numberOfNodes;
            double[] X, Y, Z;
            try
            {
                var dfsuFile = DfsuFile.Open(filePath);
                numberOfNodes = dfsuFile.NumberOfNodes;
                X = dfsuFile.X;
                Y = dfsuFile.Y;
                Z = dfsuFile.Z.Select(p => (double)p).ToArray();
            }
            catch (Exception e)
            {
                MeshFile mesh = MeshFile.ReadMesh(filePath);
                numberOfNodes = mesh.NumberOfNodes;
                X = mesh.X;
                Y = mesh.Y;
                Z = mesh.Z;
            }

            string path = Definition.PlotPath;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            
            StreamWriter writer = new StreamWriter(path);
            writer.WriteLine("x,y,z");
            for (int i = 0; i < numberOfNodes; i++)
            {
                writer.WriteLine($"{X[i]},{Y[i]},{Z[i]}");
            }

            writer.Close();
            
            File.Delete(filePath);

            JObject jObject = new JObject();
            jObject.Add("Message", Definition.IndexFileType.mesh.ToString());
            return jObject.ToString();
        }
            public static List<double> GetZones(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            IDfsuFile file = DfsuFile.Open(filePath);

            try
            {
                float[] itemTimeStepData = (float[])file.ReadItemTimeStep(1, 0).Data;

                return itemTimeStepData.Select(p => (double)p).ToList();
            }
            finally
            {
                file.Close();

                File.Delete(filePath);
            }
        }

        public static List<double> GetDistinctZones(Stream stream)
        {
            List<double> result = GetZones(stream);

            return result.Distinct().OrderBy(p => p).ToList();
        }

        public static Stream GetIndexMapImage(byte[] byteArray)
        {
            Stream resultStream = new MemoryStream();
            GetBitMap(byteArray).Save(resultStream, System.Drawing.Imaging.ImageFormat.Png);
            resultStream.Position = 0;
            return resultStream;
        }

        public static Bitmap GetBitMap(byte[] byteArray)
        {
            string filePath = Path.ChangeExtension(Path.GetTempFileName(), ".dfsu");

            File.WriteAllBytes(filePath, byteArray);

            IDfsuFile dfsuFile = DfsuFile.Open(filePath);

            float[] itemTimeStepData = (float[])dfsuFile.ReadItemTimeStep(1, 0).Data;

            int xMin = (int)dfsuFile.X.Min();
            int yMin = (int)dfsuFile.Y.Min();
            int xMax = (int)dfsuFile.X.Max();
            int yMax = (int)dfsuFile.Y.Max();

            Bitmap bitmap1 = new Bitmap(xMax - xMin, yMax - yMin);

            using (Graphics g = Graphics.FromImage(bitmap1))
            {
                for (int k = 0; k < dfsuFile.NumberOfElements; k++)
                {
                    int[] nodeNumbers = dfsuFile.ElementTable[k];

                    List<System.Drawing.Point> pointList = new List<System.Drawing.Point>();
                    for (int i = 0; i < nodeNumbers.Length; i++)
                    {
                        int nodeIndex = nodeNumbers[i] - 1;
                        pointList.Add(new System.Drawing.Point((int)dfsuFile.X[nodeIndex] - xMin, yMax - (int)dfsuFile.Y[nodeIndex]));
                    }

                    float value = itemTimeStepData[k];

                    Brush brush;
                    if (value == 0)
                    {
                        brush = Brushes.Blue;
                    }
                    else if (value == 1)
                    {
                        brush = Brushes.Red;
                    }
                    else if (value == 2)
                    {
                        brush = Brushes.Green;
                    }
                    else if (value == 3)
                    {
                        brush = Brushes.Yellow;
                    }
                    else if (value == 4)
                    {
                        brush = Brushes.Purple;
                    }
                    else if (value == 5)
                    {
                        brush = Brushes.Brown;
                    }
                    else
                    {
                        brush = Brushes.DarkGray;
                    }

                    g.FillPolygon(brush, pointList.ToArray());
                }
            }

            dfsuFile.Close();
            File.Delete(filePath);

            return bitmap1;
        }

        public static JObject GetGeoJSON(byte[] byteArray)
        {
            string filePath = Path.ChangeExtension(Path.GetTempFileName(), ".dfsu");

            File.WriteAllBytes(filePath, byteArray);

            IDfsuFile dfsuFile = DfsuFile.Open(filePath);

            Cartography cartography = new Cartography(dfsuFile.Projection.WKTString, dfsuFile.Projection.Longitude, dfsuFile.Projection.Latitude, dfsuFile.Projection.Orientation);

            //float[] itemTimeStepData = (float[])dfsuFile.ReadItemTimeStep(1, 0).Data;

            JObject jObject = new JObject();
            JArray featureCollection = new JArray();

            jObject.Add("type", "FeatureCollection");

            double lon, lat;
            for (int k = 0; k < dfsuFile.NumberOfElements; k++)
            {
                int[] nodeNumbers = dfsuFile.ElementTable[k];

                JObject featureObject = new JObject();
                featureObject.Add("type", "Feature");

                JObject geometryObject = new JObject();
                geometryObject.Add("type", "Polygon");

                string coordinates = "[[";
                for (int i = 0; i < nodeNumbers.Length; i++)
                {
                    int nodeIndex = nodeNumbers[i] - 1;
                    cartography.Xy2Geo(dfsuFile.X[nodeIndex], dfsuFile.Y[nodeIndex], out lon, out lat);
                    coordinates = coordinates + "[" + lon + ", " + lat + "],";
                }
                coordinates = coordinates + "]]";

                geometryObject.Add("coordinates", JToken.Parse(coordinates));
                featureObject.Add("geometry", geometryObject);

                JObject propertiesObject = new JObject();
                propertiesObject.Add("trykzoneom", "Zone 1");
                propertiesObject.Add("ids", "1.406");
                propertiesObject.Add("associations", JToken.Parse("[]"));
                featureObject.Add("properties", propertiesObject);

                featureCollection.Add(featureObject);
            }

            dfsuFile.Close();
            File.Delete(filePath);

            jObject.Add("features", featureCollection);

            return jObject;
        }

        public static bool CompareSpatialExtent(Dictionary<string, string> queryParameters, Stream stream)
        {
            string resultNew = GetSpatialExtent(stream);
            string dataBase64String = Dfs0SqlCache.GetDfs0(queryParameters["user"], queryParameters["filename"], queryParameters["type"]);

            string resultOld = GetSpatialExtent(new MemoryStream(Convert.FromBase64String(dataBase64String)));

            if (resultNew != resultOld)
            {
                return false;
            }

            return true;
        }

        public static string GetSpatialExtent(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.ChangeExtension(Path.GetTempFileName(), ".dfsu");

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            IDfsuFile dfsuFile = DfsuFile.Open(filePath);

            int xMin = (int)dfsuFile.X.Min();
            int yMin = (int)dfsuFile.Y.Min();
            int xMax = (int)dfsuFile.X.Max();
            int yMax = (int)dfsuFile.Y.Max();

            dfsuFile.Close();
            File.Delete(filePath);

            return xMin + "," + yMin + "," + xMax + "," + yMax;
        }

        public static int GetNumberOfElements(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            int result;
            string filePath;
            try
            {
                filePath = Path.ChangeExtension(Path.GetTempFileName(), ".dfsu");
                File.WriteAllBytes(filePath, memoryStream.ToArray());
                IDfsuFile dfsuFile = DfsuFile.Open(filePath);
                result = dfsuFile.NumberOfElements;
                dfsuFile.Close();
            }
            catch (Exception)
            {
                filePath = Path.ChangeExtension(Path.GetTempFileName(), ".mesh");
                File.WriteAllBytes(filePath, memoryStream.ToArray());
                MeshFile mesh = MeshFile.ReadMesh(filePath);
                result = mesh.NumberOfElements;
            }

            File.Delete(filePath);

            return result;
        }

        public static List<double> GetNodeZones(string filePath, List<double> zoneList)
        {
            MeshFile mesh = MeshFile.ReadMesh(filePath);

            //list to assign nodes to their parent element, being highest element
            double[] nodeList = new double[mesh.NumberOfNodes];
            for (int k = 0; k < mesh.ElementIds.Count(); k++)
            {
                int[] nodeNumbers = mesh.ElementTable[k];

                for (int i = 0; i < nodeNumbers.Length; i++)
                {
                    int nodeIndex = nodeNumbers[i] - 1; // from number to index

                    //set if zero (first time set), next only overwrite if lower giving lower non zero ones priority
                    if ((zoneList[k] == 0 && nodeList[nodeIndex] > 0) || (nodeList[nodeIndex] < zoneList[k]))
                    {
                        nodeList[nodeIndex] = zoneList[k];
                    }
                }
            }
            return nodeList.ToList();
        }

        public static void UpdateMesh(string filePath, object delta, int zone, List<double> nodeZoneList)
        {
            MeshFile mesh = MeshFile.ReadMesh(filePath);

            for (int i = 0; i < mesh.NumberOfNodes; i++)
            {
                if (nodeZoneList[i] == zone)
                {
                    mesh.Z[i] = mesh.Z[i] + (delta is List<double> ? Convert.ToSingle(((List<double>)delta)[i]) : Convert.ToSingle(delta));
                }
            }

            //List<int> alreadyRandomizeNodeList = new List<int>();

            ////get all elements for each node
            //for (int k = 0; k < mesh.ElementIds.Count(); k++)
            //{
            //    if (zoneList[k] == zone)
            //    {
            //        int[] nodeNumbers = mesh.ElementTable[k];

            //        for (int i = 0; i < nodeNumbers.Length; i++)
            //        {
            //            int nodeIndex = nodeNumbers[i] - 1; // from number to index
            //            if (!alreadyRandomizeNodeList.Any(p => p == nodeIndex))
            //            {
            //                mesh.Z[nodeIndex] = mesh.Z[nodeIndex] + (float)deltaList[k];
            //                alreadyRandomizeNodeList.Add(nodeIndex);
            //            }
            //        }
            //    }
            //}

            //////assign node vlaue to be lowest zone (give higher priority to lowered numbered zones)
            ////for (int k = 0; k < mesh.NodeIds.Count(); k++)
            ////{
            ////    int nodeId = mesh.NodeIds[k];
            ////    var tempList = nodeElements.Where(p => p.Key == nodeId && p.Value == zone).ToList();
            ////    foreach (var vp in tempList)
            ////    {
            ////        if (zoneList[vp.Value] == zone)
            ////        {
            ////            mesh.Z[nodeId] = mesh.Z[nodeId] + (deltaList[vp.Value] / tempList.Count());
            ////        }
            ////    }
            ////}

            MeshBuilder builder = new MeshBuilder();
            builder.SetNodes(mesh.X, mesh.Y, mesh.Z, mesh.Code);
            builder.SetElements(mesh.ElementTable);
            builder.SetProjection(mesh.ProjectionString);
            builder.SetEumQuantity(mesh.EumQuantity);

            MeshFile newMesh = builder.CreateMesh();
            newMesh.Write(filePath);
        }

        public static void UpdateMesh(string filePath, double oldValue, object value, List<double> zoneList, int? zone = null)
        {
            IDfsuFile dfsuFile = DfsuFile.OpenEdit(filePath);

            try
            {
                IDfsItemData<float> bathyData = (IDfsItemData<float>)dfsuFile.ReadItemTimeStepNext(); //change from 2d to not 2d :) (IDfsItemData2D<float>)dfsuFile.ReadItemTimeStepNext();

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
                dfsuFile.WriteItemTimeStep(1, 0, 0, bathyData.Data);
            }
            finally
            {
                dfsuFile.Close();
            }
        }

        public static float GetDeleteValue(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("File does not exist: " + filePath);
            }

            var file = DfsFileFactory.DfsGenericOpen(filePath);

            float result = file.FileInfo.DeleteValueFloat;

            file.Close();

            return result;
        }
        public static float[] GetSpeed(string filePath, string uItemName, string vItemName, int timeStepIndex)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("File does not exist: " + filePath);
            }

            var file = DfsFileFactory.DfsGenericOpen(filePath);
            
            var uItemIndex = file.ItemInfo.Select(r => r.Name).ToList().IndexOf(uItemName) + 1;
            if (uItemIndex == 0)
            {
                throw new Exception("The item '" + uItemIndex + "' does not exist. Options are " + string.Join(", ", file.ItemInfo.Select(r => r.Name)));
            }
            var vItemIndex = file.ItemInfo.Select(r => r.Name).ToList().IndexOf(vItemName) + 1;
            if (vItemIndex == 0)
            {
                throw new Exception("The item '" + vItemIndex + "' does not exist. Options are " + string.Join(", ", file.ItemInfo.Select(r => r.Name)));
            }
            
            float deleteValue = file.FileInfo.DeleteValueFloat;
            
            float[] uData = (float[])file.ReadItemTimeStep(uItemIndex, timeStepIndex).Data;
            float[] vData = (float[])file.ReadItemTimeStep(vItemIndex, timeStepIndex).Data;
            float[] result = (new float[uData.Length]).Select(p => deleteValue).ToArray();

            for (var j = 0; j < uData.Length; j++)
            {
                float uValue = uData[j];
                float vValue = vData[j];
                if (uValue == deleteValue || vValue == deleteValue)
                {
                    result[j] = deleteValue;
                }
                else
                {
                    result[j] = Convert.ToSingle((System.Math.Sqrt(uValue * uValue + vValue * vValue)));
                }
                //itemDataDirection[j] = Convert.ToSingle(Math.Atan2(value2, value1) / (2 * Math.PI)) * 360;
            }
            
            file.Close();

            return result;
        }
        
        public static float[] GetItem(string filePath, string itemName, int timeStepIndex)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("File does not exist: " + filePath);
            }

            var file = DfsFileFactory.DfsGenericOpen(filePath);

            var itemIndex = file.ItemInfo.Select(r => r.Name).ToList().IndexOf(itemName) + 1;
            if (itemIndex == 0)
            {
                throw new Exception("The item '" + itemIndex + "' does not exist. Options are " + string.Join(", ", file.ItemInfo.Select(r => r.Name)));
            }
            
            float[] data = (float[])file.ReadItemTimeStep(itemIndex, timeStepIndex).Data;
               
            file.Close();

            return data;
        }

        public static List<string> GetItems(string filePath)
        {
            try
            {

                if (!File.Exists(filePath))
                {
                    throw new Exception("File does not exist: " + filePath);
                }

                var file = DfsFileFactory.DfsGenericOpen(filePath);

                List<string> result = file.ItemInfo.Select(r => r.Name).ToList();

                file.Close();

                return result;
            }
            catch (Exception e)
            {
                throw new Exception("GetItems Exception: " + e.Message);
            }
        }

        public static float[] Multiply(float[] data1, float[] data2, float deleteValue)
        {
            if (data1.Length != data2.Length)
            {
                throw new Exception("Array lengths do not match");
            }

            float[] temp = new float[data1.Length];

            for (int j = 0; j < data1.Length; j++)
            {
                if (data1[j] == deleteValue || data2[j] == deleteValue)
                {
                    temp[j] = deleteValue;
                }
                else
                {
                    temp[j] = data1[j] * data2[j];
                }
            }

            return temp;
        }
        
        public static float[] GetMax(string filePath, string itemName)
        {
            if (!File.Exists(filePath))
            {
                throw new Exception("File does not exist: " + filePath);
            }

            var file = DfsFileFactory.DfsGenericOpen(filePath);
            float deleteValue = file.FileInfo.DeleteValueFloat;

            var itemIndex = file.ItemInfo.Select(r => r.Name).ToList().IndexOf(itemName) + 1;
            if (itemIndex == 0)
            {
                throw new Exception("The item '" + itemIndex + "' does not exist. Options are " + string.Join(", ", file.ItemInfo.Select(r => r.Name)));
            }

            //initialise to delete value
            float[] result = new float[file.ReadItemTimeStep(itemIndex, 0).Data.Length].Select(p => deleteValue).ToArray(); 
            for (int i = 0; i < file.FileInfo.TimeAxis.NumberOfTimeSteps; i++)
            {
                float[] tempData = (float[])file.ReadItemTimeStep(itemIndex, i).Data;

                for (int j = 0; j < result.Length; j++)
                {
                    //if not null and destination null then set
                    if (tempData[j] != deleteValue && result[j] == deleteValue)
                    {
                        result[j] = tempData[j];
                    }
                    //if source and dest not null and greater then set
                    else if (tempData[j] != deleteValue && result[j] != deleteValue && tempData[j] > result[j])
                    {
                        result[j] = tempData[j];
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            file.Close();

            return result;
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

            //var sourceFile1 = DfsFileFactory.DfsGenericOpen(sourceFilePath);
            var sourceFile = DfsFileFactory.DfsuFileOpen(sourceFilePath);
            
            DfsuBuilder builder = DfsuBuilder.Create(DfsuFileType.Dfsu2D);
            builder.SetNodes(sourceFile.X, sourceFile.Y, sourceFile.Z, sourceFile.Code);
            builder.SetElements(sourceFile.ElementTable);

            DfsFactory factory = new DfsFactory();
            builder.SetProjection(factory.CreateProjection(sourceFile.Projection.WKTString));
            builder.SetTimeInfo(sourceFile.StartDateTime, sourceFile.TimeStepInSeconds);

            builder.SetZUnit(eumUnit.eumUmeter);
            builder.AddDynamicItem(newItem1, eumQuantity.Create(eumItem.eumIFlowVelocity));
            builder.AddDynamicItem(newItem2, eumQuantity.Create(eumItem.eumIFlowVelocity));
            if (!string.IsNullOrEmpty(newItem3))
            {
                builder.AddDynamicItem(newItem3, eumQuantity.Create(eumItem.eumIFlowVelocity));
            }

            DfsuFile file = builder.CreateFile(destinationFilePath);
            
            file.Close();
            sourceFile.Close();
        }
        public static void AddItem(string sourceFilePath, string destinationFilePath, string newItem1, string newItem2, bool zeroDelete)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new Exception("File does not exist: " + sourceFilePath);
            }

            if (File.Exists(destinationFilePath))
            {
                File.Delete(destinationFilePath);
            }

            var sourceFile = DfsFileFactory.DfsGenericOpen(sourceFilePath);
            IDfsFileInfo fileInfo = sourceFile.FileInfo;
            var itemInfos = sourceFile.ItemInfo;
            
            DfsBuilder builder = DfsBuilder.Create(fileInfo.FileTitle, fileInfo.ApplicationTitle, fileInfo.ApplicationVersion);
            //Dfs2Builder builder = Dfs2Builder.Create(fileInfo.FileTitle, fileInfo.ApplicationTitle, fileInfo.ApplicationVersion);

            // Set up the header
            builder.SetDataType(fileInfo.DataType);
            builder.SetGeographicalProjection(fileInfo.Projection);
            builder.SetTemporalAxis(fileInfo.TimeAxis);
            builder.SetItemStatisticsType(fileInfo.StatsType);
            builder.DeleteValueByte = fileInfo.DeleteValueByte;
            builder.DeleteValueDouble = fileInfo.DeleteValueDouble;
            builder.DeleteValueFloat = fileInfo.DeleteValueFloat;
            builder.DeleteValueInt = fileInfo.DeleteValueInt;
            builder.DeleteValueUnsignedInt = fileInfo.DeleteValueUnsignedInt;

            // Transfer compression keys - if any.
            if (fileInfo.IsFileCompressed)
            {
                int[] xkey;
                int[] ykey;
                int[] zkey;
                fileInfo.GetEncodeKey(out xkey, out ykey, out zkey);
                builder.SetEncodingKey(xkey, ykey, zkey);
            }

            // Copy custom blocks - if any
            foreach (IDfsCustomBlock customBlock in fileInfo.CustomBlocks)
            {
                builder.AddCustomBlock(customBlock);
            }

            foreach (var itemInfo in itemInfos)
            {
                builder.AddDynamicItem(itemInfo);
            }

            DfsDynamicItemBuilder itemBuilder = builder.CreateDynamicItemBuilder();
            itemBuilder.Set(newItem1, eumQuantity.Create(eumItem.eumIFlowVelocity, eumUnit.eumUmeterPerSec), itemInfos[0].DataType);
            itemBuilder.SetAxis(itemInfos[0].SpatialAxis);
            itemBuilder.SetValueType(itemInfos[0].ValueType);
            builder.AddDynamicItem(itemBuilder.GetDynamicItemInfo());

            itemBuilder = builder.CreateDynamicItemBuilder();
            itemBuilder.Set(newItem2, eumQuantity.Create(eumItem.eumIFlowVelocity, eumUnit.eumUmeterPerSec), itemInfos[0].DataType);
            itemBuilder.SetAxis(itemInfos[0].SpatialAxis);
            itemBuilder.SetValueType(itemInfos[0].ValueType);
            builder.AddDynamicItem(itemBuilder.GetDynamicItemInfo());

            // Create file
            builder.CreateFile(destinationFilePath);

            // Copy static items - add only from main file
            IDfsStaticItem sourceStaticItem;
            while (null != (sourceStaticItem = sourceFile.ReadStaticItemNext()))
            {
                builder.AddStaticItem(sourceStaticItem);
            }
            
            // Get the file
            DfsFile destinationFile = builder.GetFile();

            // Copy dynamic item data
            //IDfsItemData sourceData;
            //double sourceTime = double.MinValue;
            //int dataLength = 0;
            //for (int i = 0; i < fileInfo.TimeAxis.NumberOfTimeSteps; i++)
            //{
            //     Copy all items for this source
            //    for (int k = 0; k < itemInfos.Count; k++)
            //    {
            //        sourceData = sourceFile.ReadItemTimeStepNext();
            //        dataLength = sourceData.Data.Length;
            //        sourceTime = sourceData.Time;
            //        destinationFile.WriteItemTimeStepNext(sourceTime, sourceData.Data);
            //    }

            //    float[] data = new float[dataLength].Select(p => fileInfo.DeleteValueFloat).ToArray();
            //    destinationFile.WriteItemTimeStepNext(sourceTime, !zeroDelete ? data : data.Select(p => p == fileInfo.DeleteValueFloat ? 0 : p).ToArray());
            //    destinationFile.WriteItemTimeStepNext(sourceTime, !zeroDelete ? data : data.Select(p => p == fileInfo.DeleteValueFloat ? 0 : p).ToArray());
            //}
            
            sourceFile.Close();
            destinationFile.Close();
        }

        public static void AddStaticAsDynamic(string sourceFilePath, string destinationFilePath, float[] static1, float[] static2, float[] static3, bool zeroDelete)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new Exception("File does not exist: " + sourceFilePath);
            }

            if (File.Exists(destinationFilePath))
            {
                File.Delete(destinationFilePath);
            }

            var sourceFile = DfsFileFactory.DfsGenericOpen(sourceFilePath);
            IDfsFileInfo fileInfo = sourceFile.FileInfo;
            var itemInfos = sourceFile.ItemInfo;

            DfsBuilder builder = DfsBuilder.Create(fileInfo.FileTitle, fileInfo.ApplicationTitle, fileInfo.ApplicationVersion);
            //Dfs2Builder builder = Dfs2Builder.Create(fileInfo.FileTitle, fileInfo.ApplicationTitle, fileInfo.ApplicationVersion);
            
            // Set up the header
            builder.SetDataType(fileInfo.DataType);
            builder.SetGeographicalProjection(fileInfo.Projection);
            builder.SetTemporalAxis(fileInfo.TimeAxis);
            builder.SetItemStatisticsType(fileInfo.StatsType);
            builder.DeleteValueByte = fileInfo.DeleteValueByte;
            builder.DeleteValueDouble = fileInfo.DeleteValueDouble;
            builder.DeleteValueFloat = fileInfo.DeleteValueFloat;
            builder.DeleteValueInt = fileInfo.DeleteValueInt;
            builder.DeleteValueUnsignedInt = fileInfo.DeleteValueUnsignedInt;

            // Transfer compression keys - if any.
            if (fileInfo.IsFileCompressed)
            {
                int[] xkey;
                int[] ykey;
                int[] zkey;
                fileInfo.GetEncodeKey(out xkey, out ykey, out zkey);
                builder.SetEncodingKey(xkey, ykey, zkey);
            }

            // Copy custom blocks - if any
            foreach (IDfsCustomBlock customBlock in fileInfo.CustomBlocks)
            {
                builder.AddCustomBlock(customBlock);
            }

            //foreach (var itemInfo in itemInfos)
            //{
            //    builder.AddDynamicItem(itemInfo);
            //}

            DfsDynamicItemBuilder itemBuilder = builder.CreateDynamicItemBuilder();
            itemBuilder.Set("Max Vxd", eumQuantity.Create(eumItem.eumIFlowVelocity, eumUnit.eumUmeterPerSec), itemInfos[0].DataType);
            itemBuilder.SetAxis(itemInfos[0].SpatialAxis);
            itemBuilder.SetValueType(itemInfos[0].ValueType);
            builder.AddDynamicItem(itemBuilder.GetDynamicItemInfo());

            itemBuilder = builder.CreateDynamicItemBuilder();
            itemBuilder.Set("Max Velocity", eumQuantity.Create(eumItem.eumIFlowVelocity, eumUnit.eumUmeterPerSec), itemInfos[0].DataType);
            itemBuilder.SetAxis(itemInfos[0].SpatialAxis);
            itemBuilder.SetValueType(itemInfos[0].ValueType);
            builder.AddDynamicItem(itemBuilder.GetDynamicItemInfo());

            itemBuilder = builder.CreateDynamicItemBuilder();
            itemBuilder.Set("Max Depth", eumQuantity.Create(eumItem.eumIWaterDepth, eumUnit.eumUmeter), itemInfos[0].DataType);
            itemBuilder.SetAxis(itemInfos[0].SpatialAxis);
            itemBuilder.SetValueType(itemInfos[0].ValueType);
            builder.AddDynamicItem(itemBuilder.GetDynamicItemInfo());

            // Create file
            builder.CreateFile(destinationFilePath);

            // Copy static items - add only from main file
            IDfsStaticItem sourceStaticItem;
            while (null != (sourceStaticItem = sourceFile.ReadStaticItemNext()))
            {
                builder.AddStaticItem(sourceStaticItem);
            }

            // Get the file
            DfsFile destinationFile = builder.GetFile();

            IDfsItemData sourceData;
            double sourceTime = double.MinValue;

            // Copy all items for this source
            for (int k = 0; k < itemInfos.Count; k++)
            {
                sourceData = sourceFile.ReadItemTimeStepNext();
                sourceTime = sourceData.Time;
            }

            destinationFile.WriteItemTimeStepNext(sourceTime, !zeroDelete ? static1 : static1.Select(p => p == fileInfo.DeleteValueFloat ? 0 : p).ToArray());
            destinationFile.WriteItemTimeStepNext(sourceTime, !zeroDelete ? static2 : static2.Select(p => p == fileInfo.DeleteValueFloat ? 0 : p).ToArray());
            destinationFile.WriteItemTimeStepNext(sourceTime, !zeroDelete ? static3 : static3.Select(p => p == fileInfo.DeleteValueFloat ? 0 : p).ToArray());

            sourceFile.Close();
            destinationFile.Close();
        }
        
        public static void AppendToFile(string inputFile, string destinationFile, int timeStepIndex, float[] data1, float[] data3, bool zeroDelete, float deleteValue)
        {
            // Open target for appending and source for reading
            IDfsFile target = DfsFileFactory.DfsGenericOpenAppend(destinationFile);
            IDfsFile source = DfsFileFactory.DfsGenericOpen(inputFile);
            
            source.FindTimeStep(timeStepIndex);

            IDfsItemData sourceData;
            double sourceTime = 0;
            for (int k = 0; k < source.ItemInfo.Count; k++)
            {
                sourceData = source.ReadItemTimeStepNext();
                sourceTime = sourceData.Time;
                target.WriteItemTimeStepNext(sourceTime, sourceData.Data);
            }
            target.WriteItemTimeStepNext(sourceTime, !zeroDelete ? data1 : data1.Select(p => p == deleteValue ? 0 : p).ToArray());
            target.WriteItemTimeStepNext(sourceTime, !zeroDelete ? data3 : data3.Select(p => p == deleteValue ? 0 : p).ToArray());

            // Close the files
            target.Close();
            source.Close();
        }

        public static void AppendToFile(string destinationFile, int timeStepIndex, float[] data1, float[] data3, bool zeroDelete, float deleteValue, float[] data4 = null)
        {
            // Open target for appending and source for reading
            IDfsFile target = DfsFileFactory.DfsGenericOpenAppend(destinationFile);

            double sourceTime = target.FileInfo.TimeAxis.TimeStepInSeconds();

            target.WriteItemTimeStepNext(sourceTime, !zeroDelete ? data1 : data1.Select(p => p == deleteValue ? 0 : p).ToArray());
            target.WriteItemTimeStepNext(sourceTime, !zeroDelete ? data3 : data3.Select(p => p == deleteValue ? 0 : p).ToArray());
            if (data4 != null)
            {
                target.WriteItemTimeStepNext(sourceTime, !zeroDelete ? data4 : data4.Select(p => p == deleteValue ? 0 : p).ToArray());
            }

            // Close the files
            target.Close();
        }

        public static int GetNumberTimeStpes(string sourceFilePath)
        {
            if (!File.Exists(sourceFilePath))
            {
                throw new Exception("File does not exist: " + sourceFilePath);
            }
            var sourceFile = DfsFileFactory.DfsGenericOpen(sourceFilePath);
            IDfsFileInfo fileInfo = sourceFile.FileInfo;
            int result = fileInfo.TimeAxis.NumberOfTimeSteps; 
            sourceFile.Close();
            return result;
        }

        public static void MakeShapeFile(string filePath, string itemName, int timeStepIndex, string outputFolder)
        {
            string outputFile = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(filePath) + "_" + itemName + "_" + timeStepIndex + ".shp");

            if (!File.Exists(filePath))
            {
                throw new Exception("File does not exist: " + filePath);
            }

            IDfsuFile dfsuFile = DfsuFile.Open(filePath);
            int itemIndex;
            if (!int.TryParse(itemName, out itemIndex))
            {
                itemIndex = dfsuFile.ItemInfo.Select(r => r.Name).ToList().IndexOf(itemName) + 1;
            }
            if (itemIndex == 0)
            {
                throw new Exception("The item '" + itemIndex + "' does not exist. Options are " + string.Join(", ", dfsuFile.ItemInfo.Select(r => r.Name)));
            }

            float[] itemTimeStepData = (float[])dfsuFile.ReadItemTimeStep(itemIndex, timeStepIndex).Data;

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory();
            var features = new List<IFeature>();
            
            for (int k = 0; k < dfsuFile.NumberOfElements; k++)
            {
                int[] nodeNumbers = dfsuFile.ElementTable[k];

                var coordinates = new List<Coordinate>();
                
                for (int i = 0; i < nodeNumbers.Length; i++)
                {
                    int nodeIndex = nodeNumbers[i] - 1;
                    coordinates.Add(new Coordinate(dfsuFile.X[nodeIndex], dfsuFile.Y[nodeIndex]));
                }
                coordinates.Add(new Coordinate(dfsuFile.X[nodeNumbers[0] - 1], dfsuFile.Y[nodeNumbers[0] - 1]));

                float value = itemTimeStepData[k];

                var attributesTable = new AttributesTable();
                attributesTable.AddAttribute("Index", k);
                attributesTable.AddAttribute(itemName.Substring(0, System.Math.Min(itemName.Length,11)), value);

                IGeometry geometry = geometryFactory.CreatePolygon(coordinates.ToArray());
                features.Add(new Feature(geometry, attributesTable));
            }

            dfsuFile.Close();
            
            _WriteShapeFile(outputFile, features);
        }

        //private static void _WriteShapeFile(string fileName, IList<IFeature> features)
        //{
        //    var writer = new ShapefileDataWriter(fileName) { Header = ShapefileDataWriter.GetHeader(features[0], features.Count) };
        //    writer.Write(features);
        //}

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