using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppCutAudio.Helpers
{
    public class AsyncFileWriter
    {
        public async Task WriteToFileAsync(string filePath, string data)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(data);

            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }

        internal async Task WriteAllBytesAsync(string filePath, byte[] encodedText)
        {
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }
    }
}
