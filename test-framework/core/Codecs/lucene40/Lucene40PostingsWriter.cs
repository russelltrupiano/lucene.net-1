using System.Diagnostics;

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

	/// <summary>
	/// Consumes doc & freq, writing them using the current
	///  index file format 
	/// </summary>


	using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
	using DocsEnum = Lucene.Net.Index.DocsEnum;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
	using DataOutput = Lucene.Net.Store.DataOutput;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Concrete class that writes the 4.0 frq/prx postings format.
	/// </summary>
	/// <seealso cref= Lucene40PostingsFormat
	/// @lucene.experimental  </seealso>
	public sealed class Lucene40PostingsWriter : PostingsWriterBase
	{

	  internal readonly IndexOutput FreqOut;
	  internal readonly IndexOutput ProxOut;
	  internal readonly Lucene40SkipListWriter SkipListWriter;
	  /// <summary>
	  /// Expert: The fraction of TermDocs entries stored in skip tables,
	  /// used to accelerate <seealso cref="DocsEnum#advance(int)"/>.  Larger values result in
	  /// smaller indexes, greater acceleration, but fewer accelerable cases, while
	  /// smaller values result in bigger indexes, less acceleration and more
	  /// accelerable cases. More detailed experiments would be useful here. 
	  /// </summary>
	  internal const int DEFAULT_SKIP_INTERVAL = 16;
	  internal readonly int SkipInterval;

	  /// <summary>
	  /// Expert: minimum docFreq to write any skip data at all
	  /// </summary>
	  internal readonly int SkipMinimum;

	  /// <summary>
	  /// Expert: The maximum number of skip levels. Smaller values result in 
	  /// slightly smaller indexes, but slower skipping in big posting lists.
	  /// </summary>
	  internal readonly int MaxSkipLevels = 10;
	  internal readonly int TotalNumDocs;

	  internal IndexOptions IndexOptions;
	  internal bool StorePayloads;
	  internal bool StoreOffsets;
	  // Starts a new term
	  internal long FreqStart;
	  internal long ProxStart;
	  internal FieldInfo FieldInfo;
	  internal int LastPayloadLength;
	  internal int LastOffsetLength;
	  internal int LastPosition;
	  internal int LastOffset;

	  internal static readonly StandardTermState EmptyState = new StandardTermState();
	  internal StandardTermState LastState;

	  // private String segment;

	  /// <summary>
	  /// Creates a <seealso cref="Lucene40PostingsWriter"/>, with the
	  ///  <seealso cref="#DEFAULT_SKIP_INTERVAL"/>. 
	  /// </summary>
	  public Lucene40PostingsWriter(SegmentWriteState state) : this(state, DEFAULT_SKIP_INTERVAL)
	  {
	  }

	  /// <summary>
	  /// Creates a <seealso cref="Lucene40PostingsWriter"/>, with the
	  ///  specified {@code skipInterval}. 
	  /// </summary>
	  public Lucene40PostingsWriter(SegmentWriteState state, int skipInterval) : base()
	  {
		this.SkipInterval = skipInterval;
		this.SkipMinimum = skipInterval; // set to the same for now
		// this.segment = state.segmentName;
		string fileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, Lucene40PostingsFormat.FREQ_EXTENSION);
		FreqOut = state.directory.createOutput(fileName, state.context);
		bool success = false;
		IndexOutput proxOut = null;
		try
		{
		  CodecUtil.writeHeader(FreqOut, Lucene40PostingsReader.FRQ_CODEC, Lucene40PostingsReader.VERSION_CURRENT);
		  // TODO: this is a best effort, if one of these fields has no postings
		  // then we make an empty prx file, same as if we are wrapped in 
		  // per-field postingsformat. maybe... we shouldn't
		  // bother w/ this opto?  just create empty prx file...?
		  if (state.fieldInfos.hasProx())
		  {
			// At least one field does not omit TF, so create the
			// prox file
			fileName = IndexFileNames.segmentFileName(state.segmentInfo.name, state.segmentSuffix, Lucene40PostingsFormat.PROX_EXTENSION);
			proxOut = state.directory.createOutput(fileName, state.context);
			CodecUtil.writeHeader(proxOut, Lucene40PostingsReader.PRX_CODEC, Lucene40PostingsReader.VERSION_CURRENT);
		  }
		  else
		  {
			// Every field omits TF so we will write no prox file
			proxOut = null;
		  }
		  this.ProxOut = proxOut;
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(FreqOut, proxOut);
		  }
		}

		TotalNumDocs = state.segmentInfo.DocCount;

		SkipListWriter = new Lucene40SkipListWriter(skipInterval, MaxSkipLevels, TotalNumDocs, FreqOut, proxOut);
	  }

	  public override void Init(IndexOutput termsOut)
	  {
		CodecUtil.writeHeader(termsOut, Lucene40PostingsReader.TERMS_CODEC, Lucene40PostingsReader.VERSION_CURRENT);
		termsOut.writeInt(SkipInterval); // write skipInterval
		termsOut.writeInt(MaxSkipLevels); // write maxSkipLevels
		termsOut.writeInt(SkipMinimum); // write skipMinimum
	  }

	  public override BlockTermState NewTermState()
	  {
		return new StandardTermState();
	  }


	  public override void StartTerm()
	  {
		FreqStart = FreqOut.FilePointer;
		//if (DEBUG) System.out.println("SPW: startTerm freqOut.fp=" + freqStart);
		if (ProxOut != null)
		{
		  ProxStart = ProxOut.FilePointer;
		}
		// force first payload to write its length
		LastPayloadLength = -1;
		// force first offset to write its length
		LastOffsetLength = -1;
		SkipListWriter.ResetSkip();
	  }

	  // Currently, this instance is re-used across fields, so
	  // our parent calls setField whenever the field changes
	  public override int SetField(FieldInfo fieldInfo)
	  {
		//System.out.println("SPW: setField");
		/*
		if (BlockTreeTermsWriter.DEBUG && fieldInfo.name.equals("id")) {
		  DEBUG = true;
		} else {
		  DEBUG = false;
		}
		*/
		this.FieldInfo = fieldInfo;
		IndexOptions = fieldInfo.IndexOptions_e;

		StoreOffsets = IndexOptions.compareTo(IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		StorePayloads = fieldInfo.hasPayloads();
		LastState = EmptyState;
		//System.out.println("  set init blockFreqStart=" + freqStart);
		//System.out.println("  set init blockProxStart=" + proxStart);
		return 0;
	  }

	  internal int LastDocID;
	  internal int Df;

	  public override void StartDoc(int docID, int termDocFreq)
	  {
		// if (DEBUG) System.out.println("SPW:   startDoc seg=" + segment + " docID=" + docID + " tf=" + termDocFreq + " freqOut.fp=" + freqOut.getFilePointer());

		int delta = docID - LastDocID;

		if (docID < 0 || (Df > 0 && delta <= 0))
		{
		  throw new CorruptIndexException("docs out of order (" + docID + " <= " + LastDocID + " ) (freqOut: " + FreqOut + ")");
		}

		if ((++Df % SkipInterval) == 0)
		{
		  SkipListWriter.SetSkipData(LastDocID, StorePayloads, LastPayloadLength, StoreOffsets, LastOffsetLength);
		  SkipListWriter.bufferSkip(Df);
		}

		Debug.Assert(docID < TotalNumDocs, "docID=" + docID + " totalNumDocs=" + TotalNumDocs);

		LastDocID = docID;
		if (IndexOptions == IndexOptions_e.DOCS_ONLY)
		{
		  FreqOut.writeVInt(delta);
		}
		else if (1 == termDocFreq)
		{
		  FreqOut.writeVInt((delta << 1) | 1);
		}
		else
		{
		  FreqOut.writeVInt(delta << 1);
		  FreqOut.writeVInt(termDocFreq);
		}

		LastPosition = 0;
		LastOffset = 0;
	  }

	  /// <summary>
	  /// Add a new position & payload </summary>
	  public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
	  {
		//if (DEBUG) System.out.println("SPW:     addPos pos=" + position + " payload=" + (payload == null ? "null" : (payload.length + " bytes")) + " proxFP=" + proxOut.getFilePointer());
		Debug.Assert(IndexOptions.compareTo(IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS) >= 0, "invalid indexOptions: " + IndexOptions);
		Debug.Assert(ProxOut != null);

		int delta = position - LastPosition;

		Debug.Assert(delta >= 0, "position=" + position + " lastPosition=" + LastPosition); // not quite right (if pos=0 is repeated twice we don't catch it)

		LastPosition = position;

		int payloadLength = 0;

		if (StorePayloads)
		{
		  payloadLength = payload == null ? 0 : payload.length;

		  if (payloadLength != LastPayloadLength)
		  {
			LastPayloadLength = payloadLength;
			ProxOut.writeVInt((delta << 1) | 1);
			ProxOut.writeVInt(payloadLength);
		  }
		  else
		  {
			ProxOut.writeVInt(delta << 1);
		  }
		}
		else
		{
		  ProxOut.writeVInt(delta);
		}

		if (StoreOffsets)
		{
		  // don't use startOffset - lastEndOffset, because this creates lots of negative vints for synonyms,
		  // and the numbers aren't that much smaller anyways.
		  int offsetDelta = startOffset - LastOffset;
		  int offsetLength = endOffset - startOffset;
		  Debug.Assert(offsetDelta >= 0 && offsetLength >= 0, "startOffset=" + startOffset + ",lastOffset=" + LastOffset + ",endOffset=" + endOffset);
		  if (offsetLength != LastOffsetLength)
		  {
			ProxOut.writeVInt(offsetDelta << 1 | 1);
			ProxOut.writeVInt(offsetLength);
		  }
		  else
		  {
			ProxOut.writeVInt(offsetDelta << 1);
		  }
		  LastOffset = startOffset;
		  LastOffsetLength = offsetLength;
		}

		if (payloadLength > 0)
		{
		  ProxOut.writeBytes(payload.bytes, payload.offset, payloadLength);
		}
	  }

	  public override void FinishDoc()
	  {
	  }

	  private class StandardTermState : BlockTermState
	  {
		public long FreqStart;
		public long ProxStart;
		public long SkipOffset;
	  }

	  /// <summary>
	  /// Called when we are done adding docs to this term </summary>
	  public override void FinishTerm(BlockTermState _state)
	  {
		StandardTermState state = (StandardTermState)_state;
		// if (DEBUG) System.out.println("SPW: finishTerm seg=" + segment + " freqStart=" + freqStart);
		Debug.Assert(state.docFreq > 0);

		// TODO: wasteful we are counting this (counting # docs
		// for this term) in two places?
		Debug.Assert(state.docFreq == Df);
		state.FreqStart = FreqStart;
		state.ProxStart = ProxStart;
		if (Df >= SkipMinimum)
		{
		  state.SkipOffset = SkipListWriter.writeSkip(FreqOut) - FreqStart;
		}
		else
		{
		  state.SkipOffset = -1;
		}
		LastDocID = 0;
		Df = 0;
	  }

	  public override void EncodeTerm(long[] empty, DataOutput @out, FieldInfo fieldInfo, BlockTermState _state, bool absolute)
	  {
		StandardTermState state = (StandardTermState)_state;
		if (absolute)
		{
		  LastState = EmptyState;
		}
		@out.writeVLong(state.FreqStart - LastState.FreqStart);
		if (state.SkipOffset != -1)
		{
		  Debug.Assert(state.SkipOffset > 0);
		  @out.writeVLong(state.SkipOffset);
		}
		if (IndexOptions.compareTo(IndexOptions_e.DOCS_AND_FREQS_AND_POSITIONS) >= 0)
		{
		  @out.writeVLong(state.ProxStart - LastState.ProxStart);
		}
		LastState = state;
	  }

	  public override void Close()
	  {
		try
		{
		  FreqOut.close();
		}
		finally
		{
		  if (ProxOut != null)
		  {
			ProxOut.close();
		  }
		}
	  }
	}

}