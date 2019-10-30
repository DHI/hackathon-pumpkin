using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Mike1D.CrossSectionModule;
using DHI.Mike1D.Generic;
using Newtonsoft.Json.Linq;

namespace DHI.Services.ARRWebPortal
{
    public static class CrossSection
    {

        public static void ChangeDatum(string filePath, string branch, string topo, double value, JArray jArray)
        {
            // Load cross section data
            Diagnostics diagnostics = new Diagnostics("Errors");
            CrossSectionDataFactory csDataFactory = new CrossSectionDataFactory();
            CrossSectionData crossSectionData = csDataFactory.Open(Connection.Create(filePath), diagnostics);

            LocationSpan locationSpan = new LocationSpan(branch, 0, double.MaxValue);
            IList<ICrossSection> crossSectionList = crossSectionData.FindCrossSectionsForLocationSpan(locationSpan, topo, false);
            foreach (ICrossSection crossSection in crossSectionList)
            {
                if (jArray.Any(p => p["branchtopo"].ToObject<string>() == branch + ":" + topo && p["chainage"].ToObject<double>() == crossSection.Location.Chainage && p["checked"].ToObject<bool>()))
                {
                    crossSection.Location.Z += value;
                }

                //XSBaseRaw xsBaseRaw = crossSection.BaseCrossSection as XSBaseRaw;
                //xsBaseRaw.CalculateProcessedData();

                //IDiagnostics diagnostics2 = xsBaseRaw.Validate();

                //if (diagnostics.ErrorCountRecursive > 0)
                //{
                //    throw new Exception(String.Format("Number of errors: {0}", diagnostics.Errors.Count));
                //}
            }

            CrossSectionDataFactory.Save(crossSectionData);

            if (diagnostics.ErrorCountRecursive > 0)
            {
            throw new Exception(String.Format("Number of errors: {0}", diagnostics.Errors.Count));
            }
        }

        public static string GetChainages(List<string> branchTopoList, string filePath)
        {
            JArray jArray = new JArray();

            foreach (string branchTopo in branchTopoList)
            {
                string branch = branchTopo.Split(new char[] { ':' })[0];
                string topo = branchTopo.Split(new char[] { ':' })[1];

                // Load cross section data
                Diagnostics diagnostics = new Diagnostics("Errors");
                CrossSectionDataFactory csDataFactory = new CrossSectionDataFactory();
                CrossSectionData crossSectionData = csDataFactory.Open(Connection.Create(filePath), diagnostics);

                LocationSpan locationSpan = new LocationSpan(branch, 0, double.MaxValue);
                IList<ICrossSection> crossSectionList = crossSectionData.FindCrossSectionsForLocationSpan(locationSpan, topo, false);
                foreach (ICrossSection crossSection in crossSectionList)
                {
                    JObject jObject = new JObject();
                    jObject.Add("branchtopo", branch + ":" + topo);
                    jObject.Add("chainage", crossSection.Location.Chainage);
                    jObject.Add("checked", crossSectionList.IndexOf(crossSection) == 0 ? false : true);
                    jObject.Add("visible", false);
                    jObject.Add("locked", crossSectionList.IndexOf(crossSection) == 0 ? true : false);
                    jArray.Add(jObject);
                }

                if (diagnostics.ErrorCountRecursive > 0)
                {
                    throw new Exception(String.Format("Number of errors: {0}", diagnostics.Errors.Count));
                }
            }

            return jArray.ToString();
        }
    }
}
