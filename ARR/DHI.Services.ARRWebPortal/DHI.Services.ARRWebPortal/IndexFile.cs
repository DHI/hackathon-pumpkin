using DHI.Generic.MikeZero.DFS;
using DHI.Generic.MikeZero.DFS.dfs123;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    class IndexFile
    {
        public static Stream CreateIndexFile(string fileName, Stream stream)
        {            
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            Dfs2File dfs2 = DfsFileFactory.Dfs2FileOpenEdit(filePath);
            
            float landValue = (float)dfs2.FileInfo.CustomBlocks[0][3];

            IDfsItemData2D<float> bathyData = (IDfsItemData2D<float>)dfs2.ReadItemTimeStepNext();

            // Modify bathymetry data
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

          // Write back bathymetry data
          dfs2.WriteItemTimeStep(1, 0, 0, bathyData.Data);
          dfs2.Close();

          byte[] byteArray = File.ReadAllBytes(filePath);
          
          File.Delete(filePath);
          
          return new MemoryStream(byteArray);
        }

        public static List<int> GetIndexZones(string fileName, Stream stream)
        {
            List<int> result = new List<int>();
            
            MemoryStream memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            string filePath = Path.GetTempFileName();

            File.WriteAllBytes(filePath, memoryStream.ToArray());

            Dfs2File dfs2 = DfsFileFactory.Dfs2FileOpenEdit(filePath);

           
            IDfsItemData2D<float> bathyData = (IDfsItemData2D<float>)dfs2.ReadItemTimeStepNext();

            // Modify bathymetry data
            for (int i = 0; i < bathyData.Data.Length; i++)
            {
                int value = (int)bathyData.Data[i];

                if (!result.Contains(value))
                {
                    result.Add(value);
                }
            }

            dfs2.Close();

            return result;
        }
    }
}
