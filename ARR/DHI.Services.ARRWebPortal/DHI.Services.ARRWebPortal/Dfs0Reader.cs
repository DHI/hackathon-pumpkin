using DHI.Generic.MikeZero.DFS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public static class Dfs0Reader
    {
        public static List<KeyValuePair<DateTime, List<double>>> GetTSData(string base64String)
        {
            string filePath = Path.ChangeExtension(Path.GetTempFileName(), ".dfs0");
            byte[] byteArray = Convert.FromBase64String(base64String);
            File.WriteAllBytes(filePath, byteArray); 
            
            // Open the file as a generic dfs file
            IDfsFile dfs0File = DfsFileFactory.DfsGenericOpen(filePath);

            List<KeyValuePair<DateTime, List<double>>> result = new List<KeyValuePair<DateTime, List<double>>>();

            try
            {
                // we assume double or float in the file
                object deleteValue = null;
                if (dfs0File.ItemInfo[0].DataType == DfsSimpleType.Double)
                {
                    deleteValue = dfs0File.FileInfo.DeleteValueDouble;
                }
                else
                {
                    deleteValue = dfs0File.FileInfo.DeleteValueFloat;
                }

                DateTime start = DateTime.MinValue;
                if (dfs0File.FileInfo.TimeAxis.IsEquidistant())
                {
                    start = (dfs0File.FileInfo.TimeAxis as IDfsEqCalendarAxis).StartDateTime;
                }
                else
                {
                    start = (dfs0File.FileInfo.TimeAxis as IDfsNonEqCalendarAxis).StartDateTime;
                }

                IDfsItemData itemData;
                for (int i = 0; i < dfs0File.FileInfo.TimeAxis.NumberOfTimeSteps; i++)
                {
                    DateTime current = DateTime.MinValue;
                    List<double> values = new List<double>();
                                        
                    for (int j = 0; j < dfs0File.ItemInfo.Count; j++)
                    {
                        itemData = dfs0File.ReadItemTimeStep(j + 1, i);

                        if (j == 0)
                        {
                            current = start.AddSeconds(itemData.TimeInSeconds(dfs0File.FileInfo.TimeAxis));
                        }

                        object v = itemData.Data.GetValue(0);
                        if (!v.Equals(deleteValue))
                        {
                            values.Add(Convert.ToDouble(v));
                        }
                    }

                    result.Add(new KeyValuePair<DateTime, List<double>>(current, values));
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (dfs0File != null)
                {
                    dfs0File.Close();
                    dfs0File.Dispose();
                    File.Delete(filePath);
                }
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return result;
        }

        public static List<string> GetTSItemNames(string base64String)
        {
            List<string> result = new List<string>();

            string filePath = Path.ChangeExtension(Path.GetTempFileName(), ".dfs0");
            byte[] byteArray = Convert.FromBase64String(base64String);
            File.WriteAllBytes(filePath, byteArray);

            IDfsFile dfs0File = DfsFileFactory.DfsGenericOpen(filePath);

            try
            {
                foreach (var itm in dfs0File.ItemInfo)
                {
                    result.Add(itm.Name);
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (dfs0File != null)
                {
                    dfs0File.Close();
                    dfs0File.Dispose();
                    File.Delete(filePath);
                }
            }

            return result;
        }
    }
}
