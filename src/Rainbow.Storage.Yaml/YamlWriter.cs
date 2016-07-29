using System;
using System.IO;
using System.Text;

namespace Rainbow.Storage.Yaml
{
	public class YamlWriter : IDisposable
	{
		private readonly StreamWriter _writer;
		private const int IndentSpaces = 2;
		private const char IndentCharacter = ' ';

		public YamlWriter(Stream stream, int bufferSize, bool leaveStreamOpen)
		{
			_writer = new StreamWriter(stream, Encoding.UTF8, bufferSize, leaveStreamOpen);
			_writer.WriteLine("---");
		}

		public int Indent { get; protected set; }

		public void IncreaseIndent()
		{
			Indent += IndentSpaces;
		}

		public void DecreaseIndent()
		{
			if(Indent < IndentSpaces) throw new ArgumentException("Indent is already at minimum");

			Indent -= IndentSpaces;
		}

		public void WriteBeginListItem(string key, string value)
		{
			if(Indent < IndentSpaces) throw new InvalidOperationException("Indent is at minimum. You must indent to be able to support a list.");

			_writer.Write(new string(IndentCharacter, Indent - 2));
			_writer.Write("- ");
			WriteMapInternal(key, value);
		}

		public virtual void WriteMap(string key)
		{
			_writer.Write(new string(IndentCharacter, Indent));
			_writer.WriteLine(key + ":");
		}

		public virtual void WriteMap(string key, string value)
		{
			_writer.Write(new string(IndentCharacter, Indent));
			WriteMapInternal(key, value);
		}

		public virtual void WriteComment(string value)
		{
			_writer.Write(new string(IndentCharacter, Indent));
			_writer.WriteLine("# " + value);
		}

		protected virtual void WriteMapInternal(string key, string value)
		{
			if (value.IndexOfAny(new [] { '\n', '\r', '"', '\\' }) > -1) // captures any style of endline, cr, lf, or crlf
			{
				_writer.WriteLine("{0}: |{1}{2}", key, Environment.NewLine, IndentMultilineString(Indent + IndentSpaces, value));
				return;
			}

			_writer.WriteLine("{0}: {1}", key, value.IndexOfAny(new [] { '"', ':', '[', ']', '{', '}', '!', '?', '-' }) > -1 ? Encode(value) : value);
		}

		protected virtual string Encode(string value)
		{
			return string.Format("\"{0}\"", value.Replace("\"", @"\"""));
		}

		protected virtual string IndentMultilineString(int indent, string value)
		{
			string indentValue = new string(IndentCharacter, indent);

			var charBuffer = new StringBuilder(indentValue);

			for(int index = 0; index < value.Length; index++)
			{
				char c = value[index];
				switch (c)
				{
					case '\r':
						// ignore \r as that by itself is no newline, UNLESS the following char is not \n
						if (index < value.Length - 1 && value[index + 1] != '\n')
						{
							charBuffer.Append(Environment.NewLine + indentValue);
						}
						break; 
					case '\n': 
						// if we find \n we know it's either \n (Unix-style) or \r\n (Windows-style) so we normalize it to the environment endline
						// and add the appropriate indent to the next line for the multline YAML expression
						charBuffer.Append(Environment.NewLine + indentValue);
						break;
					default: 
						// not any sort of endline char, echo it verbatim
						charBuffer.Append(c);
						break;
				}
			}

			return charBuffer.ToString();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
				_writer.Dispose();
		}
	}
}
