using System.Collections.Generic;

namespace Lucene.Net.Codecs.Lucene46
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
	using ChecksumIndexInput = Lucene.Net.Store.ChecksumIndexInput;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Lucene 4.6 implementation of <seealso cref="SegmentInfoReader"/>.
	/// </summary>
	/// <seealso cref= Lucene46SegmentInfoFormat
	/// @lucene.experimental </seealso>
	public class Lucene46SegmentInfoReader : SegmentInfoReader
	{

	  /// <summary>
	  /// Sole constructor. </summary>
	  public Lucene46SegmentInfoReader()
	  {
	  }

	  public override SegmentInfo Read(Directory dir, string segment, IOContext context)
	  {
		string fileName = IndexFileNames.SegmentFileName(segment, "", Lucene46SegmentInfoFormat.SI_EXTENSION);
		ChecksumIndexInput input = dir.OpenChecksumInput(fileName, context);
		bool success = false;
		try
		{
		  int codecVersion = CodecUtil.CheckHeader(input, Lucene46SegmentInfoFormat.CODEC_NAME, Lucene46SegmentInfoFormat.VERSION_START, Lucene46SegmentInfoFormat.VERSION_CURRENT);
		  string version = input.ReadString();
		  int docCount = input.ReadInt();
		  if (docCount < 0)
		  {
			throw new CorruptIndexException("invalid docCount: " + docCount + " (resource=" + input + ")");
		  }
		  bool isCompoundFile = input.ReadByte() == SegmentInfo.YES;
		  IDictionary<string, string> diagnostics = input.ReadStringStringMap();
		  ISet<string> files = input.ReadStringSet();

		  if (codecVersion >= Lucene46SegmentInfoFormat.VERSION_CHECKSUM)
		  {
			CodecUtil.CheckFooter(input);
		  }
		  else
		  {
			CodecUtil.CheckEOF(input);
		  }

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