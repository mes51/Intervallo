using Intervallo.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Cache
{
    public static class CacheFile
    {
        static readonly string CacheDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Cache");

        public static void CreateCacheDirectory()
        {
            try
            {
                Directory.CreateDirectory(CacheDirectory);
            }
            catch { }
        }

        public static void ClearChaceFile()
        {
            try
            {
                Directory.GetFiles(CacheDirectory)
                    .Where((s) => Path.GetExtension(s) == ".cache")
                    .ForEach((s) => File.Delete(s));
            }
            catch { }
        }

        public static Optional<T> FindCache<T>(string hash)
        {
            if (!Directory.Exists(CacheDirectory))
            {
                return Optional<T>.None();
            }

            var filePath = Path.Combine(CacheDirectory, CreateCacheName(typeof(T), hash));
            if (!File.Exists(filePath))
            {
                return Optional<T>.None();
            }

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
                    return Optional<T>.FromNull((T)formatter.Deserialize(fs));
                }
            }
            catch
            {
                return Optional<T>.None();
            }
        }

        public static void SaveCache(object value, string hash)
        {
            var filePath = Path.Combine(CacheDirectory, CreateCacheName(value.GetType(), hash));
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Create))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, value);
                }
            }
            catch { }
        }

        static string CreateCacheName(Type type, string hash)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            using (var algorithm = SHA256.Create())
            {
                writer.Write(type.FullName);
                writer.Write(hash);
                ms.Position = 0;
                return string.Join("", algorithm.ComputeHash(ms).Select((x) => x.ToString("X2"))) + ".cache";
            }
        }
    }
}
