using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene40
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */


	using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentInfo = Lucene.Net.Index.SegmentInfo;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexInput = Lucene.Net.Store.IndexInput;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Lucene 4.0 implementation of <seealso cref="SegmentInfoReader"/>.
	/// </summary>
	/// <seealso cref= Lucene40SegmentInfoFormat
	/// @lucene.experimental </seealso>
	/// @deprecated Only for reading old 4.0-4.5 segments 
	[Obsolete("Only for reading old 4.0-4.5 segments")]
	public class Lucene40SegmentInfoReader : SegmentInfoReader
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene40SegmentInfoReader()
	  {
	  }

	  public override SegmentInfo Read(Directory dir, string segment, IOContext context)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String fileName = Lucene.Net.Index.IndexFileNames.segmentFileName(segment, "", Lucene40SegmentInfoFormat.SI_EXTENSION);
		string fileName = IndexFileNames.SegmentFileName(segment, "", Lucene40SegmentInfoFormat.SI_EXTENSION);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Store.IndexInput input = dir.openInput(fileName, context);
		IndexInput input = dir.OpenInput(fileName, context);
		bool success = false;
		try
		{
		  CodecUtil.CheckHeader(input, Lucene40SegmentInfoFormat.CODEC_NAME, Lucene40SegmentInfoFormat.VERSION_START, Lucene40SegmentInfoFormat.VERSION_CURRENT);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final String version = input.readString();
		  string version = input.ReadString();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int docCount = input.readInt();
		  int docCount = input.ReadInt();
		  if (docCount < 0)
		  {
			throw new CorruptIndexException("invalid docCount: " + docCount + " (resource=" + input + ")");
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean isCompoundFile = input.readByte() == Lucene.Net.Index.SegmentInfo.YES;
		  bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<String,String> diagnostics = input.readStringStringMap();
		  IDictionary<string, string> diagnostics = input.ReadStringStringMap();
		  input.ReadStringStringMap(); // read deprecated attributes
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<String> files = input.readStringSet();
		  ISet<string> files = input.ReadStringSet();

		  CodecUtil.CheckEOF(input);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.SegmentInfo si = new Lucene.Net.Index.SegmentInfo(dir, version, segment, docCount, isCompoundFile, null, diagnostics);
		  SegmentInfo si = new SegmentInfo(dir, version, segment, docCount, isCompoundFile, null, diagnostics);
		  si.Files = files;

		  success = true;

		  return si;

		}
		finally
		{
		  if (!success)
		  {
			IOUtils.CloseWhileHandlingException(input);
		  }
		  else
		  {
			input.Close();
		  }
		}
	  }
	}

}