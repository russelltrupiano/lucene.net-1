using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Lucene.Net.Search
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


	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using Fields = Lucene.Net.Index.Fields;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using Term = Lucene.Net.Index.Term;
	using TermContext = Lucene.Net.Index.TermContext;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using AttributeSource = Lucene.Net.Util.AttributeSource;
	using BytesRef = Lucene.Net.Util.BytesRef;

	internal abstract class TermCollectingRewrite<Q> : MultiTermQuery.RewriteMethod where Q : Query
	{


	  /// <summary>
	  /// Return a suitable top-level Query for holding all expanded terms. </summary>
	  protected internal abstract Q TopLevelQuery {get;}

	  /// <summary>
	  /// Add a MultiTermQuery term to the top-level query </summary>
	  protected internal void AddClause(Q topLevel, Term term, int docCount, float boost)
	  {
		AddClause(topLevel, term, docCount, boost, null);
	  }

	  protected internal abstract void AddClause(Q topLevel, Term term, int docCount, float boost, TermContext states);


	  internal void CollectTerms(IndexReader reader, MultiTermQuery query, TermCollector collector)
	  {
		IndexReaderContext topReaderContext = reader.Context;
		IComparer<BytesRef> lastTermComp = null;
		foreach (AtomicReaderContext context in topReaderContext.Leaves())
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Fields fields = context.reader().fields();
		  Fields fields = context.Reader().fields();
		  if (fields == null)
		  {
			// reader has no fields
			continue;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Terms terms = fields.terms(query.field);
		  Terms terms = fields.Terms(query.Field_Renamed);
		  if (terms == null)
		  {
			// field does not exist
			continue;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermsEnum termsEnum = getTermsEnum(query, terms, collector.attributes);
		  TermsEnum termsEnum = getTermsEnum(query, terms, collector.Attributes);
		  Debug.Assert(termsEnum != null);

		  if (termsEnum == TermsEnum.EMPTY)
		  {
			continue;
		  }

		  // Check comparator compatibility:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Comparator<Lucene.Net.Util.BytesRef> newTermComp = termsEnum.getComparator();
		  IComparer<BytesRef> newTermComp = termsEnum.Comparator;
		  if (lastTermComp != null && newTermComp != null && newTermComp != lastTermComp)
		  {
			throw new Exception("term comparator should not change between segments: " + lastTermComp + " != " + newTermComp);
		  }
		  lastTermComp = newTermComp;
		  collector.SetReaderContext(topReaderContext, context);
		  collector.NextEnum = termsEnum;
		  BytesRef bytes;
		  while ((bytes = termsEnum.Next()) != null)
		  {
			if (!collector.Collect(bytes))
			{
			  return; // interrupt whole term collection, so also don't iterate other subReaders
			}
		  }
		}
	  }

	  internal abstract class TermCollector
	  {

		protected internal AtomicReaderContext ReaderContext;
		protected internal IndexReaderContext TopReaderContext;

		public virtual void SetReaderContext(IndexReaderContext topReaderContext, AtomicReaderContext readerContext)
		{
		  this.ReaderContext = readerContext;
		  this.TopReaderContext = topReaderContext;
		}
		/// <summary>
		/// attributes used for communication with the enum </summary>
		public readonly AttributeSource Attributes = new AttributeSource();

		/// <summary>
		/// return false to stop collecting </summary>
		public abstract bool Collect(BytesRef bytes);

		/// <summary>
		/// the next segment's <seealso cref="TermsEnum"/> that is used to collect terms </summary>
		public abstract TermsEnum NextEnum {set;}
	  }
	}

}