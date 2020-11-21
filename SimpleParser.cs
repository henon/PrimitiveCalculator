//MIT License

//Copyright (c) 2020 Meinrad Recheis

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace PrimitiveCalculator
{
    public class SimpleParser
    {
        public SimpleParser(string s)
        {
            Debug.Assert(s != null);
            m_data = s;
        }
        protected string m_data;
        private int i = 0;
        public int Position { get { return i; } set { i = value; } }

        // returns true if the next char is one of the given chars. does not consume
        public bool NextIs(params char[] chars)
        {
            if (i >= m_data.Length)
                return false;
            return chars.Contains(m_data[i]);
        }

        public bool NextIs(string s)
        {
            var current_i = i;
            try {
                foreach (var ch in s) {
                    if (!NextIs(ch))
                        return false;
                    Skip(1);
                }
                return true;
            }
            finally {
                i = current_i;
            }
        }

        public string ConsumeAny(params char[] chars)
        {
            var sb = new StringBuilder();
            while (i < m_data.Length)
            {
                if (chars.Contains(m_data[i]))
                {
                    sb.Append(m_data[i]);
                    i += 1;
                }
                else
                    break;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Reads until it encounters one of the stop chars. Does not return the stopchar as part of the return string but consumes it.
        /// </summary>
        /// <param name="stop_chars"></param>
        /// <returns></returns>
        public string ReadUntil(params char[] stop_chars)
        {
            var s = new StringBuilder();
            while (i < m_data.Length)
            {
                char c1 = m_data[i];
                LastChar = c1;
                i += 1;
                if (stop_chars.Any(stop_char => stop_char == c1))
                    break;
                s.Append(c1);
            }
            return s.ToString();
        }

        /// <summary>
        /// Reads until it encounters the expected string. Does not return the expected string as part of the return string but consumes it.
        /// </summary>
        /// <param name="expected"></param>
        /// <returns></returns>
        public string ReadUntil(string expected)
        {
            var s = new StringBuilder();
            while (i < m_data.Length)
            {
                s.Append(ReadUntil(expected[0]));
                var pos = Position;
                Unskip(1);
                if (NextIs(expected))
                {
                    Skip(expected.Length);
                    break;
                }
                Position = pos;
                if (pos < m_data.Length)
                    s.Append(expected[0]); // we didn't find the expected string, so add the consumed stopchar, or else it would be missing due to ReadUntil(expected[0]) having not returned it.
            }
            return s.ToString();
        }

        public char LastChar
        {
            get;
            private set;
        }

        public char? NextChar
        {
            get
            {
                if (i < m_data.Length)
                    return m_data[i];
                return null;
            }
        }

        public bool HasNext { get { return i < m_data.Length; } }

        public void Skip(int n)
        {
            i += n;
        }

        public void Unskip(int n)
        {
            i = Math.Max(0, i - n);
        }

        public bool SkipUntil(string expected)
        {
            if (m_data.Length < expected.Length)
                return false;
            while (i < m_data.Length)
            {
                SkipUntil(expected[0]);
                var pos = Position;
                Unskip(1);
                if (NextIs(expected))
                {
                    Skip(expected.Length);
                    return true;
                }
                Position = pos;
            }
            return false;
        }

        public bool SkipUntil(params char[] stop_chars)
        {
            while (i < m_data.Length)
            {
                char c1 = m_data[i];
                LastChar = c1;
                i += 1;
                if (stop_chars.Any(stop_char => stop_char == c1))
                    return true;
            }
            return false;
        }
    }

#if DEBUG
    namespace Test
    {
        [TestFixture]
        public class SimpleParserParserTest
        {
            [Test]
            public void ReadUntil()
            {
                Assert.AreEqual("", new SimpleParser("abcabc").ReadUntil('a'));
                Assert.AreEqual("a", new SimpleParser("abcabc").ReadUntil('b'));
                Assert.AreEqual("ab", new SimpleParser("abcabc").ReadUntil('c'));
                Assert.AreEqual("abcabc", new SimpleParser("abcabc").ReadUntil('d'));
                Assert.AreEqual("", new SimpleParser("").ReadUntil('d'));
                var p = new SimpleParser("abc.def ghi");
                Assert.AreEqual('\0', p.LastChar);
                Assert.AreEqual('a', p.NextChar.Value);
                Assert.AreEqual("abc", p.ReadUntil('.', ' '));
                Assert.AreEqual('.', p.LastChar);
                Assert.AreEqual('d', p.NextChar.Value);
                Assert.AreEqual("def", p.ReadUntil('.', ' '));
                Assert.AreEqual(' ', p.LastChar);
                Assert.IsTrue(p.HasNext);
                Assert.AreEqual("ghi", p.ReadUntil());
                Assert.AreEqual('i', p.LastChar);
                Assert.IsNull(p.NextChar);
                Assert.IsFalse(p.HasNext);
            }

            [Test]
            public void ReadUntil1()
            {
                var p = new SimpleParser("rubbish<h1>A headline</h1>");
                p.SkipUntil("<h1>");
                Assert.AreEqual("A headline",p.ReadUntil("</h1>"));
                p.Position=0;
                Assert.AreEqual("rubbish", p.ReadUntil("<h1>"));
                p.Position = 0;
                Assert.AreEqual("rubbish<h1>A headline</h1>", p.ReadUntil("<h2>"));
                p.Position = 0;
                Assert.AreEqual("rubbish<h1", p.ReadUntil(">"));
                Assert.AreEqual("A headline</h1", p.ReadUntil(">"));
            }

            [Test]
            public void ConsumeAny()
            {
                var p = new SimpleParser("abc.def ghi");
                p.ConsumeAny('a', 'b', 'c', 'd', 'e', 'f');
                Assert.AreEqual('.', p.NextChar.Value);
                p.Skip(1);
                p.ConsumeAny('a', 'b', 'c', 'd', 'e', 'f');
                Assert.AreEqual(' ', p.NextChar.Value);
                p.Skip(1);
                p.ConsumeAny('a', 'b', 'c', 'd', 'e', 'f');
                Assert.AreEqual(true, p.NextIs('g'));
            }

            [Test]
            public void ParseCommandParameterStrings()
            {
                var parser = new SimpleParser("\"asdf asdf\" 1 []");
                parser.Skip(1);
                Assert.AreEqual("asdf asdf", parser.ReadUntil('"'));
                parser.Skip(1);
                Assert.AreEqual("1", parser.ReadUntil(' '));
                Assert.AreEqual("[]", parser.ReadUntil(' '));
            }

            [Test]
            public void NextIs()
            {
                var parser = new SimpleParser("<asdf/>");
                Assert.IsFalse(parser.NextIs("asdf"));
                Assert.IsTrue(parser.NextIs('<'));
                Assert.AreEqual(0, parser.Position);
                Assert.IsTrue(parser.NextIs("<asdf/>"));
                Assert.AreEqual(0, parser.Position);
                Assert.IsTrue(parser.NextIs('<'));
                parser.ReadUntil('/');
                parser.Unskip(1);
                Assert.IsTrue(parser.NextIs('/'));
                Assert.IsFalse(parser.NextIs("asdf"));
                Assert.IsTrue(parser.NextIs('/'));
                Assert.IsTrue(parser.NextIs("/>"));
                Assert.IsTrue(parser.NextIs('/'));
            }

            [Test]
            public void SkipUntil()
            {
                var p = new SimpleParser("abcabc");
                Assert.AreEqual(false, p.SkipUntil('x'));
                Assert.False(p.HasNext);
                p = new SimpleParser("abcabc");
                Assert.AreEqual(true, p.SkipUntil('a'));
                Assert.AreEqual(1, p.Position);
                Assert.AreEqual(true, p.SkipUntil('b'));
                Assert.AreEqual(2, p.Position);
                Assert.AreEqual(true, p.SkipUntil('b'));
                Assert.AreEqual(5, p.Position);
            }


            [Test]
            public void SkipUntil1()
            {
                var parser = new SimpleParser("<asdf/>");
                Assert.IsTrue(parser.SkipUntil("asdf"));
                Assert.AreEqual(5, parser.Position);
                Assert.IsTrue(parser.NextIs('/'));
                parser = new SimpleParser("<aasasdasdf/>");
                Assert.IsTrue(parser.SkipUntil("asdf"));
                Assert.IsTrue(parser.NextIs('/'));
                parser = new SimpleParser("<asdf/>");
                Assert.IsFalse(parser.SkipUntil("xyz"));
                Assert.False(parser.HasNext);
            }
        }
    }
#endif
}
