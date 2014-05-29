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


	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Store = Lucene.Net.Document.Field.Store;
	using TextField = Lucene.Net.Document.TextField;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using ChildScorer = Lucene.Net.Search.Scorer.ChildScorer;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	// TODO: refactor to a base class, that collects freqs from the scorer tree
	// and test all queries with it
	public class TestBooleanQueryVisitSubscorers : LuceneTestCase
	{
	  internal Analyzer Analyzer;
	  internal IndexReader Reader;
	  internal IndexSearcher Searcher;
	  internal Directory Dir;

	  internal const string F1 = "title";
	  internal const string F2 = "body";

	  public override void SetUp()
	  {
		base.setUp();
		Analyzer = new MockAnalyzer(random());
		Dir = newDirectory();
		IndexWriterConfig config = newIndexWriterConfig(TEST_VERSION_CURRENT, Analyzer);
		config.MergePolicy = newLogMergePolicy(); // we will use docids to validate
		RandomIndexWriter writer = new RandomIndexWriter(random(), Dir, config);
		writer.addDocument(Doc("lucene", "lucene is a very popular search engine library"));
		writer.addDocument(Doc("solr", "solr is a very popular search server and is using lucene"));
		writer.addDocument(Doc("nutch", "nutch is an internet search engine with web crawler and is using lucene and hadoop"));
		Reader = writer.Reader;
		writer.close();
		Searcher = newSearcher(Reader);
	  }

	  public override void TearDown()
	  {
		Reader.close();
		Dir.close();
		base.tearDown();
	  }

	  public virtual void TestDisjunctions()
	  {
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term(F1, "lucene")), BooleanClause.Occur_e.SHOULD);
		bq.add(new TermQuery(new Term(F2, "lucene")), BooleanClause.Occur_e.SHOULD);
		bq.add(new TermQuery(new Term(F2, "search")), BooleanClause.Occur_e.SHOULD);
		IDictionary<int?, int?> tfs = GetDocCounts(Searcher, bq);
		Assert.AreEqual(3, tfs.Count); // 3 documents
		Assert.AreEqual(3, (int)tfs[0]); // f1:lucene + f2:lucene + f2:search
		Assert.AreEqual(2, (int)tfs[1]); // f2:search + f2:lucene
		Assert.AreEqual(2, (int)tfs[2]); // f2:search + f2:lucene
	  }

	  public virtual void TestNestedDisjunctions()
	  {
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term(F1, "lucene")), BooleanClause.Occur_e.SHOULD);
		BooleanQuery bq2 = new BooleanQuery();
		bq2.add(new TermQuery(new Term(F2, "lucene")), BooleanClause.Occur_e.SHOULD);
		bq2.add(new TermQuery(new Term(F2, "search")), BooleanClause.Occur_e.SHOULD);
		bq.add(bq2, BooleanClause.Occur_e.SHOULD);
		IDictionary<int?, int?> tfs = GetDocCounts(Searcher, bq);
		Assert.AreEqual(3, tfs.Count); // 3 documents
		Assert.AreEqual(3, (int)tfs[0]); // f1:lucene + f2:lucene + f2:search
		Assert.AreEqual(2, (int)tfs[1]); // f2:search + f2:lucene
		Assert.AreEqual(2, (int)tfs[2]); // f2:search + f2:lucene
	  }

	  public virtual void TestConjunctions()
	  {
		BooleanQuery bq = new BooleanQuery();
		bq.add(new TermQuery(new Term(F2, "lucene")), BooleanClause.Occur_e.MUST);
		bq.add(new TermQuery(new Term(F2, "is")), BooleanClause.Occur_e.MUST);
		IDictionary<int?, int?> tfs = GetDocCounts(Searcher, bq);
		Assert.AreEqual(3, tfs.Count); // 3 documents
		Assert.AreEqual(2, (int)tfs[0]); // f2:lucene + f2:is
		Assert.AreEqual(3, (int)tfs[1]); // f2:is + f2:is + f2:lucene
		Assert.AreEqual(3, (int)tfs[2]); // f2:is + f2:is + f2:lucene
	  }

	  internal static Document Doc(string v1, string v2)
	  {
		Document doc = new Document();
		doc.add(new TextField(F1, v1, Store.YES));
		doc.add(new TextField(F2, v2, Store.YES));
		return doc;
	  }

	  internal static IDictionary<int?, int?> GetDocCounts(IndexSearcher searcher, Query query)
	  {
		MyCollector collector = new MyCollector();
		searcher.search(query, collector);
		return collector.DocCounts;
	  }

	  internal class MyCollector : Collector
	  {

		internal TopDocsCollector<ScoreDoc> Collector;
		internal int DocBase;

		public readonly IDictionary<int?, int?> DocCounts = new Dictionary<int?, int?>();
		internal readonly Set<Scorer> TqsSet = new HashSet<Scorer>();

		internal MyCollector()
		{
		  Collector = TopScoreDocCollector.create(10, true);
		}

		public override bool AcceptsDocsOutOfOrder()
		{
		  return false;
		}

		public override void Collect(int doc)
		{
		  int freq = 0;
		  foreach (Scorer scorer in TqsSet)
		  {
			if (doc == scorer.docID())
			{
			  freq += scorer.freq();
			}
		  }
		  DocCounts[doc + DocBase] = freq;
		  Collector.collect(doc);
		}

		public override AtomicReaderContext NextReader
		{
			set
			{
			  this.DocBase = value.docBase;
			  Collector.NextReader = value;
			}
		}

		public override Scorer Scorer
		{
			set
			{
			  Collector.Scorer = value;
			  TqsSet.clear();
			  FillLeaves(value, TqsSet);
			}
		}

		internal virtual void FillLeaves(Scorer scorer, Set<Scorer> set)
		{
		  if (scorer.Weight.Query is TermQuery)
		  {
			set.add(scorer);
		  }
		  else
		  {
			foreach (ChildScorer child in scorer.Children)
			{
			  FillLeaves(child.child, set);
			}
		  }
		}

		public virtual TopDocs TopDocs()
		{
		  return Collector.topDocs();
		}

		public virtual int Freq(int doc)
		{
		  return DocCounts[doc];
		}
	  }
	}

}