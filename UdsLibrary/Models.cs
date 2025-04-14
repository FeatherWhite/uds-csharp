using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triumph.Uds
{
    public class UDSRDBIVar<T>
    {
        public ushort DID;
        public ushort Len;
        public byte[] Data;
        public Func<byte[], byte[],int, int,T> UnpackFn;
    }
}
