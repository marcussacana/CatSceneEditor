using ComponentAce;
using System.IO;

namespace CatSceneEditor {
    internal static class Algo {
        internal static void CompressData(byte[] inData, out byte[] outData, int CompressLevel = 9) {
            using (MemoryStream outMemoryStream = new MemoryStream())
            using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, CompressLevel))
            using (Stream inMemoryStream = new MemoryStream(inData)) {
                CopyStream(inMemoryStream, outZStream);
                outZStream.Finish();
                outData = outMemoryStream.ToArray();
            }
        }
        internal static void CompressData(Stream inData, Stream outData, int CompressLevel = 9) {
            MemoryStream tmp = new MemoryStream();
            ZOutputStream outZStream = new ZOutputStream(tmp, CompressLevel);
            CopyStream(inData, outZStream);
            outZStream.Finish();
            tmp.Position = 0;
            CopyStream(tmp, outData);
            outZStream.Close();
            tmp.Close();
        }
        internal static void CopyStream(Stream input, Stream output, long Length = -1) {
            long Readed = 0;
            byte[] Buffer = new byte[2000];
            if (Length == -1) {
                while ((Readed = input.Read(Buffer, 0, Buffer.Length)) > 0)
                    output.Write(Buffer, 0, (int)Readed);

            } else {
                do {
                    Buffer = new byte[Readed + 2000 > Length ? Length - Readed : 2000];
                    Readed += input.Read(Buffer, 0, Buffer.Length);
                    output.Write(Buffer, 0, Buffer.Length);
                } while (Readed < Length);
            }
            output.Flush();
        }
        internal static void DecompressData(byte[] inData, out byte[] outData) {
            try {
                using (Stream inMemoryStream = new MemoryStream(inData))
                using (ZInputStream outZStream = new ZInputStream(inMemoryStream)) {
                    MemoryStream outMemoryStream = new MemoryStream();
                    CopyStream(outZStream, outMemoryStream);
                    outData = outMemoryStream.ToArray();
                }
            }
            catch {
                outData = new byte[0];
            }
        }
        internal static void DecompressData(Stream Input, Stream Output) {
            using (ZInputStream outZStream = new ZInputStream(Input)) {
                CopyStream(outZStream, Output);
            }
        }
    }
}