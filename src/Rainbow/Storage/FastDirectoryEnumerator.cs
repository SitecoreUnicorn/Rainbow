using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

// http://www.codeproject.com/Articles/38959/A-Faster-Directory-Enumerator
// Reduced directory iteration time by nearly 3x in Rainbow testing over Directory.GetFiles()
namespace Rainbow.Storage
{
	/// <summary>
	///     Contains information about a file returned by the
	///     <see cref="FastDirectoryEnumerator" /> class.
	/// </summary>
	[Serializable]
	public class FileData
	{
		/// <summary>
		///     Attributes of the file.
		/// </summary>
		public readonly FileAttributes Attributes;

		/// <summary>
		///     File creation time in UTC
		/// </summary>
		public readonly DateTime CreationTimeUtc;

		/// <summary>
		///     File last access time in UTC
		/// </summary>
		public readonly DateTime LastAccessTimeUtc;

		/// <summary>
		///     File last write time in UTC
		/// </summary>
		public readonly DateTime LastWriteTimeUtc;

		/// <summary>
		///     Name of the file
		/// </summary>
		public readonly string Name;

		/// <summary>
		///     Full path to the file.
		/// </summary>
		public readonly string Path;

		/// <summary>
		///     Size of the file in bytes
		/// </summary>
		public readonly long Size;

		/// <summary>
		///     Initializes a new instance of the <see cref="FileData" /> class.
		/// </summary>
		/// <param name="dir">The directory that the file is stored at</param>
		/// <param name="findData">
		///     WIN32_FIND_DATA structure that this
		///     object wraps.
		/// </param>
		internal FileData(string dir, WIN32_FIND_DATA findData)
		{
			Attributes = findData.dwFileAttributes;


			CreationTimeUtc = ConvertDateTime(findData.ftCreationTime_dwHighDateTime,
				findData.ftCreationTime_dwLowDateTime);

			LastAccessTimeUtc = ConvertDateTime(findData.ftLastAccessTime_dwHighDateTime,
				findData.ftLastAccessTime_dwLowDateTime);

			LastWriteTimeUtc = ConvertDateTime(findData.ftLastWriteTime_dwHighDateTime,
				findData.ftLastWriteTime_dwLowDateTime);

			Size = CombineHighLowInts(findData.nFileSizeHigh, findData.nFileSizeLow);

			Name = findData.cFileName;
			Path = System.IO.Path.Combine(dir, findData.cFileName);
		}

		public DateTime CreationTime
		{
			get { return CreationTimeUtc.ToLocalTime(); }
		}

		/// <summary>
		///     Gets the last access time in local time.
		/// </summary>
		public DateTime LastAccesTime
		{
			get { return LastAccessTimeUtc.ToLocalTime(); }
		}

		/// <summary>
		///     Gets the last access time in local time.
		/// </summary>
		public DateTime LastWriteTime
		{
			get { return LastWriteTimeUtc.ToLocalTime(); }
		}

		/// <summary>
		///     Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </returns>
		public override string ToString()
		{
			return Name;
		}

		private static long CombineHighLowInts(uint high, uint low)
		{
			return (((long) high) << 0x20) | low;
		}

		private static DateTime ConvertDateTime(uint high, uint low)
		{
			var fileTime = CombineHighLowInts(high, low);
			return DateTime.FromFileTimeUtc(fileTime);
		}
	}

	/// <summary>
	///     Contains information about the file that is found
	///     by the FindFirstFile or FindNextFile functions.
	/// </summary>
	[Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto), BestFitMapping(false)]
	internal class WIN32_FIND_DATA
	{
		public FileAttributes dwFileAttributes;
		public uint ftCreationTime_dwLowDateTime;
		public uint ftCreationTime_dwHighDateTime;
		public uint ftLastAccessTime_dwLowDateTime;
		public uint ftLastAccessTime_dwHighDateTime;
		public uint ftLastWriteTime_dwLowDateTime;
		public uint ftLastWriteTime_dwHighDateTime;
		public uint nFileSizeHigh;
		public uint nFileSizeLow;
		public int dwReserved0;
		public int dwReserved1;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;

		/// <summary>
		///     Returns a <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </summary>
		/// <returns>
		///     A <see cref="T:System.String" /> that represents the current <see cref="T:System.Object" />.
		/// </returns>
		public override string ToString()
		{
			return "File name=" + cFileName;
		}
	}

	/// <summary>
	///     A fast enumerator of files in a directory.  Use this if you need to get attributes for
	///     all files in a directory.
	/// </summary>
	/// <remarks>
	///     This enumerator is substantially faster than using <see cref="Directory.GetFiles(string)" />
	///     and then creating a new FileInfo object for each path.  Use this version when you
	///     will need to look at the attibutes of each file returned (for example, you need
	///     to check each file in a directory to see if it was modified after a specific date).
	/// </remarks>
	public static class FastDirectoryEnumerator
	{
		/// <summary>
		///     Gets <see cref="FileData" /> for all the files in a directory.
		/// </summary>
		/// <param name="path">The path to search.</param>
		/// <returns>
		///     An object that implements <see cref="IEnumerable{FileData}" /> and
		///     allows you to enumerate the files in the given directory.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path" /> is a null reference (Nothing in VB)
		/// </exception>
		public static IEnumerable<FileData> EnumerateFiles(string path)
		{
			return EnumerateFiles(path, "*");
		}

		/// <summary>
		///     Gets <see cref="FileData" /> for all the files in a directory that match a
		///     specific filter.
		/// </summary>
		/// <param name="path">The path to search.</param>
		/// <param name="searchPattern">The search string to match against files in the path.</param>
		/// <returns>
		///     An object that implements <see cref="IEnumerable{FileData}" /> and
		///     allows you to enumerate the files in the given directory.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path" /> is a null reference (Nothing in VB)
		/// </exception>
		public static IEnumerable<FileData> EnumerateFiles(string path, string searchPattern)
		{
			return EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
		}

		/// <summary>
		///     Gets <see cref="FileData" /> for all the files in a directory that
		///     match a specific filter, optionally including all sub directories.
		/// </summary>
		/// <param name="path">The path to search.</param>
		/// <param name="searchPattern">The search string to match against files in the path.</param>
		/// <param name="searchOption">
		///     One of the SearchOption values that specifies whether the search
		///     operation should include all subdirectories or only the current directory.
		/// </param>
		/// <returns>
		///     An object that implements <see cref="IEnumerable{FileData}" /> and
		///     allows you to enumerate the files in the given directory.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path" /> is a null reference (Nothing in VB)
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     <paramref name="searchOption" /> is not one of the valid values of the
		///     <see cref="System.IO.SearchOption" /> enumeration.
		/// </exception>
		public static IEnumerable<FileData> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
		{
			if (path == null)
			{
				throw new ArgumentNullException("path");
			}
			if (searchPattern == null)
			{
				throw new ArgumentNullException("searchPattern");
			}
			if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
			{
				throw new ArgumentOutOfRangeException("searchOption");
			}

			var fullPath = Path.GetFullPath(path);

			return new FileEnumerable(fullPath, searchPattern, searchOption);
		}

		/// <summary>
		///     Gets <see cref="FileData" /> for all the files in a directory that match a
		///     specific filter.
		/// </summary>
		/// <param name="path">The path to search.</param>
		/// <param name="searchPattern">The search string to match against files in the path.</param>
		/// <param name="searchOption"></param>
		/// <returns>
		///     An object that implements <see cref="IEnumerable{FileData}" /> and
		///     allows you to enumerate the files in the given directory.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		///     <paramref name="path" /> is a null reference (Nothing in VB)
		/// </exception>
		public static FileData[] GetFiles(string path, string searchPattern, SearchOption searchOption)
		{
			var e = EnumerateFiles(path, searchPattern, searchOption);
			var list = new List<FileData>(e);

			var retval = new FileData[list.Count];
			list.CopyTo(retval);

			return retval;
		}

		/// <summary>
		///     Provides the implementation of the
		///     <see cref="T:System.Collections.Generic.IEnumerable`1" /> interface
		/// </summary>
		private class FileEnumerable : IEnumerable<FileData>
		{
			private readonly string _mFilter;
			private readonly string _mPath;
			private readonly SearchOption _mSearchOption;

			/// <summary>
			///     Initializes a new instance of the <see cref="FileEnumerable" /> class.
			/// </summary>
			/// <param name="path">The path to search.</param>
			/// <param name="filter">The search string to match against files in the path.</param>
			/// <param name="searchOption">
			///     One of the SearchOption values that specifies whether the search
			///     operation should include all subdirectories or only the current directory.
			/// </param>
			public FileEnumerable(string path, string filter, SearchOption searchOption)
			{
				_mPath = path;
				_mFilter = filter;
				_mSearchOption = searchOption;
			}

			#region IEnumerable<FileData> Members

			/// <summary>
			///     Returns an enumerator that iterates through the collection.
			/// </summary>
			/// <returns>
			///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can
			///     be used to iterate through the collection.
			/// </returns>
			public IEnumerator<FileData> GetEnumerator()
			{
				return new FileEnumerator(_mPath, _mFilter, _mSearchOption);
			}

			#endregion

			#region IEnumerable Members

			/// <summary>
			///     Returns an enumerator that iterates through a collection.
			/// </summary>
			/// <returns>
			///     An <see cref="T:System.Collections.IEnumerator" /> object that can be
			///     used to iterate through the collection.
			/// </returns>
			IEnumerator IEnumerable.GetEnumerator()
			{
				return new FileEnumerator(_mPath, _mFilter, _mSearchOption);
			}

			#endregion
		}

		/// <summary>
		///     Wraps a FindFirstFile handle.
		/// </summary>
		private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
		{
			/// <summary>
			///     Initializes a new instance of the <see cref="SafeFindHandle" /> class.
			/// </summary>
			[SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
			internal SafeFindHandle()
				: base(true)
			{
			}

			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
			[DllImport("kernel32.dll")]
			private static extern bool FindClose(IntPtr handle);

			/// <summary>
			///     When overridden in a derived class, executes the code required to free the handle.
			/// </summary>
			/// <returns>
			///     true if the handle is released successfully; otherwise, in the
			///     event of a catastrophic failure, false. In this case, it
			///     generates a releaseHandleFailed MDA Managed Debugging Assistant.
			/// </returns>
			protected override bool ReleaseHandle()
			{
				return FindClose(handle);
			}
		}

		/// <summary>
		///     Provides the implementation of the
		///     <see cref="T:System.Collections.Generic.IEnumerator`1" /> interface
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		private class FileEnumerator : IEnumerator<FileData>
		{
			private readonly Stack<SearchContext> _mContextStack;
			private readonly string _mFilter;
			private readonly SearchOption _mSearchOption;
			private readonly WIN32_FIND_DATA _mWinFindData = new WIN32_FIND_DATA();
			private SearchContext _mCurrentContext;
			private SafeFindHandle _mHndFindFile;
			private string _mPath;

			/// <summary>
			///     Initializes a new instance of the <see cref="FileEnumerator" /> class.
			/// </summary>
			/// <param name="path">The path to search.</param>
			/// <param name="filter">The search string to match against files in the path.</param>
			/// <param name="searchOption">
			///     One of the SearchOption values that specifies whether the search
			///     operation should include all subdirectories or only the current directory.
			/// </param>
			public FileEnumerator(string path, string filter, SearchOption searchOption)
			{
				_mPath = path;
				_mFilter = filter;
				_mSearchOption = searchOption;
				_mCurrentContext = new SearchContext(path);

				if (_mSearchOption == SearchOption.AllDirectories)
				{
					_mContextStack = new Stack<SearchContext>();
				}
			}

			#region IEnumerator<FileData> Members

			/// <summary>
			///     Gets the element in the collection at the current position of the enumerator.
			/// </summary>
			/// <value></value>
			/// <returns>
			///     The element in the collection at the current position of the enumerator.
			/// </returns>
			public FileData Current
			{
				get { return new FileData(_mPath, _mWinFindData); }
			}

			#endregion

			#region IDisposable Members

			/// <summary>
			///     Performs application-defined tasks associated with freeing, releasing,
			///     or resetting unmanaged resources.
			/// </summary>
			public void Dispose()
			{
				if (_mHndFindFile != null)
				{
					_mHndFindFile.Dispose();
				}
			}

			#endregion

			[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			private static extern SafeFindHandle FindFirstFile(string fileName,
				[In, Out] WIN32_FIND_DATA data);

			[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			private static extern bool FindNextFile(SafeFindHandle hndFindFile,
				[In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_DATA lpFindFileData);

			/// <summary>
			///     Hold context information about where we current are in the directory search.
			/// </summary>
			private class SearchContext
			{
				public readonly string Path;
				public Stack<string> SubdirectoriesToProcess;

				public SearchContext(string path)
				{
					Path = path;
				}
			}

			#region IEnumerator Members

			/// <summary>
			///     Gets the element in the collection at the current position of the enumerator.
			/// </summary>
			/// <value></value>
			/// <returns>
			///     The element in the collection at the current position of the enumerator.
			/// </returns>
			object IEnumerator.Current
			{
				get { return new FileData(_mPath, _mWinFindData); }
			}

			/// <summary>
			///     Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			///     true if the enumerator was successfully advanced to the next element;
			///     false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">
			///     The collection was modified after the enumerator was created.
			/// </exception>
			public bool MoveNext()
			{
				var retval = false;

				//If the handle is null, this is first call to MoveNext in the current 
				// directory.  In that case, start a new search.
				if (_mCurrentContext.SubdirectoriesToProcess == null)
				{
					if (_mHndFindFile == null)
					{
						new FileIOPermission(FileIOPermissionAccess.PathDiscovery, _mPath).Demand();

						var searchPath = Path.Combine(_mPath, _mFilter);
						_mHndFindFile = FindFirstFile(searchPath, _mWinFindData);
						retval = !_mHndFindFile.IsInvalid;
					}
					else
					{
						//Otherwise, find the next item.
						retval = FindNextFile(_mHndFindFile, _mWinFindData);
					}
				}

				//If the call to FindNextFile or FindFirstFile succeeded...
				if (retval)
				{
					if ((_mWinFindData.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
					{
						//Ignore folders for now.   We call MoveNext recursively here to 
						// move to the next item that FindNextFile will return.
						return MoveNext();
					}
				}
				else if (_mSearchOption == SearchOption.AllDirectories)
				{
					//SearchContext context = new SearchContext(m_hndFindFile, m_path);
					//m_contextStack.Push(context);
					//m_path = Path.Combine(m_path, m_win_find_data.cFileName);
					//m_hndFindFile = null;

					if (_mCurrentContext.SubdirectoriesToProcess == null)
					{
						var subDirectories = Directory.GetDirectories(_mPath);
						_mCurrentContext.SubdirectoriesToProcess = new Stack<string>(subDirectories);
					}

					if (_mCurrentContext.SubdirectoriesToProcess.Count > 0)
					{
						var subDir = _mCurrentContext.SubdirectoriesToProcess.Pop();

						_mContextStack.Push(_mCurrentContext);
						_mPath = subDir;
						_mHndFindFile = null;
						_mCurrentContext = new SearchContext(_mPath);
						return MoveNext();
					}

					//If there are no more files in this directory and we are 
					// in a sub directory, pop back up to the parent directory and
					// continue the search from there.
					if (_mContextStack.Count > 0)
					{
						_mCurrentContext = _mContextStack.Pop();
						_mPath = _mCurrentContext.Path;
						if (_mHndFindFile != null)
						{
							_mHndFindFile.Close();
							_mHndFindFile = null;
						}

						return MoveNext();
					}
				}

				return retval;
			}

			/// <summary>
			///     Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// <exception cref="T:System.InvalidOperationException">
			///     The collection was modified after the enumerator was created.
			/// </exception>
			public void Reset()
			{
				_mHndFindFile = null;
			}

			#endregion
		}
	}
}