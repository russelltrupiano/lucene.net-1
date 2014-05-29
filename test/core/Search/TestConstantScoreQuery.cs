using System.Diagnostics;

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

	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

	/// <summary>
	/// this class only tests some basic functionality in CSQ, the main parts are mostly
	/// tested by MultiTermQuery tests, explanations seems to be tested in TestExplanations! 
	/// </summary>
	public class TestConstantScoreQuery : LuceneTestCase
	{

	  public virtual void TestCSQ()
	  {
		Query q1 = new ConstantScoreQuery(new TermQuery(new Term("a", "b")));
		Query q2 = new ConstantScoreQuery(new TermQuery(new Term("a", "c")));
		Query q3 = new ConstantScoreQuery(TermRangeFilter.newStringRange("a", "b", "c", true, true));
		QueryUtils.check(q1);
		QueryUtils.check(q2);
		QueryUtils.checkEqual(q1,q1);
		QueryUtils.checkEqual(q2,q2);
		QueryUtils.checkEqual(q3,q3);
		QueryUtils.checkUnequal(q1,q2);
		QueryUtils.checkUnequal(q2,q3);
		QueryUtils.checkUnequal(q1,q3);
		QueryUtils.checkUnequal(q1, new TermQuery(new Term("a", "b")));
	  }

	  private void CheckHits(IndexSearcher searcher, Query q, float expectedScore, string scorerClassName, string innerScorerClassName)
	  {
		int[] count = new int[1];
		searcher.search(q, new CollectorAnonymousInnerClassHelper(this, expectedScore, scorerClassName, innerScorerClassName, count));
		Assert.AreEqual("invalid number of results", 1, count[0]);
	  }

	  private class CollectorAnonymousInnerClassHelper : Collector
	  {
		  private readonly TestConstantScoreQuery OuterInstance;

		  private float ExpectedScore;
		  private string ScorerClassName;
		  private string InnerScorerClassName;
		  private int[] Count;

		  public CollectorAnonymousInnerClassHelper(TestConstantScoreQuery outerInstance, float expectedScore, string scorerClassName, string innerScorerClassName, int[] count)
		  {
			  this.OuterInstance = outerInstance;
			  this.ExpectedScore = expectedScore;
			  this.ScorerClassName = scorerClassName;
			  this.InnerScorerClassName = innerScorerClassName;
			  this.Count = count;
		  }

		  private Scorer scorer;

		  public override Scorer Scorer
		  {
			  set
			  {
				this.scorer = value;
				Assert.AreEqual("Scorer is implemented by wrong class", ScorerClassName, value.GetType().Name);
				if (InnerScorerClassName != null && value is ConstantScoreQuery.ConstantScorer)
				{
				  ConstantScoreQuery.ConstantScorer innerScorer = (ConstantScoreQuery.ConstantScorer) value;
				  Assert.AreEqual("inner Scorer is implemented by wrong class", InnerScorerClassName, innerScorer.docIdSetIterator.GetType().Name);
				}
			  }
		  }

		  public override void Collect(int doc)
		  {
			Assert.AreEqual("Score differs from expected", ExpectedScore, this.scorer.score(), 0);
			Count[0]++;
		  }

		  public override AtomicReaderContext NextReader
		  {
			  set
			  {
			  }
		  }

		  public override bool AcceptsDocsOutOfOrder()
		  {
			return true;
		  }
	  }

	  public virtual void TestWrapped2Times()
	  {
		Directory directory = null;
		IndexReader reader = null;
		IndexSearcher searcher = null;
		try
		{
		  directory = newDirectory();
		  RandomIndexWriter writer = new RandomIndexWriter(random(), directory);

		  Document doc = new Document();
		  doc.add(newStringField("field", "term", Field.Store.NO));
		  writer.addDocument(doc);

		  reader = writer.Reader;
		  writer.close();
		  // we don't wrap with AssertingIndexSearcher in order to have the original scorer in setScorer.
		  searcher = newSearcher(reader, true, false);

		  // set a similarity that does not normalize our boost away
		  searcher.Similarity = new DefaultSimilarityAnonymousInnerClassHelper(this);

		  Query csq1 = new ConstantScoreQuery(new TermQuery(new Term("field", "term")));
		  csq1.Boost = 2.0f;
		  Query csq2 = new ConstantScoreQuery(csq1);
		  csq2.Boost = 5.0f;

		  BooleanQuery bq = new BooleanQuery();
		  bq.add(csq1, BooleanClause.Occur_e.SHOULD);
		  bq.add(csq2, BooleanClause.Occur_e.SHOULD);

		  Query csqbq = new ConstantScoreQuery(bq);
		  csqbq.Boost = 17.0f;

		  CheckHits(searcher, csq1, csq1.Boost, typeof(ConstantScoreQuery.ConstantScorer).Name, null);
		  CheckHits(searcher, csq2, csq2.Boost, typeof(ConstantScoreQuery.ConstantScorer).Name, typeof(ConstantScoreQuery.ConstantScorer).Name);

		  // for the combined BQ, the scorer should always be BooleanScorer's BucketScorer, because our scorer supports out-of order collection!
		  string bucketScorerClass = typeof(FakeScorer).Name;
		  CheckHits(searcher, bq, csq1.Boost + csq2.Boost, bucketScorerClass, null);
		  CheckHits(searcher, csqbq, csqbq.Boost, typeof(ConstantScoreQuery.ConstantScorer).Name, bucketScorerClass);
		}
		finally
		{
		  if (reader != null)
		  {
			  reader.close();
		  }
		  if (directory != null)
		  {
			  directory.close();
		  }
		}
	  }

	  private class DefaultSimilarityAnonymousInnerClassHelper : DefaultSimilarity
	  {
		  private readonly TestConstantScoreQuery OuterInstance;

		  public DefaultSimilarityAnonymousInnerClassHelper(TestConstantScoreQuery outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  public override float QueryNorm(float sumOfSquaredWeights)
		  {
			return 1.0f;
		  }
	  }

	  public virtual void TestConstantScoreQueryAndFilter()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		Document doc = new Document();
		doc.add(newStringField("field", "a", Field.Store.NO));
		w.addDocument(doc);
		doc = new Document();
		doc.add(newStringField("field", "b", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		Filter filterB = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "b"))));
		Query query = new ConstantScoreQuery(filterB);

		IndexSearcher s = newSearcher(r);
		Assert.AreEqual(1, s.search(query, filterB, 1).totalHits); // Query for field:b, Filter field:b

		Filter filterA = new CachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("field", "a"))));
		query = new ConstantScoreQuery(filterA);

		Assert.AreEqual(0, s.search(query, filterB, 1).totalHits); // Query field:b, Filter field:a

		r.close();
		d.close();
	  }

	  // LUCENE-5307
	  // don't reuse the scorer of filters since they have been created with bulkScorer=false
	  public virtual void TestQueryWrapperFilter()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		Document doc = new Document();
		doc.add(newStringField("field", "a", Field.Store.NO));
		w.addDocument(doc);
		IndexReader r = w.Reader;
		w.close();

		Filter filter = new QueryWrapperFilter(AssertingQuery.wrap(random(), new TermQuery(new Term("field", "a"))));
		IndexSearcher s = newSearcher(r);
		Debug.Assert(s is AssertingIndexSearcher);
		// this used to fail
		s.search(new ConstantScoreQuery(filter), new TotalHitCountCollector());

		// check the rewrite
		Query rewritten = (new ConstantScoreQuery(filter)).rewrite(r);
		Assert.IsTrue(rewritten is ConstantScoreQuery);
		Assert.IsTrue(((ConstantScoreQuery) rewritten).Query is AssertingQuery);

		r.close();
		d.close();
	  }

	}

}