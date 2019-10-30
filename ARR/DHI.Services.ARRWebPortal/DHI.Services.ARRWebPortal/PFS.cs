using DHI.PFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public static class PFS
    {
        public static string GetCoupleFile(string filePath, string ext)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                string fileName;

                PFSSection target = pfsFile.GetTarget(1);

                fileName = target.GetKeyword(ext, 1).GetParameter(1).ToFileName();
                return fileName;

                throw new Exception("Not a valid file");
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyCoupleFile(string filePath, string newFilePath, string ext, string value)
        {
            PFSFile pfsFile = File.Exists(newFilePath) ? pfsFile = new PFSFile(newFilePath, true) : pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target = pfsFile.GetTarget(1);

                target.GetKeyword(ext, 1).GetParameter(1).ModifyFileNameParameter(value);

                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static string GetInputFile(string filePath, string ext)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                string fileName;

                try
                {
                    PFSSection target = pfsFile.GetTarget("Run11", 1);

                    PFSSection section1 = target.GetSection("Input", 1);

                    fileName = section1.GetKeyword(ext, 1).GetParameter(1).ToFileName();
                    return fileName;
                }
                catch (Exception) { }

                throw new Exception("Not a valid file");
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyInputFile(string filePath, string newFilePath, string ext, string value)
        {
            PFSFile pfsFile = File.Exists(newFilePath) ? pfsFile = new PFSFile(newFilePath, true) : pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target = pfsFile.GetTarget("Run11", 1);

                PFSSection section1 = target.GetSection("Input", 1);

                section1.GetKeyword(ext, 1).GetParameter(1).ModifyFileNameParameter(value);

                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static List<string> GetBranches(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                List<string> branchList = new List<string>();

                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE_11_Network_editor"
                // "MIKEHYDRO"


                PFSSection section1 = target.GetSectionsCount("BRANCHES") > 0 ? target.GetSection("BRANCHES", 1) : target.GetSection("MH_River_Network", 1);

                for (int i = 1; i <= section1.GetSectionsCount("branch"); i++)
                {
                    PFSSection section2 = section1.GetSection("branch", i);
                    branchList.Add(section2.GetKeyword("definitions", 1).GetParameter(1).ToString() + ":" + section2.GetKeyword("definitions", 1).GetParameter(2).ToString());
                }

                if (section1.GetSectionsCount("Branches") > 0)
                {
                    PFSSection section2 = section1.GetSection("Branches", 1);

                    for (int i = 1; i <= section2.GetSectionsCount(); i++)
                    {
                        PFSSection section3 = section2.GetSection(i);
                        if (section3.Name.StartsWith("Branch"))
                        {
                            branchList.Add(section3.GetKeyword("Name", 1).GetParameter(1).ToString() + ":" + section3.GetKeyword("TopoID", 1).GetParameter(1).ToString());
                        }
                    }
                }

                return branchList;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static List<string> GetBranchPoints(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                List<string> branchList = new List<string>();

                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE_11_Network_editor"
                // "MIKEHYDRO"


                PFSSection section1 = target.GetSectionsCount("BRANCHES") > 0 ? target.GetSection("BRANCHES", 1) : target.GetSection("MH_River_Network", 1);

                for (int i = 1; i <= section1.GetSectionsCount("branch"); i++)
                {
                    PFSSection section2 = section1.GetSection("branch", i);
                    string branch = section2.GetKeyword("definitions", 1).GetParameter(1).ToString();
                    for (int j = 1; j <= section2.GetKeyword("points", 1).GetParametersCount(); j++ )
                    {
                        if (section2.GetKeyword("points", 1).GetParameter(j).IsString())
                        {
                            branch = branch + "," + section2.GetKeyword("points", 1).GetParameter(j).ToString();
                        }
                        else
                        {
                            branch = branch + "," + section2.GetKeyword("points", 1).GetParameter(j).ToDouble().ToString();
                        }
                    }
                    branchList.Add(branch);
                }

                if (section1.GetSectionsCount("Branches") > 0)
                {
                    PFSSection section2 = section1.GetSection("Branches", 1);

                    for (int i = 1; i <= section2.GetSectionsCount(); i++)
                    {
                        PFSSection section3 = section2.GetSection(i);
                        if (section3.Name.StartsWith("Branch"))
                        {
                            if (section3.GetKeyword("Shape", 1).GetParameter(1).IsString())
                            {
                                branchList.Add(section3.GetKeyword("Name", 1).GetParameter(1).ToString() + "," + section3.GetKeyword("Shape", 1).GetParameter(1).ToString());
                            }
                            else
                            {
                                branchList.Add(section3.GetKeyword("Name", 1).GetParameter(1).ToString() + "," + section3.GetKeyword("Shape", 1).GetParameter(1).ToDouble().ToString());
                            }
                        }
                    }
                }

                return branchList;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static List<string> GetStructures(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                List<string> structureList = new List<string>();

                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE_11_Network_editor"
                // "MIKEHYDRO"


                PFSSection section1 = target.GetSectionsCount("MH_River_Network") > 0 && target.GetSection("MH_River_Network", 1).GetSectionsCount("Structure") > 0 ? target.GetSection("MH_River_Network", 1).GetSection("Structure", 1) : target.GetSection("STRUCTURE_MODULE", 1);
                
                List<KeyValuePair<string, string>> structureVpList = new List<KeyValuePair<string, string>>();
                structureVpList.Add(new KeyValuePair<string, string>("Weirs", "Weir_"));
                structureVpList.Add(new KeyValuePair<string, string>("Culverts", "Culvert_"));
                structureVpList.Add(new KeyValuePair<string, string>("WEIR", "weir_data"));

                foreach (KeyValuePair<string, string> structureVp in structureVpList)
                {
                    if (section1.GetSectionsCount(structureVp.Key) > 0)
                    {
                        PFSSection section2 = section1.GetSection(structureVp.Key, 1);

                        for (int i = 1; i <= section2.GetSectionsCount(); i++)
                        {
                            PFSSection section3 = section2.GetSection(i);
                            if (section3.Name.StartsWith(structureVp.Value))
                            {
                                if (section3.GetKeywordsCount("BranchID") > 0)
                                {
                                    structureList.Add(section3.GetKeyword("BranchID", 1).GetParameter(1).ToString() + "," + section3.GetKeyword("Chainage", 1).GetParameter(1).ToString());
                                }

                                if (section3.GetKeywordsCount("Location") > 0)
                                {
                                    structureList.Add(section3.GetKeyword("Location", 1).GetParameter(1).ToString() + "," + section3.GetKeyword("Location", 1).GetParameter(2).ToString());
                                }
                            }
                        }
                    }
                }

                return structureList;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static List<string> GetPoints(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                List<string> pointList = new List<string>();

                PFSSection target = pfsFile.GetTarget(1);
                    
                PFSSection section1 = target.GetSection("POINTS", 1);

                for (int i = 1; i <= section1.GetKeywordsCount("point"); i++)
                {
                    pointList.Add((section1.GetKeyword("point", i).GetParameter(1).IsString() ? section1.GetKeyword("point", i).GetParameter(1).ToString() : section1.GetKeyword("point", i).GetParameter(1).ToInt().ToString()) + "," + (section1.GetKeyword("point", i).GetParameter(2).IsString() ? section1.GetKeyword("point", i).GetParameter(2).ToString() : section1.GetKeyword("point", i).GetParameter(2).ToDouble().ToString()) + "," + (section1.GetKeyword("point", i).GetParameter(3).IsString() ? section1.GetKeyword("point", i).GetParameter(3).ToString() : section1.GetKeyword("point", i).GetParameter(3).ToDouble().ToString()));
                }
                return pointList;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static bool IsGlobalRoughness(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target;
                try
                {
                    target = pfsFile.GetTarget("MIKE21_NESTED_MODEL", 1);
                }
                catch (Exception)
                {
                    target = pfsFile.GetTarget("FEMEngineHD", 1);
                }

                PFSSection section1 = target.GetSection("HYDRODYNAMIC_MODULE", 1);

                PFSSection section2;
                try
                {
                    section2 = section1.GetSection("RESISTANCE", 1);
                }
                catch (Exception)
                {
                    section2 = section1.GetSection("BED_RESISTANCE", 1);
                }

                bool returnValue;
                try
                {
                    PFSSection section3 = section2.GetSection("AREA_1", 1);
                    PFSSection section4 = section3.GetSection("DATA_FILE", 1);

                    returnValue = !section4.GetKeyword("FILE_NAME", 1).GetParameter(1).IsFilename();

                }
                catch (Exception)
                {

                    PFSSection section3 = section2.GetSection("MANNING_NUMBER", 1);
                    returnValue = !(section3.GetKeyword("file_name", 1).GetParameter(1).IsFilename() && !string.IsNullOrEmpty(section3.GetKeyword("file_name", 1).GetParameter(1).ToFileName()));
                }

                return returnValue;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static List<string> GetRoughness(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            List<string> roughnessList = new List<string>();
            try{
                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE0_HD"
                //"MIKE21_NESTED_MODEL", 1);
                //"FEMEngineHD"

                PFSSection section1 = target.GetSectionsCount("HYDRODYNAMIC_MODULE") > 0 ? target.GetSection("HYDRODYNAMIC_MODULE", 1) : (target.GetSectionsCount("Global_Variables") > 0 ? target.GetSection("Global_Variables", 1) : target.GetSection("MH_HDParameters", 1));

                PFSSection section2 = section1.GetSectionsCount("Global_Values") > 0 ? section1.GetSection("Global_Values", 1) : (section1.GetSectionsCount("RESISTANCE") > 0 ? section1.GetSection("RESISTANCE", 1) : (section1.GetSectionsCount("BED_RESISTANCE") > 0 ? section1.GetSection("BED_RESISTANCE", 1) : section1.GetSection("MH_BED_RESISTANCE", 1)));

                if (section2.GetKeywordsCount("GlobalResistanceNumber") > 0)
                {
                    if (section2.GetKeyword("GlobalResistanceNumber", 1).GetParameter(1).IsString())
                    {
                        roughnessList.Add("Global: " + section2.GetKeyword("GlobalResistanceNumber", 1).GetParameter(1).ToString());
                    }
                    else
                    {
                        roughnessList.Add("Global: " + section2.GetKeyword("GlobalResistanceNumber", 1).GetParameter(1).ToDouble().ToString());
                    }
                    
                }
                    
                if (section1.GetSectionsCount("Global_Values") > 0)
                {
                    if (section2.GetKeyword("G_resistance", 1).GetParameter(1).IsString())
                    {
                        roughnessList.Add("Global: " + section2.GetKeyword("G_resistance", 1).GetParameter(1).ToString());
                    }
                    else
                    {
                        roughnessList.Add("Global: " + section2.GetKeyword("G_resistance", 1).GetParameter(1).ToDouble().ToString());
                    }
                    
                    section1 = target.GetSection("BedList", 1);

                    for (int i = 1; i <= section1.GetKeywordsCount("DATA"); i++)
                    {
                        if (section1.GetKeyword("DATA", i).GetParameter(3).IsString())
                        {
                            roughnessList.Add(section1.GetKeyword("DATA", i).GetParameter(1).ToString() + ": " + section1.GetKeyword("DATA", i).GetParameter(3).ToString());
                        }
                        else
                        {
                            roughnessList.Add(section1.GetKeyword("DATA", i).GetParameter(1).ToString() + ": " + section1.GetKeyword("DATA", i).GetParameter(3).ToDouble().ToString());
                        }
                    }
                }
                    
                if (section2.GetSectionsCount("AREA_1") > 0 || section2.GetSectionsCount("MANNING_NUMBER") > 0)
                {
                    PFSSection section3 = section2.GetSectionsCount("AREA_1") > 0 ? section2.GetSection("AREA_1", 1) : section2.GetSection("MANNING_NUMBER", 1);
                    
                    if (section3.GetKeyword("constant_value", 1).GetParameter(1).IsString())
                    {
                        roughnessList.Add("Global: " + section3.GetKeyword("constant_value", 1).GetParameter(1).ToString());
                    }
                    else
                    {
                        roughnessList.Add("Global: " + section3.GetKeyword("constant_value", 1).GetParameter(1).ToDouble().ToString());
                    }
                }

                return roughnessList;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyRoughness(string filePath, string newFilePath, double newValue)
        {
            PFSFile pfsFile = File.Exists(newFilePath) ? pfsFile = new PFSFile(newFilePath, true) : pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target = pfsFile.GetTarget(1);

                PFSSection section1 = target.GetSection("HYDRODYNAMIC_MODULE", 1);

                PFSSection section2 = section1.GetSectionsCount("RESISTANCE") > 0 ? section1.GetSection("RESISTANCE", 1) : section1.GetSection("BED_RESISTANCE", 1);

                try
                {
                    PFSSection section3 = section2.GetSection("AREA_1", 1);
                    section3.GetKeyword("Constant_Value", 1).DeleteParameter(1);
                    section3.GetKeyword("Constant_Value", 1).InsertNewParameterDouble(newValue, 1);
                }
                catch (Exception)
                {
                    PFSSection section3 = section2.GetSection("MANNING_NUMBER", 1);
                    section3.GetKeyword("constant_value", 1).DeleteParameter(1);
                    section3.GetKeyword("constant_value", 1).InsertNewParameterDouble(newValue, 1);
                }

                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyRoughness(string filePath, string newFilePath, string zone, double newValue)
        {
            PFSFile pfsFile = File.Exists(newFilePath) ? pfsFile = new PFSFile(newFilePath, true) : pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target = pfsFile.GetTarget(1);
                
                PFSSection section1 = target.GetSectionsCount("HYDRODYNAMIC_MODULE") > 0 ? target.GetSection("HYDRODYNAMIC_MODULE", 1) : (target.GetSectionsCount("Global_Variables") > 0 ? target.GetSection("Global_Variables", 1) : target.GetSection("MH_HDParameters", 1));
                PFSSection section2 = section1.GetSectionsCount("Global_Values") > 0 ? section1.GetSection("Global_Values", 1) : (section1.GetSectionsCount("RESISTANCE") > 0 ? section1.GetSection("RESISTANCE", 1) : (section1.GetSectionsCount("BED_RESISTANCE") > 0 ? section1.GetSection("BED_RESISTANCE", 1) : section1.GetSection("MH_BED_RESISTANCE", 1)));

                if (zone == "Global" && section2.GetKeywordsCount("G_resistance") > 0)
                {
                    //section2.GetKeyword("G_resistance", 1).GetParameter(1).ModifyDoubleParameter(newValue);
                    section2.GetKeyword("G_resistance", 1).DeleteParameter(1);
                    section2.GetKeyword("G_resistance", 1).InsertNewParameterDouble(newValue, 1);
                }

                if (target.GetSectionsCount("BedList") > 0)
                {
                    section1 = target.GetSection("BedList", 1);
                    for (int i = 1; i <= section1.GetKeywordsCount("DATA"); i++)
                    {
                        if (zone == section1.GetKeyword("DATA", i).GetParameter(1).ToString())
                        {
                            //section1.GetKeyword("DATA", i).GetParameter(3).ModifyDoubleParameter(newValue);
                            section1.GetKeyword("DATA", i).DeleteParameter(1);
                            section1.GetKeyword("DATA", i).InsertNewParameterDouble(newValue, 1);
                        }
                    }
                }

                if (zone == "Global" && section2.GetKeywordsCount("GlobalResistanceNumber") > 0)
                {
                    section2.GetKeyword("GlobalResistanceNumber", 1).DeleteParameter(1);
                    section2.GetKeyword("GlobalResistanceNumber", 1).InsertNewParameterDouble(newValue, 1);
                }

                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static string GetRoughnessFile(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE21_NESTED_MODEL"
                //"FemEngineHD"

                PFSSection section1 = target.GetSection("HYDRODYNAMIC_MODULE", 1);

                PFSSection section2 = section1.GetSectionsCount("RESISTANCE") > 0 ? section1.GetSection("RESISTANCE", 1) : section1.GetSection("BED_RESISTANCE", 1);
                
                if (section2.GetSectionsCount("AREA_1") > 0)
                {
                    PFSSection section3 = section2.GetSection("AREA_1", 1);
                    PFSSection section4 = section3.GetSection("DATA_FILE", 1);
                    return section4.GetKeyword("FILE_NAME", 1).GetParameter(1).IsFilename() ? section4.GetKeyword("FILE_NAME", 1).GetParameter(1).ToFileName() : section4.GetKeyword("FILE_NAME", 1).GetParameter(1).ToString();
                }
                else
                {
                    PFSSection section3 = section2.GetSection("MANNING_NUMBER", 1);
                    return section3.GetKeyword("file_name", 1).GetParameter(1).IsFilename() ? section3.GetKeyword("file_name", 1).GetParameter(1).ToFileName() : section3.GetKeyword("file_name", 1).GetParameter(1).ToString();
                }
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyRoughnessFile(string filePath, string newFilePath, string newRoughnessFilePath)
        {
            PFSFile pfsFile;
            if (File.Exists(newFilePath))
            {
                pfsFile = new PFSFile(newFilePath, true);
            }
            else
            {
                pfsFile = new PFSFile(filePath, true);
            }

            try
            {
                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE21_NESTED_MODEL"
                //"FemEngineHD"

                PFSSection section1 = target.GetSection("HYDRODYNAMIC_MODULE", 1);

                PFSSection section2;
                try
                {
                    section2 = section1.GetSection("RESISTANCE", 1);
                }
                catch (Exception)
                {
                    section2 = section1.GetSection("BED_RESISTANCE", 1);
                }

                try
                {
                    PFSSection section3 = section2.GetSection("AREA_1", 1);
                    PFSSection section4 = section3.GetSection("DATA_FILE", 1);
                    section4.GetKeyword("FILE_NAME", 1).GetParameter(1).ModifyFileNameParameter(newRoughnessFilePath);
                }
                catch (Exception)
                {
                    PFSSection section3 = section2.GetSection("MANNING_NUMBER", 1);
                    section3.GetKeyword("file_name", 1).GetParameter(1).ModifyFileNameParameter(newRoughnessFilePath);
                }

                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static string GetBathymetryFile(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                string fileName;

                PFSSection target = pfsFile.GetTarget(1);

                PFSSection section1 = target.GetSectionsCount("NESTED_MODEL_GLOBALS") > 0 ? target.GetSection("NESTED_MODEL_GLOBALS", 1) : (target.GetSectionsCount("DOMAIN") > 0 ? target.GetSection("DOMAIN", 1) : target.GetSection("MH_River_Network", 1));

                try
                {
                    PFSSection section2 = section1.GetSection("BATHYMETRY_SELECTION", 1);

                    PFSSection section3 = section2.GetSection("AREA_1", 1);

                    PFSSection section4 = section3.GetSection("DATA_FILE", 1);

                    fileName = section4.GetKeyword("FILE_NAME", 1).GetParameter(1).ToFileName();

                    return fileName;
                } catch (Exception) {}

                try 
                {
                    fileName = section1.GetKeyword("file_name", 1).GetParameter(1).ToFileName();

                    return fileName;
                } catch (Exception) {}
                
                try
                {
                    PFSSection section2 = section1.GetSection("MH_Cross_Sections", 1);

                    fileName = section2.GetKeyword("CrossSectionsFileName", 1).GetParameter(1).ToFileName();

                    return fileName;
                }
                catch (Exception) { }

                throw new Exception("Not a valid bathymetry file");
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyBathymetryFile(string filePath, string newFilePath, string newRoughnessFilePath)
        {
            PFSFile pfsFile = File.Exists(newFilePath) ? pfsFile = new PFSFile(newFilePath, true) : new PFSFile(filePath, true);
            
            try
            {
                PFSSection target = pfsFile.GetTarget(1);

                PFSSection section1 = target.GetSectionsCount("NESTED_MODEL_GLOBALS") > 0 ? target.GetSection("NESTED_MODEL_GLOBALS", 1) : (target.GetSectionsCount("DOMAIN") > 0 ? target.GetSection("DOMAIN", 1) : target.GetSection("MH_River_Network", 1));
                
                try
                {
                    PFSSection section2 = section1.GetSection("BATHYMETRY_SELECTION", 1);

                    PFSSection section3 = section2.GetSection("AREA_1", 1);

                    PFSSection section4 = section3.GetSection("DATA_FILE", 1);

                    section4.GetKeyword("FILE_NAME", 1).GetParameter(1).ModifyFileNameParameter(newRoughnessFilePath);
                }
                catch (Exception) { }

                try
                {
                    section1.GetKeyword("file_name", 1).GetParameter(1).ModifyFileNameParameter(newRoughnessFilePath);
                }
                catch (Exception) { }

                try
                {
                    PFSSection section2 = section1.GetSection("MH_Cross_Sections", 1);
                    section2.GetKeyword("CrossSectionsFileName", 1).GetParameter(1).ModifyFileNameParameter(newRoughnessFilePath);
                }
                catch (Exception) { }

                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static List<string> GetResultFileList(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target = pfsFile.GetTarget(1);                
                //"MIKE21_NESTED_MODEL"
                //"FEMEngineHD"
                //"Run11"

                PFSSection section1 = target.GetSectionsCount("HYDRODYNAMIC_MODULE") > 0 ? target.GetSection("HYDRODYNAMIC_MODULE", 1) : (target.GetSectionsCount("Results") > 0 ? target.GetSection("Results", 1) : target.GetSection("ResultSpecificationsGroupPfs", 1));
                
                if (section1.GetKeywordsCount("hd") > 0)
                {
                    if (section1.GetKeyword("hd", 1).GetParameter(1).IsFilename())
                    {
                        return new List<string> { section1.GetKeyword("hd", 1).GetParameter(1).ToFileName() };
                    }
                    else if (section1.GetKeyword("hd", 1).GetParameter(1).IsString())
                    {
                        return new List<string> { section1.GetKeyword("hd", 1).GetParameter(1).ToString() };
                    }
                    else
                    {
                        throw new Exception("Result must be FileName or String");
                    }
                }

                PFSSection section2 = section1.GetSectionsCount("OUTPUT_SPECIFICATION") > 0 ? section1.GetSection("OUTPUT_SPECIFICATION", 1) : (section1.GetSectionsCount("OUTPUTS") > 0 ? section1.GetSection("OUTPUTS", 1) : section1.GetSection("RiverResult", 1));

                if (section2.GetKeywordsCount("ResultsFileName") > 0)
                {
                    if (section2.GetKeyword("ResultsFileName", 1).GetParameter(1).IsFilename())
                    {
                        return new List<string> { section2.GetKeyword("ResultsFileName", 1).GetParameter(1).ToFileName() };
                    }
                    else if (section2.GetKeyword("ResultsFileName", 1).GetParameter(1).IsString())
                    {
                        return new List<string> { section2.GetKeyword("ResultsFileName", 1).GetParameter(1).ToString() };
                    }
                    else
                    {
                        throw new Exception("Result must be FileName or String");
                    }
                }

                List<PFSSection> sectionList = new List<PFSSection>();
                sectionList.Add(section2.GetSectionsCount("OUTPUT_AREA_1") > 0 ? section2.GetSection("OUTPUT_AREA_1", 1) : section2.GetSection("OUTPUT_1", 1));
                if (section2.GetSectionsCount("OUTPUT_2") > 0)
                {
                    sectionList.Add(section2.GetSection("OUTPUT_2", 1));
                }

                List<string> result = new List<string>();

                foreach (PFSSection pfsSection in sectionList)
                {
                    if (pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).IsFilename())
                    {
                        result.Add(pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).ToFileName());
                    }
                    else if (pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).IsString())
                    {
                        result.Add(pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).ToString());
                    }
                    else
                    {
                        throw new Exception("Result must be FileName or String");
                    }
                }
                
                return result;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyResultFile(string filePath, string newFilePath, string newResultFilePath)
        {
            PFSFile pfsFile;
            if (File.Exists(newFilePath))
            {
                pfsFile = new PFSFile(newFilePath, true);
            }
            else
            {
                pfsFile = new PFSFile(filePath, true);
            }

            try
            {
                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE21_NESTED_MODEL"
                //"FEMEngineHD"
                //"Run11"

                PFSSection section1 = target.GetSectionsCount("HYDRODYNAMIC_MODULE") > 0 ? target.GetSection("HYDRODYNAMIC_MODULE", 1) : (target.GetSectionsCount("Results") > 0 ? target.GetSection("Results", 1) : target.GetSection("ResultSpecificationsGroupPfs", 1));
                
                if (section1.GetKeywordsCount("hd") > 0)
                {
                    if (section1.GetKeyword("hd", 1).GetParameter(1).IsFilename())
                    {
                        section1.GetKeyword("hd", 1).GetParameter(1).ModifyFileNameParameter(newResultFilePath);
                    }
                    else if (section1.GetKeyword("hd", 1).GetParameter(1).IsString())
                    {
                        //section1.GetKeyword("hd", 1).GetParameter(1).ModifyStringParameter(newResultFilePath.Replace(Path.GetDirectoryName(newFilePath) + @"\", string.Empty));
                        section1.GetKeyword("hd", 1).DeleteParameter(1);
                        section1.GetKeyword("hd", 1).InsertNewParameterFileName(newResultFilePath.Replace(Path.GetDirectoryName(newFilePath) + @"\", string.Empty), 1);
                    }
                    else
                    {
                        throw new Exception("Result must be FileName or String");
                    }
                }
                else
                {
                    PFSSection section2 = section1.GetSectionsCount("OUTPUT_SPECIFICATION") > 0 ? section1.GetSection("OUTPUT_SPECIFICATION", 1) : (section1.GetSectionsCount("OUTPUTS") > 0 ? section1.GetSection("OUTPUTS", 1) : section1.GetSection("RiverResult", 1));

                    if (section2.GetKeywordsCount("ResultsFileName") > 0)
                    {
                        if (section2.GetKeyword("ResultsFileName", 1).GetParameter(1).IsFilename())
                        {
                            section2.GetKeyword("ResultsFileName", 1).GetParameter(1).ModifyFileNameParameter(newResultFilePath); ;
                        }
                        else if (section2.GetKeyword("ResultsFileName", 1).GetParameter(1).IsString())
                        {
                            //section2.GetKeyword("ResultsFileName", 1).GetParameter(1).ModifyStringParameter(newResultFilePath.Replace(Path.GetDirectoryName(newFilePath) + @"\", string.Empty));
                            section2.GetKeyword("ResultsFileName", 1).DeleteParameter(1);
                            section2.GetKeyword("ResultsFileName", 1).InsertNewParameterFileName(newResultFilePath.Replace(Path.GetDirectoryName(newFilePath) + @"\", string.Empty), 1);
                        }
                        else
                        {
                            throw new Exception("Result must be FileName or String");
                        }
                    }
                    else
                    {
                        List<PFSSection> sectionList = new List<PFSSection>();
                        sectionList.Add(section2.GetSectionsCount("OUTPUT_AREA_1") > 0 ? section2.GetSection("OUTPUT_AREA_1", 1) : section2.GetSection("OUTPUT_1", 1));
                        if (section2.GetSectionsCount("OUTPUT_2") > 0)
                        {
                            sectionList.Add(section2.GetSection("OUTPUT_2", 1));
                        }

                        foreach (PFSSection pfsSection in sectionList)
                        {
                            if (pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).IsFilename())
                            {
                                if (Path.GetFileNameWithoutExtension(newResultFilePath).Contains(Path.GetFileNameWithoutExtension(pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).ToFileName())))
                                {
                                    pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).ModifyFileNameParameter(newResultFilePath);
                                }
                            }
                            else if (pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).IsString())
                            {
                                if (Path.GetFileNameWithoutExtension(newResultFilePath).Contains(Path.GetFileNameWithoutExtension(pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).ToString())))
                                {
                                    //pfsSection.GetKeyword("FILE_NAME", 1).GetParameter(1).ModifyStringParameter(newResultFilePath.Replace(Path.GetDirectoryName(newFilePath) + @"\", string.Empty));
                                    pfsSection.GetKeyword("FILE_NAME", 1).DeleteParameter(1);
                                    pfsSection.GetKeyword("FILE_NAME", 1).InsertNewParameterFileName(newResultFilePath.Replace(Path.GetDirectoryName(newFilePath) + @"\", string.Empty), 1);
                                }
                            }
                            else
                            {
                                throw new Exception("M21 Result must be FileName or String");
                            }
                        }
                    }
                }

                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static string GetStatisticsFile(string filePath)
        {
            PFSFile pfsFile = new PFSFile(filePath, true);

            try
            {
                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE21_NESTED_MODEL"
                //"FEMEngineHD"
                //"Run11"

                if (target.GetSectionsCount("HYDRODYNAMIC_MODULE") > 0)
                {
                    PFSSection section1 = target.GetSection("HYDRODYNAMIC_MODULE", 1);

                    if (section1.GetSectionsCount("OUTPUT_SPECIFICATION") > 0)
                    {
                        PFSSection section2 = section1.GetSection("OUTPUT_SPECIFICATION", 1);

                        if (section2.GetSectionsCount("INUNDATION_STATISTICS") > 0)
                        {
                            PFSSection section3 = section2.GetSection("INUNDATION_STATISTICS", 1);

                            if (section3.GetKeyword("InundationFileName", 1).GetParameter(1).IsFilename())
                            {
                                return section3.GetKeyword("InundationFileName", 1).GetParameter(1).ToFileName();
                            }
                            else if (section3.GetKeyword("InundationFileName", 1).GetParameter(1).IsString())
                            {
                                return section3.GetKeyword("InundationFileName", 1).GetParameter(1).ToString();
                            }
                            else
                            {
                                throw new Exception("Statistic must be FileName or String");
                            }
                        }
                    }
                }
                return string.Empty;
            }
            finally
            {
                pfsFile.Close();
            }
        }

        public static void ModifyStatisticsFile(string filePath, string newFilePath, string newResultFilePath)
        {
            PFSFile pfsFile;
            if (File.Exists(newFilePath))
            {
                pfsFile = new PFSFile(newFilePath, true);
            }
            else
            {
                pfsFile = new PFSFile(filePath, true);
            }

            try
            {
                PFSSection target = pfsFile.GetTarget(1);
                //"MIKE21_NESTED_MODEL"
                //"FEMEngineHD"
                //"Run11"

                if (target.GetSectionsCount("HYDRODYNAMIC_MODULE") > 0)
                {
                    PFSSection section1 = target.GetSection("HYDRODYNAMIC_MODULE", 1);

                    if (section1.GetSectionsCount("OUTPUT_SPECIFICATION") > 0)
                    {
                        PFSSection section2 = section1.GetSection("OUTPUT_SPECIFICATION", 1);

                        if (section2.GetSectionsCount("INUNDATION_STATISTICS") > 0)
                        {
                            PFSSection section3 = section2.GetSection("INUNDATION_STATISTICS", 1);

                            if (section3.GetKeyword("InundationFileName", 1).GetParameter(1).IsFilename())
                            {
                                section3.GetKeyword("InundationFileName", 1).GetParameter(1).ModifyFileNameParameter(newResultFilePath);
                            }
                            else if (section3.GetKeyword("InundationFileName", 1).GetParameter(1).IsString())
                            {
                                section3.GetKeyword("InundationFileName", 1).GetParameter(1).ModifyStringParameter(newResultFilePath.Replace(Path.GetDirectoryName(newFilePath), string.Empty));
                            }
                            else
                            {
                                throw new Exception("Statistic must be FileName or String");
                            }
                        }
                    }
                }
                pfsFile.Write(newFilePath);
            }
            finally
            {
                pfsFile.Close();
            }
        }
    }
}
