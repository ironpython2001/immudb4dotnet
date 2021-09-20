using System;
using System.IO;
using System.Text;

namespace ImmuDbDotnetLib
{
    public class Helper
    {
        private int bufferSize = 16384;
        public void ReadFile(string filename)
        {
            var stringBuilder = new StringBuilder();
            var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);

            using var streamReader = new StreamReader(fileStream);
            char[] fileContents = new char[this.bufferSize];
            int charsRead = streamReader.Read(fileContents, 0, this.bufferSize);

            // Can't do much with 0 bytes
            if (charsRead == 0)
            {
                throw new Exception("File is 0 bytes");
            }

            while (charsRead > 0)
            {
                stringBuilder.Append(fileContents);
                charsRead = streamReader.Read(fileContents, 0, this.bufferSize);
            }
        }
    }
}
