using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiDetector
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    public sealed class LineReader : IEnumerable<string>
    {
        readonly Func<TextReader> dataSource;
        public LineReader(Func<Stream> streamSource)
            : this(streamSource, Encoding.UTF8)
        {
        }
        public LineReader(Func<Stream> streamSource, Encoding encoding)
            : this(() => new StreamReader(streamSource(), encoding))
        {
        }
        public LineReader(string filename)
            : this(filename, Encoding.UTF8)
        {
        }
        public LineReader(string filename, Encoding encoding)
            : this(() => new StreamReader(filename, encoding))
        {
        }
        public LineReader(Func<TextReader> dataSource)
        {
            this.dataSource = dataSource;
        }
        public IEnumerator<string> GetEnumerator()
        {
            using (TextReader reader = dataSource())
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
