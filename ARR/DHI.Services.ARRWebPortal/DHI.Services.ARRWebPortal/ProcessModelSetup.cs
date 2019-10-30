using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace DHI.Services.ARRWebPortal
{
    public static class ProcessModelSetup
    {        
        public static List<KeyValuePair<string,Stream>> GetZipEtries(Stream stream)
        {
            List<KeyValuePair<string,Stream>> result = new List<KeyValuePair<string,Stream>>();

            ZipArchive zipArchive = new ZipArchive(stream);

            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                result.Add(new KeyValuePair<string, Stream>(entry.Name.ToLower(), entry.Open()));
            }

            return result;
        }

        public static Definition.ModelType GetModelType(List<KeyValuePair<string, Stream>> streamList)
        {
            Definition.ModelType modelType;
            List<string> fileNames = streamList.Select(p => p.Key).ToList();

            if (fileNames.Any(p => Path.GetExtension(p).ToLower() == "." + Definition.ModelType.couple))
            {
                modelType = Definition.ModelType.couple;
            }
            else if (fileNames.Any(p => Path.GetExtension(p).ToLower() == "." + Definition.ModelType.m21fm))
            {
                modelType = Definition.ModelType.m21fm;
            }
            else if (fileNames.Any(p => Path.GetExtension(p).ToLower() == "." + Definition.ModelType.m21fst))
            {
                modelType = Definition.ModelType.m21fst;
            }
            else if (fileNames.Any(p => Path.GetExtension(p).ToLower() == "." + Definition.ModelType.m21))
            {
                modelType = Definition.ModelType.m21;
            }
            else if (fileNames.Any(p => Path.GetExtension(p).ToLower() == "." + Definition.ModelType.mhydro))
            {
                modelType = Definition.ModelType.mhydro;
            }
            else if (fileNames.Any(p => Path.GetExtension(p).ToLower() == "." + Definition.ModelType.sim11))
            {
                modelType = Definition.ModelType.sim11;
            }
            else
            {
                throw new Exception("No valid model files in setup");
            }

            if (fileNames.Where(p => Path.GetExtension(p).ToLower() == "." + modelType).Count() > 1)
            {
                throw new Exception("Multiple model setup files of  type: " + modelType);
            }
            return modelType;
        }

        public static JObject DetermineModelType(Dictionary<string, string> queryParameters, Stream stream, string modelFileName = null)
        {
            JObject jObject = new JObject();

            if (queryParameters.ContainsKey("type") && (queryParameters["type"] == Definition.IndexFileType.dfs2.ToString() || queryParameters["type"] == Definition.IndexFileType.dfsu.ToString()))
            {
                if (!(queryParameters["type"] == Definition.IndexFileType.dfs2.ToString() ? GridProcess.CompareSpatialExtent(queryParameters, stream) : MeshProcess.CompareSpatialExtent(queryParameters, stream)))
                {
                    jObject.Add("Message", "Error: Extents do not match");
                }
                else
                {
                    //handle index file
                    List<double> indexZones = ProcessModelSetup.ProcessIndexFile(queryParameters["user"], queryParameters["filename"], queryParameters["type"], stream);
                    jObject.Add("Message", "Index File Posted");
                    jObject.Add("2dzv", string.Join(",", indexZones));
                    jObject.Add("zt", queryParameters["type"]);
                    jObject.Add("modeltype", queryParameters["type"]);
                    jObject.Add("indexfiletype", queryParameters["type"]);
                }
            }
            else
            {
                List<KeyValuePair<string, Stream>> streamList = GetZipEtries(stream);

                string modelFilePath = Path.GetTempFileName();
                Definition.ModelType modelType;
                if (string.IsNullOrEmpty(modelFileName))
                {
                    modelType = GetModelType(streamList);

                    KeyValuePair<string, Stream> keyValuePair = streamList.First(p => Path.GetExtension(p.Key).ToLower() == "." + modelType);
                    GridProcess.Stream2File(keyValuePair.Value, modelFilePath);
                    modelFileName = keyValuePair.Key;
                }
                else
                {
                    modelType = GetModelType(new List<KeyValuePair<string, Stream>> { new KeyValuePair<string, Stream>(modelFileName, new MemoryStream()) });
                    GridProcess.Stream2File(streamList.First(p => Path.GetFileName(p.Key.ToLower()) == Path.GetFileName(modelFileName.ToLower())).Value, modelFilePath);
                }

                string modelCategory = modelType == Definition.ModelType.couple ? string.Empty : ((modelType == Definition.ModelType.sim11 || modelType == Definition.ModelType.mhydro) ? "1d" : "2d");
                                                
                List<string> zList = new List<string>();

                if (modelType == Definition.ModelType.couple)
                {
                    List<JObject> jObjectList = new List<JObject>();
                    string m21FilePath = PFS.GetCoupleFile(modelFilePath, "MIKE21_Path");
                    jObjectList.Add(DetermineModelType(queryParameters, stream, m21FilePath));
                    string m11FilePath = PFS.GetCoupleFile(modelFilePath, "MIKE11_Path");
                    jObjectList.Add(DetermineModelType(queryParameters, stream, m11FilePath));

                    foreach (JObject jObj in jObjectList)
                    {
                        foreach (JProperty jProperty in jObj.Properties())
                        {
                            if (jObject.Property(jProperty.Name) == null)
                            {
                                jObject.Add(jProperty);
                            }
                            else
                            {
                                jObject.Property(jProperty.Name).Value = jObject.Property(jProperty.Name).Value.ToString() + "," + jProperty.Value.ToString();
                            }
                        }
                    }
                }
                else
                {
                    string bathymetryFile;
                    if (modelType == Definition.ModelType.mhydro)
                    {
                        bathymetryFile = modelFileName;
                        zList = PFS.GetBranches(modelFilePath);

                        List<string> jimList = PFS.GetStructures(modelFilePath);

                        SetModelSetupSQL(queryParameters["user"], queryParameters["filename"], "nwk", new MemoryStream(File.ReadAllBytes(modelFilePath)));

                        //write name of xs file to log, not nwk
                        bathymetryFile = PFS.GetBathymetryFile(modelFilePath);
                        string bathymetryFilePath = Path.GetTempFileName();
                        bathymetryFilePath = Path.ChangeExtension(bathymetryFilePath, Path.GetExtension(bathymetryFile));
                        MemoryStream bathymetryStream = new MemoryStream();
                        streamList.First(p => p.Key == Path.GetFileName(bathymetryFile).ToLower()).Value.CopyTo(bathymetryStream);
                        byte[] bathymetryArray = bathymetryStream.ToArray();
                        GridProcess.Stream2File(new MemoryStream(bathymetryArray), bathymetryFilePath);
                        jObject.Add("chainages", CrossSection.GetChainages(zList, bathymetryFilePath));
                    }
                    else if (modelType == Definition.ModelType.sim11)
                    {
                        bathymetryFile = PFS.GetInputFile(modelFilePath, "nwk");
                        string bathymetryFilePath = Path.GetTempFileName();
                        MemoryStream bathymetryStream = new MemoryStream();
                        streamList.First(p => p.Key == Path.GetFileName(bathymetryFile).ToLower()).Value.CopyTo(bathymetryStream);
                        byte[] bathymetryArray = bathymetryStream.ToArray();
                        GridProcess.Stream2File(new MemoryStream(bathymetryArray), bathymetryFilePath);
                        SetModelSetupSQL(queryParameters["user"], queryParameters["filename"], "nwk", new MemoryStream(bathymetryArray));
                        zList = PFS.GetBranches(bathymetryFilePath);

                        File.Delete(bathymetryFilePath);

                        //write name of xs file to log, not nwk
                        bathymetryFile = PFS.GetInputFile(modelFilePath, "xs");
                        bathymetryFilePath = Path.GetTempFileName();
                        bathymetryFilePath = Path.ChangeExtension(bathymetryFilePath, Path.GetExtension(bathymetryFile));
                        bathymetryStream = new MemoryStream();
                        streamList.First(p => p.Key == Path.GetFileName(bathymetryFile).ToLower()).Value.CopyTo(bathymetryStream);
                        bathymetryArray = bathymetryStream.ToArray();
                        GridProcess.Stream2File(new MemoryStream(bathymetryArray), bathymetryFilePath);
                        jObject.Add("chainages", CrossSection.GetChainages(zList, bathymetryFilePath));
                    }
                    else if (modelType == Definition.ModelType.m21fm || modelType == Definition.ModelType.m21 || modelType == Definition.ModelType.m21fst)
                    {
                        bathymetryFile = PFS.GetBathymetryFile(modelFilePath);
                        Stream indexStream = CreateIndexFile(bathymetryFile, streamList.First(p => p.Key == Path.GetFileName(bathymetryFile).ToLower()).Value);
                        List<double> indexZones = ProcessIndexFile(queryParameters["user"], queryParameters["filename"], Path.GetExtension(bathymetryFile.ToLower()) == "." + Definition.IndexFileType.mesh ? Definition.IndexFileType.dfsu.ToString() : Definition.IndexFileType.dfs2.ToString(), indexStream, true);
                        jObject.Add("indexfiletype", Path.GetExtension(bathymetryFile.ToLower()) == "." + Definition.IndexFileType.mesh ? Definition.IndexFileType.dfsu.ToString() : Definition.IndexFileType.dfs2.ToString());
                        zList = indexZones.Select(p => p.ToString()).ToList();
                    }
                    else
                    {
                        throw new Exception("Model Type Not Implemented: " + modelType);
                    }

                    jObject.Add(modelCategory + "zv", string.Join(",", zList));
                    jObject.Add("zt", modelCategory + " Randomized Bathymetry File: " + bathymetryFile);
                    
                    string roughMessage;
                    List<string> roughnessZones;
                    if (modelType == Definition.ModelType.mhydro)
                    {
                        roughnessZones = PFS.GetRoughness(modelFilePath);
                        roughMessage = modelCategory + " Randomized Roughness File: " + modelFileName;
                    }
                    else if (modelType == Definition.ModelType.sim11)
                    {
                        string roughnessFile = PFS.GetInputFile(modelFilePath, "hd");
                        string hdFilePath = Path.GetTempFileName();
                        GridProcess.Stream2File(streamList.First(p => p.Key.ToLower() == Path.GetFileName(roughnessFile).ToLower()).Value, hdFilePath);
                        roughnessZones = PFS.GetRoughness(hdFilePath);
                        File.Delete(hdFilePath);
                        roughMessage = modelCategory + " Randomized Roughness File: " + roughnessFile;
                    }
                    else if (modelType == Definition.ModelType.m21fm || modelType == Definition.ModelType.m21 || modelType == Definition.ModelType.m21fst)
                    {
                        if (PFS.IsGlobalRoughness(modelFilePath))
                        {
                            roughnessZones = new List<string>() { PFS.GetRoughness(modelFilePath)[0] };
                            roughMessage = modelCategory + " Global Roughness";
                        }
                        else
                        {
                            string roughnessFile = PFS.GetRoughnessFile(modelFilePath);

                            Stream roughnessStream = streamList.First(p => p.Key == Path.GetFileName(roughnessFile).ToLower()).Value;
                            roughnessZones = (modelType == Definition.ModelType.m21fm ? MeshProcess.GetDistinctZones(roughnessStream) : GridProcess.GetDistinctZones(roughnessStream)).Select(p => p.ToString()).ToList(); ;
                            //insert Zone 0 at front for land and boundaries
                            roughnessZones.Insert(0, "0");
                            roughMessage = modelCategory + " Randomized Roughness File: " + roughnessFile;
                        }
                    }
                    else
                    {
                        throw new Exception("Model Type Not Implemented: " + modelType);
                    }
                    jObject.Add(modelCategory + "rv", string.Join(",", roughnessZones));
                    jObject.Add("rt", roughMessage);
                }
                File.Delete(modelFilePath);

                List<KeyValuePair<string, string>> logList = new List<KeyValuePair<string,string>>();
                logList.Add(new KeyValuePair<string, string>("modelFileName", modelFileName));
                logList.Add(new KeyValuePair<string, string>("modelType", modelType.ToString()));

                foreach (var vp in logList)
                {
                    if (jObject.Property(vp.Key) == null)
                    {
                        jObject.Add(vp.Key, vp.Value);
                    }
                    else
                    {
                        jObject.Property(vp.Key).Value = vp.Value + "," + jObject.Property(vp.Key).Value.ToString();
                    }
                }
            }
            return jObject;
        }

        public static string DetermineModelType(Dictionary<string, string> queryParameters, Stream stream)
        {
            JObject jObject = DetermineModelType(queryParameters, stream, null);
            SetModelSetupSQL(queryParameters["user"], queryParameters["filename"], queryParameters.ContainsKey("type") && (queryParameters["type"] == Definition.IndexFileType.dfs2.ToString() || queryParameters["type"] == Definition.IndexFileType.dfsu.ToString()) ? queryParameters["type"] : (jObject["modelType"].ToString().Split(new char[] { ',' })[0]), stream);
            return jObject.ToString();
        }

        public static List<double> ProcessIndexFile(string userId, string fileName, string type, Stream stream, bool persist = false)
        {
            if (persist)
            {
                SetModelSetupSQL(userId, fileName, type, stream);
            }
            stream.Position = 0;

            if (type == Definition.IndexFileType.dfs2.ToString())
            {
                return GridProcess.GetDistinctZones(stream);
            }
            else if (type == Definition.IndexFileType.dfsu.ToString())
            {
                return MeshProcess.GetDistinctZones(stream);
            }
            else 
            {
                throw new Exception("not yet implemented");
            }
        }

        public static void RandomizeModelSetup(Dictionary<string, string> queryParameters, Stream stream, string modelFileName)
        {
            List<KeyValuePair<string, Stream>> streamList = ProcessModelSetup.GetZipEtries(stream);

            List<string> modelNameList = new List<string>();
            List<string> modelResultNameList = new List<string>();
            
            Definition.ModelType modelType;
            if (string.IsNullOrEmpty(modelFileName))
            {
                modelType = GetModelType(streamList);

                if (Directory.Exists(Definition.Folder))
                {
                    Directory.Delete(Definition.Folder, true);
                }
                if (!Directory.Exists(Definition.Folder))
                {
                    Directory.CreateDirectory(Definition.Folder);
                }

                UnZipStreamToFile(stream, Definition.Folder);
            }
            else
            {
                modelType = GetModelType(new List<KeyValuePair<string, Stream>> { new KeyValuePair<string, Stream>(modelFileName, new MemoryStream()) });
            }

            string modelFilePath = Directory.GetFiles(Definition.Folder, "*." + modelType, SearchOption.AllDirectories)[0];

            int ensembleNo = Convert.ToInt16(queryParameters["ensembles"]);
            
            string subFolder = Path.GetDirectoryName(modelFilePath); // Path.Combine(Path.GetDirectoryName(m21FilePath), "Uncertainty_Sims");
            if (!Directory.Exists(subFolder))
            {
                Directory.CreateDirectory(subFolder);
            }

            if (modelType == Definition.ModelType.couple)
            {
                string m21FilePath = PFS.GetCoupleFile(modelFilePath, "MIKE21_Path");
                string m11FilePath = PFS.GetCoupleFile(modelFilePath, "MIKE11_Path");
                
                for (int fileNumber = 0; fileNumber <= ensembleNo; fileNumber++)
                {
                    string newModelFilePath = Path.Combine(Path.GetDirectoryName(modelFilePath), Path.GetFileNameWithoutExtension(modelFilePath) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(modelFilePath));
                    string newm21FilePath = Path.Combine(Path.GetDirectoryName(m21FilePath), Path.GetFileNameWithoutExtension(m21FilePath) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(m21FilePath));
                    string newm11FilePath = Path.Combine(Path.GetDirectoryName(m11FilePath), Path.GetFileNameWithoutExtension(m11FilePath) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(m11FilePath));
                    PFS.ModifyCoupleFile(modelFilePath, newModelFilePath, "MIKE21_Path", newm21FilePath);
                    PFS.ModifyCoupleFile(modelFilePath, newModelFilePath, "MIKE11_Path", newm11FilePath);
                }
                RandomizeModelSetup(queryParameters, stream, m21FilePath);
                RandomizeModelSetup(queryParameters, stream, m11FilePath);
                return;
            }

            string modelCategory = modelType == Definition.ModelType.couple ? string.Empty : ((modelType == Definition.ModelType.sim11 || modelType == Definition.ModelType.mhydro) ? "1d" : "2d");

            JArray roughnessParrayArray = queryParameters.ContainsKey("r" + modelCategory) ? JArray.Parse(queryParameters["r" + modelCategory]) : new JArray();
            JArray bathymetryParrayArray = queryParameters.ContainsKey("z" + modelCategory) ? JArray.Parse(queryParameters["z" + modelCategory]) : new JArray();

            bool global = roughnessParrayArray.Any(p => p["desc"].ToObject<string>() == "Global");

            //default to true
            bool batch = true;
            if (queryParameters.ContainsKey("batch") && bool.TryParse(queryParameters["batch"], out batch))
            {

            }

            KeyValuePair<string, Stream> streamVp = streamList.First(p => p.Key == Path.GetFileName(modelFilePath).ToLower());

            List<string> resultFileNameList = PFS.GetResultFileList(modelFilePath);
            List<string> resultFilePathList = resultFileNameList.Select(p => Path.Combine(subFolder, p)).ToList();  
                
            string resultSubFolder = Path.Combine(Path.GetDirectoryName(resultFilePathList[0]), "Uncertainty_Result");
            if (!Directory.Exists(resultSubFolder))
            {
                Directory.CreateDirectory(resultSubFolder);
            }

            string statisticsFileName = PFS.GetStatisticsFile(modelFilePath);
            string statisticsFilePath = string.Empty;
            string statisticsSubFolder = string.Empty;
            if (!string.IsNullOrEmpty(statisticsFileName))
            {
                statisticsFilePath = Path.Combine(subFolder, statisticsFileName);

                statisticsSubFolder = Path.Combine(Path.GetDirectoryName(statisticsFilePath), "Uncertainty_Statistics");
                if (!Directory.Exists(statisticsSubFolder))
                {
                    Directory.CreateDirectory(statisticsSubFolder);
                }
            }

            string bathymetryFileName = modelType == Definition.ModelType.sim11 ? PFS.GetInputFile(modelFilePath, "xs") : PFS.GetBathymetryFile(modelFilePath);
            string bathymetryFilePath = Directory.GetFiles(Definition.Folder, Path.GetFileName(bathymetryFileName), SearchOption.AllDirectories)[0];

            MemoryStream bathymetryMemoryStream = new MemoryStream();
            streamList.First(p => p.Key == Path.GetFileName(bathymetryFileName).ToLower()).Value.CopyTo(bathymetryMemoryStream);
            byte[] bathymetryByteArray = bathymetryMemoryStream.ToArray();

            List<double> zoneList = new List<double>();
            if (modelType == Definition.ModelType.m21 || modelType == Definition.ModelType.m21fm || modelType == Definition.ModelType.m21fst)
            {
                string dataBase64String = Dfs0SqlCache.GetDfs0(queryParameters["user"], queryParameters["filename"], Path.GetExtension(bathymetryFileName.ToLower()).Remove(0, 1).Replace(Definition.IndexFileType.mesh.ToString(), Definition.IndexFileType.dfsu.ToString()));

                //create the index file if not already in database
                byte[] zoneByteArray = string.IsNullOrEmpty(dataBase64String) ? CreateIndexFile(bathymetryFileName, new MemoryStream(bathymetryByteArray)).ToArray(): Convert.FromBase64String(dataBase64String);

                File.WriteAllBytes(Path.Combine(Definition.Folder,"IndexFile" + Path.GetExtension(bathymetryFileName.ToLower())), zoneByteArray);

                zoneList = Path.GetExtension(bathymetryFileName.ToLower()) == "." + Definition.IndexFileType.dfs2 ? GridProcess.GetZones(new MemoryStream(zoneByteArray)) : MeshProcess.GetZones(new MemoryStream(zoneByteArray));
            }

            if (roughnessParrayArray.Count > 0)
            {
                string roughnessFilePath = string.Empty;
                string roughnessSubFolder = string.Empty;
                byte[] roughnessByteArray = new byte[0];

                string roughnessFileName = modelType == Definition.ModelType.sim11 ? PFS.GetInputFile(modelFilePath, "hd") : PFS.GetRoughnessFile(modelFilePath);

                if (!global || modelType == Definition.ModelType.sim11)
                {
                    roughnessFilePath = Directory.GetFiles(Definition.Folder, Path.GetFileName(roughnessFileName), SearchOption.AllDirectories)[0];

                    if (modelType == Definition.ModelType.m21 || modelType == Definition.ModelType.m21fm || modelType == Definition.ModelType.m21fst)
                    {
                        MemoryStream roughnessMemoryStream = new MemoryStream();
                        streamList.First(p => p.Key == Path.GetFileName(roughnessFileName).ToLower()).Value.CopyTo(roughnessMemoryStream);
                        roughnessByteArray = roughnessMemoryStream.ToArray();
                    }

                    roughnessSubFolder = Path.Combine(Path.GetDirectoryName(roughnessFilePath), "Uncertainty_Roughness");
                    if (!Directory.Exists(roughnessSubFolder))
                    {
                        Directory.CreateDirectory(roughnessSubFolder);
                    }
                }

                foreach (JObject jObject in roughnessParrayArray)
                {                    
                    if (jObject["ss"].ToObject<string>() != "2")
                    {
                        List<double> randomManningValues = _GetRandomValues(jObject, ensembleNo);

                        double mannings = jObject["M"].ToObject<double>();

                        for (int fileNumber = 0; fileNumber <= ensembleNo; fileNumber++)
                        {
                            double newRandomValue = randomManningValues[fileNumber];

                            string newModelName = Path.GetFileNameWithoutExtension(streamVp.Key) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(streamVp.Key);
                            string newModelFilePath = Path.Combine(subFolder, newModelName);
                            modelNameList.Add(newModelName);

                            foreach (string resultFilePath in resultFilePathList)
                            {
                                string newResultName = Path.GetFileNameWithoutExtension(resultFilePath) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(resultFilePath);
                                string newResultFilePath = Path.Combine(resultSubFolder, newResultName);
                                modelResultNameList.Add(newResultName);
                                if (!File.Exists(newResultFilePath))
                                {
                                    PFS.ModifyResultFile(modelFilePath, newModelFilePath, newResultFilePath);
                                }
                            }
                            
                            if (!string.IsNullOrEmpty(statisticsFileName))
                            {
                                string newStatisticsFilePath = Path.Combine(statisticsSubFolder, Path.GetFileNameWithoutExtension(statisticsFilePath) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(statisticsFilePath));
                                if (!File.Exists(newStatisticsFilePath))
                                {
                                    PFS.ModifyStatisticsFile(modelFilePath, newModelFilePath, newStatisticsFilePath);
                                }
                            }

                            if (modelType == Definition.ModelType.sim11)
                            {
                                string hdFile = Path.Combine(subFolder, PFS.GetInputFile(modelFilePath, "hd"));
                                string newhDFile = Path.Combine(roughnessSubFolder, Path.GetFileNameWithoutExtension(hdFile) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(hdFile));
                                PFS.ModifyInputFile(modelFilePath, newModelFilePath, "hd", newhDFile);
                                if (!File.Exists(newhDFile))
                                {
                                    File.Copy(hdFile, newhDFile, false);
                                }
                                if (fileNumber > 0)
                                {
                                    PFS.ModifyRoughness(hdFile, newhDFile, jObject["desc"].ToObject<string>(), newRandomValue);
                                }
                            }
                            else if (modelType == Definition.ModelType.mhydro)
                            {
                                PFS.ModifyRoughness(modelFilePath, newModelFilePath, jObject["desc"].ToObject<string>(), newRandomValue);
                            }
                            else
                            {
                                if (global)
                                {
                                    if (fileNumber > 0)
                                    {
                                        PFS.ModifyRoughness(modelFilePath, newModelFilePath, newRandomValue);
                                    }
                                }
                                else
                                {
                                    string newRoughnessFilePath = Path.Combine(roughnessSubFolder, Path.GetFileNameWithoutExtension(roughnessFileName) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(roughnessFilePath));

                                    if (!File.Exists(newRoughnessFilePath))
                                    {
                                        GridProcess.Stream2File(new MemoryStream(roughnessByteArray), newRoughnessFilePath);
                                        PFS.ModifyRoughnessFile(modelFilePath, newModelFilePath, newRoughnessFilePath);
                                    }

                                    //if randomise by zone
                                    if (fileNumber > 0 && jObject["sm"].ToObject<string>() == "1")
                                    {
                                        if (Path.GetExtension(newRoughnessFilePath) == "." + Definition.IndexFileType.dfs2)
                                        {
                                            GridProcess.UpdateGrid(newRoughnessFilePath, mannings, newRandomValue, zoneList);
                                        }
                                        else
                                        {
                                            MeshProcess.UpdateMesh(newRoughnessFilePath, mannings, newRandomValue, zoneList, null);
                                        }
                                    }
                                    //else if randomize by cell
                                    else if (fileNumber > 0 && jObject["sm"].ToObject<string>() == "0")
                                    {
                                        if (Path.GetExtension(newRoughnessFilePath) == "." + Definition.IndexFileType.dfs2)
                                        {
                                            List<string> spatialExtent = GridProcess.GetSpatialExtent(new MemoryStream(roughnessByteArray));
                                            int xDim = Convert.ToInt32(spatialExtent[0]);
                                            int yDim = Convert.ToInt32(spatialExtent[1]);
                                            GridProcess.UpdateGrid(newRoughnessFilePath, mannings, _GetRandomValues(jObject, xDim * yDim), zoneList);
                                        }
                                        else
                                        {
                                            int NumberOfElements = MeshProcess.GetNumberOfElements(new MemoryStream(roughnessByteArray));
                                            MeshProcess.UpdateMesh(newRoughnessFilePath, mannings, _GetRandomValues(jObject, NumberOfElements), zoneList, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (bathymetryParrayArray.Count > 0)
            {
                JArray jArray = new JArray();
                if (queryParameters.ContainsKey("Chainages"))
                {

                    jArray = JArray.Parse(queryParameters["Chainages"]);
                }

                string bathymetrySubFolder = string.Empty;
                
                bathymetrySubFolder = Path.Combine(Path.GetDirectoryName(bathymetryFilePath), "Uncertainty_Topo");
                if (!Directory.Exists(bathymetrySubFolder))
                {
                    Directory.CreateDirectory(bathymetrySubFolder);
                }

                if (Path.GetExtension(bathymetryFileName) == ".mesh")
                {
                    zoneList = MeshProcess.GetNodeZones(bathymetryFilePath, zoneList);
                }

                foreach (JObject jObject in bathymetryParrayArray)
                {
                    if (jObject["ss"].ToObject<string>() != "2")
                    { 
                        List<double> randomZStandardDeviations = _GetRandomBathymetryValues(jObject, ensembleNo);

                        for (int fileNumber = 0; fileNumber <= ensembleNo; fileNumber++)
                        {
                            double newRandomStandardDeviation = randomZStandardDeviations[fileNumber];

                            string newModelName = Path.GetFileNameWithoutExtension(streamVp.Key) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(streamVp.Key);
                            string newModelFilePath = Path.Combine(subFolder, newModelName);
                            modelNameList.Add(newModelName);

                            foreach (string resultFilePath in resultFilePathList)
                            {
                                string newResultName = Path.GetFileNameWithoutExtension(resultFilePath) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(resultFilePath);
                                string newResultFilePath = Path.Combine(resultSubFolder, newResultName);
                                modelResultNameList.Add(newResultName);
                                if (!File.Exists(newResultFilePath))
                                {
                                    PFS.ModifyResultFile(modelFilePath, newModelFilePath, newResultFilePath);
                                }
                            }

                            if (!string.IsNullOrEmpty(statisticsFileName))
                            {
                                string newStatisticsFilePath = Path.Combine(statisticsSubFolder, Path.GetFileNameWithoutExtension(statisticsFilePath) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(statisticsFilePath));
                                if (!File.Exists(newStatisticsFilePath))
                                {
                                    PFS.ModifyStatisticsFile(modelFilePath, newModelFilePath, newStatisticsFilePath);
                                }
                            }

                            string newBathymetryFilePath = Path.Combine(bathymetrySubFolder, Path.GetFileNameWithoutExtension(bathymetryFileName) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(bathymetryFilePath));

                            if (!File.Exists(newBathymetryFilePath))
                            {
                                GridProcess.Stream2File(new MemoryStream(bathymetryByteArray), newBathymetryFilePath);
                                if (modelType == Definition.ModelType.sim11)
                                {
                                    PFS.ModifyInputFile(modelFilePath, newModelFilePath, "xs", newBathymetryFilePath);
                                }
                                else
                                {
                                    PFS.ModifyBathymetryFile(modelFilePath, newModelFilePath, newBathymetryFilePath);
                                }

                            }

                            if (modelType == Definition.ModelType.sim11 || modelType == Definition.ModelType.mhydro)
                            {
                                if (fileNumber > 0)
                                {
                                    CrossSection.ChangeDatum(newBathymetryFilePath, jObject["desc"].ToObject<string>().Split(new char[] { ':' })[0], jObject["desc"].ToObject<string>().Split(new char[] { ':' })[1], newRandomStandardDeviation, jArray);
                                }
                            }
                            else
                            {
                                //if randomise by zone
                                if (fileNumber > 0 && jObject["sm"].ToObject<string>() == "1")
                                {
                                    if (Path.GetExtension(bathymetryFileName) == ".dfs2")
                                    {
                                        GridProcess.UpdateGrid(newBathymetryFilePath, -1, newRandomStandardDeviation, zoneList, Convert.ToInt32(jObject["desc"].ToObject<string>().Replace("Zone ", string.Empty)));
                                    }
                                    else if (Path.GetExtension(bathymetryFileName) == ".mesh")
                                    {
                                        MeshProcess.UpdateMesh(newBathymetryFilePath, newRandomStandardDeviation, Convert.ToInt32(jObject["desc"].ToObject<string>().Replace("Zone ", string.Empty)), zoneList);
                                    }
                                    else
                                    {
                                        MeshProcess.UpdateMesh(newBathymetryFilePath, -1, newRandomStandardDeviation, zoneList, Convert.ToInt32(jObject["desc"].ToObject<string>().Replace("Zone ", string.Empty)));
                                    }
                                }
                                //else if randomize by cell
                                else if (fileNumber > 0 && jObject["sm"].ToObject<string>() == "0")
                                {
                                    if (Path.GetExtension(bathymetryFileName) == ".dfs2")
                                    {
                                        List<string> spatialExtent = GridProcess.GetSpatialExtent(new MemoryStream(bathymetryByteArray));
                                        int xDim = Convert.ToInt32(spatialExtent[0]);
                                        int yDim = Convert.ToInt32(spatialExtent[1]);
                                        GridProcess.UpdateGrid(newBathymetryFilePath, -1, _GetRandomBathymetryValues(jObject, xDim * yDim), zoneList, Convert.ToInt32(jObject["desc"].ToObject<string>().Replace("Zone ", string.Empty)));
                                    }
                                    else if (Path.GetExtension(bathymetryFileName) == ".mesh")
                                    {
                                        int numberOfElements = MeshProcess.GetNumberOfElements(new MemoryStream(bathymetryByteArray));
                                        MeshProcess.UpdateMesh(newBathymetryFilePath, _GetRandomBathymetryValues(jObject, numberOfElements), Convert.ToInt32(jObject["desc"].ToObject<string>().Replace("Zone ", string.Empty)), zoneList);
                                    }
                                    else
                                    {
                                        int numberOfElements = MeshProcess.GetNumberOfElements(new MemoryStream(bathymetryByteArray));
                                        MeshProcess.UpdateMesh(newBathymetryFilePath, -1, _GetRandomBathymetryValues(jObject, numberOfElements), zoneList, Convert.ToInt32(jObject["desc"].ToObject<string>().Replace("Zone ", string.Empty)));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            using (StreamWriter file = File.CreateText(Path.Combine(Definition.Folder, "ensembleinfo.json")))
            {
                using (JsonTextWriter writer = new JsonTextWriter(file))
                {
                    JObject jObj = JObject.Parse(JsonConvert.SerializeObject(queryParameters));
                    jObj.Add("model", string.Join(",", modelNameList.Distinct()));
                    jObj.Add("result", string.Join(",", modelResultNameList.Distinct()));
                    jObj.WriteTo(writer);
                }
            }
            
            if (batch)
            {
                string batchFileName = Path.Combine(Path.GetDirectoryName(modelFilePath), "Run Ensemble.bat");

                StreamWriter streamWriter = new StreamWriter(batchFileName);

                for (int fileNumber = 0; fileNumber <= ensembleNo; fileNumber++)
                {
                    string newM21FilePath = Path.Combine(subFolder, Path.GetFileNameWithoutExtension(streamVp.Key) + "_" + fileNumber.ToString().PadLeft(3, '0') + Path.GetExtension(streamVp.Key));
                    streamWriter.WriteLine("\"" + @"C:\Program Files (x86)\DHI\2017\bin\x64\" + Definition.ModelEngines.First(p => p.Key == modelType).Value + ".exe" + "\"" + " " + "\"" + newM21FilePath.Replace(Path.GetDirectoryName(modelFilePath), string.Empty).TrimStart(new char[] { '\\' }) + "\"");
                }
                streamWriter.WriteLine("pause");
                streamWriter.Close();
            }
        }

        public static Stream RandomizeModelSetup(Dictionary<string, string> queryParameters, Stream stream)
        {
            RandomizeModelSetup(queryParameters, stream, null);
            
            string zipPath = Path.GetTempFileName();
            File.Delete(zipPath);

            ZipFile.CreateFromDirectory(Definition.Folder, zipPath, CompressionLevel.Fastest, false);

            byte[] byteArray = File.ReadAllBytes(zipPath);

            File.Delete(zipPath);
            Directory.Delete(Definition.Folder, true);

            return new MemoryStream(byteArray);
        }

        private static List<double> _GetRandomValues(JObject jObject, int ensembleNo)
        {
            double mannings = jObject["M"].ToObject<double>();
            double standardDeviation = jObject["sd"].ToObject<double>();
            double n = 1 / mannings;

            //as standard deviation is now calculated in web page
            ////if use n/3 overwrite std dev
            //if (parameterList.First(p => p.Contains("SelectedSD:")).Replace("SelectedSD:", string.Empty) == "0")
            //{
            //    double n = 1 / mannings;
            //    standardDeviation = n / 3;
            //}

            double minn = 0.01;
            double maxM = 1 / minn;

            List<double> randomManningValues = Randomize.GetRandomValues(n, standardDeviation, ensembleNo);

            //min n max of
            randomManningValues = randomManningValues.Select(p => System.Math.Max(minn, p)).ToList();

            //invert
            randomManningValues = randomManningValues.Select(p => 1 / p).ToList();

            return randomManningValues;

            //return randomManningValues.Select(p => Math.Min(maxM, p)).ToList();
        }

        private static List<double> _GetRandomBathymetryValues(JObject jObject, int ensembleNo)
        {
            double standardDeviation = jObject["sd"].ToObject<double>();

            List<double> randomManningValues = Randomize.GetRandomValues(standardDeviation, ensembleNo);

            return randomManningValues;
        }

        public static void UnZipStreamToFile(Stream stream, string folder)
        {
            stream.Position = 0;
            
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            ZipFile.ExtractToDirectory(filePath, folder);

            File.Delete(filePath);
        }

        public static Stream ZipStreamList(List<Stream> streamList, List<string> fileNameList)
        {
            string folder = Path.Combine(Path.GetTempPath(), DateTime.Now.ToOADate().ToString());
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            int index = 0;
            foreach(Stream stream in streamList)
            {
                stream.Position = 0;
                GridProcess.Stream2File(stream, Path.Combine(folder, fileNameList[index]));
                index++;
            }

            string zipPath = Path.GetTempFileName();
            File.Delete(zipPath);

            ZipFile.CreateFromDirectory(folder, zipPath, CompressionLevel.Fastest, false);

            byte[] byteArray = File.ReadAllBytes(zipPath);

            File.Delete(zipPath);
            Directory.Delete(folder, true);

            return new MemoryStream(byteArray);
        }

        public static MemoryStream CreateIndexFile(string bathymetryFileName, Stream stream)
        {
            MemoryStream result = new MemoryStream();
            Stream tempStream = Path.GetExtension(bathymetryFileName.ToLower()) == "." + Definition.IndexFileType.mesh ? MeshProcess.CreateIndexFile(stream) : GridProcess.CreateIndexFile(stream);
            tempStream.CopyTo(result);
            return result;
        }

        public static void SetModelSetupSQL(string userId, string name, string type, Stream stream)
        {
            stream.Position = 0;

            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            string modelSetupString = Convert.ToBase64String(memoryStream.ToArray());

            Dfs0SqlCache.SetDfs0(userId, name, type, modelSetupString);
        }
    }
}
