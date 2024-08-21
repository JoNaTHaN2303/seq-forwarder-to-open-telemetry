using System;

namespace Seq.Forwarder.Util
{
    public static class GuidExtensions
    {
        public static string ToSpanId(this Guid guid)
        {
            // Extract the first 8 bytes of the GUID
            byte[] bytes = guid.ToByteArray();
            Array.Resize(ref bytes, 8);

            // Convert the bytes to a hexadecimal string
            string spanId = BitConverter.ToString(bytes).Replace("-", string.Empty).ToUpper();

            return spanId;
        }
    }
}
