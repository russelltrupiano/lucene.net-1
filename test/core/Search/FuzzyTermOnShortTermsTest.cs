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
namespace Lucene.Net.Search
{



	using Analyzer = Lucene.Net.Analysis.Analyzer;
	using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
	using Tokenizer = Lucene.Net.Analysis.Tokenizer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using DirectoryReader = Lucene.Net.Index.DirectoryReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using Test = org.junit.Test;


	public class FuzzyTermOnShortTermsTest : LuceneTestCase
	{
	   private const string FIELD = "field";

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void test() throws Exception
	   public virtual void Test()
	   {
		  // proves rule that edit distance between the two terms
		  // must be > smaller term for there to be a match
		  Analyzer a = Analyzer;
		  //these work
		  CountHits(a, new string[]{"abc"}, new FuzzyQuery(new Term(FIELD, "ab"), 1), 1);
		  CountHits(a, new string[]{"ab"}, new FuzzyQuery(new Term(FIELD, "abc"), 1), 1);

		  CountHits(a, new string[]{"abcde"}, new FuzzyQuery(new Term(FIELD, "abc"), 2), 1);
		  CountHits(a, new string[]{"abc"}, new FuzzyQuery(new Term(FIELD, "abcde"), 2), 1);

		  //these don't      
		  CountHits(a, new string[]{"ab"}, new FuzzyQuery(new Term(FIELD, "a"), 1), 0);
		  CountHits(a, new string[]{"a"}, new FuzzyQuery(new Term(FIELD, "ab"), 1), 0);

		  CountHits(a, new string[]{"abc"}, new FuzzyQuery(new Term(FIELD, "a"), 2), 0);
		  CountHits(a, new string[]{"a"}, new FuzzyQuery(new Term(FIELD, "abc"), 2), 0);

		  CountHits(a, new string[]{"abcd"}, new FuzzyQuery(new Term(FIELD, "ab"), 2), 0);
		  CountHits(a, new string[]{"ab"}, new FuzzyQuery(new Term(FIELD, "abcd"), 2), 0);
	   }

	   private void CountHits(Analyzer analyzer, string[] docs, Query q, int expected)
	   {
		  Directory d = GetDirectory(analyzer, docs);
		  IndexReader r = DirectoryReader.open(d);
		  IndexSearcher s = new IndexSearcher(r);
		  TotalHitCountCollector c = new TotalHitCountCollector();
		  s.search(q, c);
		  Assert.AreEqual(q.ToString(), expected, c.TotalHits);
		  r.close();
		  d.close();
	   }

	   public static Analyzer Analyzer
	   {
		   get
		   {
			  return new AnalyzerAnonymousInnerClassHelper();
		   }
	   }

	   private class AnalyzerAnonymousInnerClassHelper : Analyzer
	   {
		   public AnalyzerAnonymousInnerClassHelper()
		   {
		   }

		   public override TokenStreamComponents CreateComponents(string fieldName, Reader reader)
		   {
			  Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
			  return new TokenStreamComponents(tokenizer, tokenizer);
		   }
	   }
	   public static Directory GetDirectory(Analyzer analyzer, string[] vals)
	   {
		  Directory directory = newDirectory();
		  RandomIndexWriter writer = new RandomIndexWriter(random(), directory, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).setMaxBufferedDocs(TestUtil.Next(random(), 100, 1000)).setMergePolicy(newLogMergePolicy()));

		  foreach (string s in vals)
		  {
			 Document d = new Document();
			 d.add(newTextField(FIELD, s, Field.Store.YES));
			 writer.addDocument(d);

		  }
		  writer.close();
		  return directory;
	   }
	}

}