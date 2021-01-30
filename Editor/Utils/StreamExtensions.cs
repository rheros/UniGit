﻿using System.IO;

namespace UniGit.Utils
{
    internal static class StreamExtensions
    {
        public static void CopyTo(this Stream input, Stream output)
        {
            lock (input)
            {
                var buffer = new byte[16 * 1024]; // Fairly arbitrary size
                int bytesRead;

                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                }
            }
        }
    }
}