using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Intervallo.Util
{
    public static class StreamExtension
    {

        public static void WriteInt(this Stream stream, int value)
        {
            const int IntSize = sizeof(int);

            for (var d = 0; d < IntSize; d++, value >>= 8)
            {
                stream.WriteByte((byte)(value & 0xff));
            }
        }

        public static void WriteShort(this Stream stream, short value)
        {
            const int ShortSize = sizeof(short);

            for (var d = 0; d < ShortSize; d++, value >>= 8)
            {
                stream.WriteByte((byte)(value & 0xff));
            }
        }
    }
}
