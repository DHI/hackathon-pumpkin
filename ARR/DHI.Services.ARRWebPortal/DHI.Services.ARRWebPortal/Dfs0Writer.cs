using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DHI.Generic.MikeZero;
using DHI.Generic.MikeZero.DFS;
using System.IO;

namespace DHI.Services.ARRWebPortal
{
    public class Dfs0Writer : IDisposable
    {
        private string _dfs0FilePath = null;
        private IDfsFile _dfs0File = null;
        private DateTime _start = DateTime.MinValue;
        
        public void Dispose()
        {
            if (_dfs0File != null)
            {
                _dfs0File.Close();
                _dfs0File.Dispose();
                File.Delete(_dfs0FilePath);
            }
        }

        public void Close()
        {
            if (_dfs0File != null)
            {
                _dfs0File.Close();
            }
        }

        public Stream OpenStream()
        {
            return File.OpenRead(_dfs0FilePath);
        }

        public string FilePath()
        {
            return _dfs0FilePath;
        }
        
        public void Delete()
        {
            if (File.Exists(_dfs0FilePath))
            {
                _dfs0File.Dispose();
                File.Delete(_dfs0FilePath);
            }
        }

        public Dfs0Writer(DateTime start, List<string> catchmentNameList, string type, double timeStepSeconds, bool addRiverLevels = false)
        {
            _dfs0FilePath = Path.GetTempFileName(); //Path.Combine(Path.GetTempPath(), Path.ChangeExtension(fileName, ".dfs0"));
            
            _start = start;

            DfsFactory factory = new DfsFactory();
            DfsBuilder builder = DfsBuilder.Create("Import from " + type.ToUpper(), "dfs Timeseries Bridge", 10000);

            // Set up file header
            builder.SetDataType(1);
            builder.SetGeographicalProjection(factory.CreateProjectionUndefined());
            builder.SetTemporalAxis(factory.CreateTemporalEqCalendarAxis(eumUnit.eumUsec, start, 0, timeStepSeconds));

            builder.SetItemStatisticsType(StatType.RegularStat);

            foreach (string catchmentName in catchmentNameList)
            {
                DfsDynamicItemBuilder item1 = builder.CreateDynamicItemBuilder();
                item1.Set(catchmentName, eumQuantity.Create(eumItem.eumIDischarge, eumUnit.eumUm3PerSec), DfsSimpleType.Float);
                
                item1.SetValueType(DataValueType.Instantaneous);
                item1.SetAxis(factory.CreateAxisEqD0());
                builder.AddDynamicItem(item1.GetDynamicItemInfo());   
            }

            if (addRiverLevels)
            {
                foreach (string catchmentName in catchmentNameList)
                {
                    DfsDynamicItemBuilder item1 = builder.CreateDynamicItemBuilder();
                    item1.Set(catchmentName, eumQuantity.Create(eumItem.eumIWaterLevel, eumUnit.eumUmeter), DfsSimpleType.Float);

                    item1.SetValueType(DataValueType.Instantaneous);
                    item1.SetAxis(factory.CreateAxisEqD0());
                    builder.AddDynamicItem(item1.GetDynamicItemInfo());
                }
            }

            //add velocity
            DfsDynamicItemBuilder velocityItem = builder.CreateDynamicItemBuilder();
            velocityItem.Set("Velocity", eumQuantity.Create(eumItem.eumICurrentSpeed, eumUnit.eumUmeterPerSec), DfsSimpleType.Float);
            velocityItem.SetValueType(DataValueType.Instantaneous);
            velocityItem.SetAxis(factory.CreateAxisEqD0());
            builder.AddDynamicItem(velocityItem.GetDynamicItemInfo());

            // Create file
            builder.CreateFile(_dfs0FilePath);
            _dfs0File = builder.GetFile();
        }

        public Dfs0Writer(DateTime start, List<string> itemNameList, double timeStepSeconds)
        {
            _dfs0FilePath = Path.GetTempFileName(); 

            _start = start;

            DfsFactory factory = new DfsFactory();
            DfsBuilder builder = DfsBuilder.Create("Rainfall Generator", "dfs Timeseries Bridge", 10000);

            // Set up file header
            builder.SetDataType(1);
            builder.SetGeographicalProjection(factory.CreateProjectionUndefined());
            builder.SetTemporalAxis(factory.CreateTemporalEqCalendarAxis(eumUnit.eumUsec, start, 0, timeStepSeconds));

            builder.SetItemStatisticsType(StatType.RegularStat);

            foreach (string itemName in itemNameList)
            {
                DfsDynamicItemBuilder item1 = builder.CreateDynamicItemBuilder();
                
                if (itemName.ToLower().Contains("rate"))
                {
                    item1.Set(itemName, eumQuantity.Create(eumItem.eumIPrecipitationRate , eumUnit.eumUmillimeterPerDay), DfsSimpleType.Float);
                }
                else
                {
                    item1.Set(itemName, eumQuantity.Create(eumItem.eumIRainfallDepth), DfsSimpleType.Float);
                }

                item1.SetValueType(DataValueType.StepAccumulated);
                item1.SetAxis(factory.CreateAxisEqD0());
                builder.AddDynamicItem(item1.GetDynamicItemInfo());
            }

            // Create file
            builder.CreateFile(_dfs0FilePath);
            _dfs0File = builder.GetFile();
        }

        public void AddData(DateTime dateTime, List<double> values)
        {
            //add velocity
            values.Add(0);
            
            var data = new float[1];
            foreach (var value in values)
            {
                data[0] = (float)value;
                _dfs0File.WriteItemTimeStepNext((dateTime - _start).TotalSeconds, data);
            }
        }

        public void AddRain(DateTime dateTime, List<double> values)
        {
            var data = new float[1];
            foreach (var value in values)
            {
                data[0] = (float)value;
                _dfs0File.WriteItemTimeStepNext((dateTime - _start).TotalSeconds, data);
            }
        }
    }
}
