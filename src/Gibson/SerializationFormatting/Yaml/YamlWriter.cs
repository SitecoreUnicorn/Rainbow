using System;
using System.IO;
using System.Text;

namespace Gibson.SerializationFormatting.Yaml
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
			if (value.IndexOf(Environment.NewLine, StringComparison.Ordinal) > 0)
			{
				_writer.WriteLine("{0}: |{1}{2}", key, Environment.NewLine, IndentMultilineString(Indent + IndentSpaces, value));
				return;
			}

			_writer.WriteLine("{0}: {1}", key, value.IndexOf('"') > 0 ? Encode(value) : value);
		}

		protected virtual string Encode(string value)
		{
			return string.Format("\"{0}\"", value.Replace("\"", @"\"""));
		}

		protected virtual string IndentMultilineString(int indent, string value)
		{
			string indentValue = new string(IndentCharacter, indent);
			return indentValue + value.Replace(Environment.NewLine, Environment.NewLine + indentValue);
		}

		public void Dispose()
		{
			_writer.Dispose();
		}
	}
}
