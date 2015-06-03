using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sitecore.Diagnostics;

namespace Gibson.SerializationFormatting.Yaml
{
	public class YamlReader : IDisposable
	{
		private readonly StreamReader _reader;
		private const int IndentSpaces = 2;
		private const char IndentCharacter = ' ';
		private readonly PeekableStreamReaderAdapter _readerAdapter;
		private KeyValuePair<string, string>? _peek; 

		public YamlReader(Stream stream, int bufferSize, bool leaveStreamOpen)
		{
			_reader = new StreamReader(stream, Encoding.UTF8, false, bufferSize, leaveStreamOpen);
			_readerAdapter = new PeekableStreamReaderAdapter(_reader);

			Assert.AreEqual("---", _readerAdapter.ReadLine(), "Invalid YAML format, missing expected --- header.");
		}

		public virtual Guid ReadExpectedGuidMap(string expectedKey)
		{
			return GetExpectedGuid(expectedKey, ReadExpectedMap);
		}

		public virtual string ReadExpectedMap(string expectedKey)
		{
			return GetExpectedMap(expectedKey, ReadMap);
		}

		public virtual KeyValuePair<string, string>? PeekMap()
		{
			if (_peek != null) return _peek.Value;

			var map = ReadMapInternal();

			_peek = map;

			return map;
		}

		public virtual KeyValuePair<string, string>? ReadMap()
		{
			if (_peek != null)
			{
				var value = _peek.Value;
				_peek = null;
				return value;
			}

			return ReadMapInternal();
		}

		protected virtual Guid GetExpectedGuid(string expectedKey, Func<string, string> mapFunction)
		{
			var guidString = mapFunction(expectedKey);
			Guid result;

			if (!Guid.TryParse(guidString, out result)) throw new InvalidOperationException(CreateErrorMessage("Map value was not a valid GUID. Got " + guidString));

			return result;
		}

		protected internal string GetExpectedMap(string expectedKey, Func<KeyValuePair<string, string>?> mapFunction)
		{
			var map = mapFunction();

			if (map == null) throw new InvalidOperationException(CreateErrorMessage("Unable to read expected " + expectedKey + " map; found end of file instead."));

			if (!map.Value.Key.Equals(expectedKey, StringComparison.Ordinal)) throw new InvalidOperationException(CreateErrorMessage("Expected map key " + expectedKey + " was not found. Instead got " + map.Value.Key));

			return map.Value.Value;
		}

		protected virtual KeyValuePair<string, string>? ReadMapInternal()
		{
			var initialValue = ReadNextDataLine();

			if (initialValue == null) return null;

			var mapIndex = initialValue.IndexOf(':');

			Assert.IsTrue(mapIndex > 0, CreateErrorMessage("Invalid YAML map value (no : found) " + initialValue));

			var currentIndent = GetIndent(initialValue);

			var mapKey = initialValue.Substring(currentIndent, mapIndex - currentIndent);

			var mapValue = initialValue.Substring(mapIndex + 1).Trim();

			if (mapValue.Equals("|"))
			{
				mapValue = ReadMultilineString(currentIndent + IndentSpaces);
			}
			else
			{
				mapValue = Decode(mapValue);
			}

			return new KeyValuePair<string, string>(mapKey, mapValue);
		}

		protected virtual string Decode(string value)
		{
			if (value.StartsWith("\"") && value.EndsWith("\""))
			{
				return value.Substring(1, value.Length - 2).Replace(@"\""", "\"");
			}

			return value;
		}

		protected virtual string ReadMultilineString(int indent)
		{
			var lines = new List<string>();

			do
			{
				string currentLine = _readerAdapter.PeekLine();

				if (currentLine == null) break;

				var currentIndent = GetIndent(currentLine);

				if (currentIndent >= indent)
				{
					lines.Add(currentLine.Substring(indent));
					_readerAdapter.ReadLine(); // consume the line we peeked
				}
				else break;

			} while (true);

			return string.Join(Environment.NewLine, lines);
		}

		protected int GetIndent(string line)
		{
			for (int i = 0; i < line.Length; i++)
			{
				// '-' = a list. for our DSL YAML that's for your visual reference only.
				if (line[i] != IndentCharacter && line[i] != '-') return i;
			}

			return line.Length;
		}

		protected string ReadNextDataLine()
		{
			do
			{
				string currentLine = _readerAdapter.ReadLine();

				if (currentLine == null) return null;

				// blank line
				if (string.IsNullOrWhiteSpace(currentLine)) continue;

				var indent = GetIndent(currentLine);

				// comment line
				if (currentLine.Length > indent && currentLine[indent] == '#') continue;

				return currentLine;
			} while (true);
		}

		protected string CreateErrorMessage(string message)
		{
			return "Line " + _readerAdapter.CurrentLine + ": " + message;
		}

		public void Dispose()
		{
			_reader.Dispose();
		}

		protected class PeekableStreamReaderAdapter
		{
			private readonly StreamReader _underlying;
			private string _peekedLine;
			private int _currentLine;

			public PeekableStreamReaderAdapter(StreamReader underlying)
			{
				_underlying = underlying;
			}

			public string PeekLine()
			{
				if(_peekedLine != null) return _peekedLine;

				string line = _underlying.ReadLine();

				if (line == null)
					return null;

				_peekedLine = line;

				return line;
			}

			public int CurrentLine { get { return _currentLine; } }

			public string ReadLine()
			{
				_currentLine++;

				if (_peekedLine != null)
				{
					var value = _peekedLine;
					_peekedLine = null;
					return value;
				}

				return _underlying.ReadLine();
			}
		}
	}
}
