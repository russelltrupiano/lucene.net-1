using System;
using System.Collections.Generic;
using System.Text;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using StringField = Lucene.Net.Document.StringField;
	using TextField = Lucene.Net.Document.TextField;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
	using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
	using Term = Lucene.Net.Index.Term;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using BeforeClass = org.junit.BeforeClass;

	public class TestLucene40PostingsReader : LuceneTestCase
	{
	  internal static readonly string[] Terms = new string[100];
	  static TestLucene40PostingsReader()
	  {
		for (int i = 0; i < Terms.Length; i++)
		{
		  Terms[i] = Convert.ToString(i + 1);
		}
	  }

//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @BeforeClass public static void beforeClass()
	  public static void BeforeClass()
	  {
		OLD_FORMAT_IMPERSONATION_IS_ACTIVE = true; // explicitly instantiates ancient codec
	  }

	  /// <summary>
	  /// tests terms with different probabilities of being in the document.
	  ///  depends heavily on term vectors cross-check at checkIndex
	  /// </summary>
	  public virtual void TestPostings()
	  {
		Directory dir = newFSDirectory(createTempDir("postings"));
		IndexWriterConfig iwc = newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()));
		iwc.Codec = Codec.forName("Lucene40");
		RandomIndexWriter iw = new RandomIndexWriter(random(), dir, iwc);

		Document doc = new Document();

		// id field
		FieldType idType = new FieldType(StringField.TYPE_NOT_STORED);
		idType.StoreTermVectors = true;
		Field idField = new Field("id", "", idType);
		doc.add(idField);

		// title field: short text field
		FieldType titleType = new FieldType(TextField.TYPE_NOT_STORED);
		titleType.StoreTermVectors = true;
		titleType.StoreTermVectorPositions = true;
		titleType.StoreTermVectorOffsets = true;
		titleType.IndexOptions = IndexOptions();
		Field titleField = new Field("title", "", titleType);
		doc.add(titleField);

		// body field: long text field
		FieldType bodyType = new FieldType(TextField.TYPE_NOT_STORED);
		bodyType.StoreTermVectors = true;
		bodyType.StoreTermVectorPositions = true;
		bodyType.StoreTermVectorOffsets = true;
		bodyType.IndexOptions = IndexOptions();
		Field bodyField = new Field("body", "", bodyType);
		doc.add(bodyField);

		int numDocs = atLeast(1000);
		for (int i = 0; i < numDocs; i++)
		{
		  idField.StringValue = Convert.ToString(i);
		  titleField.StringValue = FieldValue(1);
		  bodyField.StringValue = FieldValue(3);
		  iw.addDocument(doc);
		  if (random().Next(20) == 0)
		  {
			iw.deleteDocuments(new Term("id", Convert.ToString(i)));
		  }
		}
		if (random().nextBoolean())
		{
		  // delete 1-100% of docs
		  iw.deleteDocuments(new Term("title", Terms[random().Next(Terms.Length)]));
		}
		iw.close();
		dir.close(); // checkindex
	  }

	  internal virtual IndexOptions IndexOptions()
	  {
		switch (random().Next(4))
		{
		  case 0:
			  return IndexOptions.DOCS_ONLY;
		  case 1:
			  return IndexOptions.DOCS_AND_FREQS;
		  case 2:
			  return IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
		  default:
			  return IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
		}
	  }

	  internal virtual string FieldValue(int maxTF)
	  {
		List<string> shuffled = new List<string>();
		StringBuilder sb = new StringBuilder();
		int i = random().Next(Terms.Length);
		while (i < Terms.Length)
		{
		  int tf = TestUtil.Next(random(), 1, maxTF);
		  for (int j = 0; j < tf; j++)
		  {
			shuffled.Add(Terms[i]);
		  }
		  i++;
		}
		Collections.shuffle(shuffled, random());
		foreach (string term in shuffled)
		{
		  sb.Append(term);
		  sb.Append(' ');
		}
		return sb.ToString();
	  }
	}

}