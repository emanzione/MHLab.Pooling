using System.Buffers;

namespace MHLab.Pooling
{
    public class BytesPool
    {
        private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;

        public static byte[] Get(int size)
        {
            return Pool.Rent(size);
        }

        public static void Recycle(byte[] buffer, bool clear = false)
        {
            Pool.Return(buffer, clear);
        }
    }
}
