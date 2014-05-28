namespace Lucene.Net.Index
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
	using TextField = Lucene.Net.Document.TextField;
	using Directory = Lucene.Net.Store.Directory;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using TestUtil = Lucene.Net.Util.TestUtil;

	public class TestCodecHoldsOpenFiles : LuceneTestCase
	{
	  public virtual void Test()
	  {
		Directory d = newDirectory();
		RandomIndexWriter w = new RandomIndexWriter(random(), d);
		int numDocs = atLeast(100);
		for (int i = 0;i < numDocs;i++)
		{
		  Document doc = new Document();
		  doc.add(newField("foo", "bar", TextField.TYPE_NOT_STORED));
		  w.addDocument(doc);
		}

		IndexReader r = w.Reader;
		w.close();

		foreach (string fileName in d.listAll())
		{
		  try
		  {
			d.deleteFile(fileName);
		  }
		  catch (IOException ioe)
		  {
			// ignore: this means codec (correctly) is holding
			// the file open
		  }
		}

		foreach (AtomicReaderContext cxt in r.leaves())
		{
		  TestUtil.checkReader(cxt.reader());
		}

		r.close();
		d.close();
	  }
	}

}