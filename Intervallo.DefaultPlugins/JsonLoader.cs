using Intervallo.DefaultPlugins.Properties;
using Intervallo.Plugin;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;

namespace Intervallo.DefaultPlugins
{
    [Export(typeof(IScaleLoader))]
    public class JsonLoader : IScaleLoader
    {
        public string Copyright => ((AssemblyCopyrightAttribute)typeof(JsonLoader).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;

        public string Description => LangResources.JsonLoader_Description;

        public string PluginName => typeof(JsonLoader).Name;

        public Version Version => new Version(((AssemblyVersionAttribute)typeof(JsonLoader).Assembly.GetCustomAttribute(typeof(AssemblyVersionAttribute))).Version);

        public string[] SupportedFileExtensions => new string[] { "*.json" };

        public double[] Load(string filePath, double framePeriod, int maxFrameLength)
        {
            var data = Load(filePath);

            if (framePeriod == data.FramePeriod)
            {
                return data.Scales;
            }
            else
            {
                framePeriod *= 0.001;
                var dataFramePeriod = data.FramePeriod * 0.001;
                var result = new double[maxFrameLength];
                for (var i = 0; i < maxFrameLength; i++)
                {
                    var currentTime = i * framePeriod;
                    var dataIndex = (int)(currentTime / dataFramePeriod);
                    if (dataIndex < data.Scales.Length - 1)
                    {
                        switch (data.InterpolationType)
                        {
                            case ScaleInterpolationType.Linear:
                                result[i] = Frequency.FromNoteNumber(
                                    Interpolation.Linear(
                                        data.Scales[dataIndex],
                                        data.Scales[dataIndex + 1],
                                        dataIndex * dataFramePeriod,
                                        (dataIndex + 1) * dataFramePeriod,
                                        currentTime
                                    )
                                );
                                break;
                            case ScaleInterpolationType.CatmullRom:
                                result[i] = Frequency.FromNoteNumber(
                                        Interpolation.CatmullRom(
                                        data.Scales[Math.Max(0, dataIndex - 1)],
                                        data.Scales[dataIndex],
                                        data.Scales[dataIndex + 1],
                                        data.Scales[Math.Min(dataIndex + 1, data.Scales.Length - 1)],
                                        dataIndex * dataFramePeriod,
                                        (dataIndex + 1) * dataFramePeriod,
                                        currentTime
                                    )
                                );
                                break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                return result;
            }
        }

        ScaleData Load(string filePath)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(ScaleData));
                    var data = serializer.ReadObject(fs) as ScaleData;
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            catch (Exception e)
            {
                throw new ScaleLoadException(LangResources.JsonLoader_FailLoadFile, e);
            }

            throw new ScaleLoadException(LangResources.JsonLoader_FailLoadFile);
        }
    }

    public class ScaleData
    {
        public double FramePeriod { get; set; }

        public ScaleInterpolationType InterpolationType { get; set; }

        public double[] Scales { get; set; }
    }

    public enum ScaleInterpolationType
    {
        Linear,
        CatmullRom
    }
}
