using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DHI.Services.ARRWebPortal
{
    public static class ProcessMike11
    {
        public static void RandomizeManningRoughness(List<KeyValuePair<string, Stream>> streamList)
        {
            foreach (KeyValuePair<string, Stream> streamVp in streamList)
            {
                if (Path.GetExtension(streamVp.Key).ToLower()==".sim11")
                {

                    string sim11FilePath = Path.GetTempFileName();
                    PFS.Stream2File(streamVp.Value, sim11FilePath);

                    try
                    {
                        PFS.ModifyPFSSimHDFile(sim11FilePath, "newName");
                    }
                    catch (Exception e)
                    {

                    }
                }
                
                if (Path.GetExtension(streamVp.Key).ToLower()==".hd11")
                {
                    string hd11FilePath = Path.GetTempFileName();
                    PFS.Stream2File(streamVp.Value, hd11FilePath);

                    try
                    {
                        PFS.ModifyPFSManningRoughness(hd11FilePath, 50);
                    }
                    catch (Exception e)
                    {

                    }

                    
                }
            }
        }
    }
}
