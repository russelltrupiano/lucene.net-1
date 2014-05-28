using System;
using System.Threading;

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


	using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
	using Document = Lucene.Net.Document.Document;
	using Field = Lucene.Net.Document.Field;
	using FieldType = Lucene.Net.Document.FieldType;
	using NumericDocValuesField = Lucene.Net.Document.NumericDocValuesField;
	using TextField = Lucene.Net.Document.TextField;
	using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
	using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
	using Directory = Lucene.Net.Store.Directory;
	using LockObtainFailedException = Lucene.Net.Store.LockObtainFailedException;
	using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
	using Bits = Lucene.Net.Util.Bits;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using LineFileDocs = Lucene.Net.Util.LineFileDocs;
	using SuppressCodecs = Lucene.Net.Util.LuceneTestCase.SuppressCodecs;
	using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
	using TestUtil = Lucene.Net.Util.TestUtil;
	using ThreadInterruptedException = Lucene.Net.Util.ThreadInterruptedException;
	using Slow = Lucene.Net.Util.LuceneTestCase.Slow;

	/// <summary>
	/// MultiThreaded IndexWriter tests
	/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressCodecs("Lucene3x") @Slow public class TestIndexWriterWithThreads extends Lucene.Net.Util.LuceneTestCase
	public class TestIndexWriterWithThreads : LuceneTestCase
	{

	  // Used by test cases below
	  private class IndexerThread : System.Threading.Thread
	  {
		  private readonly TestIndexWriterWithThreads OuterInstance;


		internal bool DiskFull;
		internal Exception Error;
		internal AlreadyClosedException Ace;
		internal IndexWriter Writer;
		internal bool NoErrors;
		internal volatile int AddCount;

		public IndexerThread(TestIndexWriterWithThreads outerInstance, IndexWriter writer, bool noErrors)
		{
			this.OuterInstance = outerInstance;
		  this.Writer = writer;
		  this.NoErrors = noErrors;
		}

		public override void Run()
		{

		  Document doc = new Document();
		  FieldType customType = new FieldType(TextField.TYPE_STORED);
		  customType.StoreTermVectors = true;
		  customType.StoreTermVectorPositions = true;
		  customType.StoreTermVectorOffsets = true;

		  doc.add(newField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));
		  doc.add(new NumericDocValuesField("dv", 5));

		  int idUpto = 0;
		  int fullCount = 0;
		  long stopTime = System.currentTimeMillis() + 200;

		  do
		  {
			try
			{
			  Writer.updateDocument(new Term("id", "" + (idUpto++)), doc);
			  AddCount++;
			}
			catch (IOException ioe)
			{
			  if (VERBOSE)
			  {
				Console.WriteLine("TEST: expected exc:");
				ioe.printStackTrace(System.out);
			  }
			  //System.out.println(Thread.currentThread().getName() + ": hit exc");
			  //ioe.printStackTrace(System.out);
			  if (ioe.Message.StartsWith("fake disk full at") || ioe.Message.Equals("now failing on purpose"))
			  {
				DiskFull = true;
				try
				{
				  Thread.Sleep(1);
				}
				catch (InterruptedException ie)
				{
				  throw new ThreadInterruptedException(ie);
				}
				if (fullCount++ >= 5)
				{
				  break;
				}
			  }
			  else
			  {
				if (NoErrors)
				{
				  Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected IOException:");
				  ioe.printStackTrace(System.out);
				  Error = ioe;
				}
				break;
			  }
			}
			catch (Exception t)
			{
			  //t.printStackTrace(System.out);
			  if (NoErrors)
			  {
				Console.WriteLine(Thread.CurrentThread.Name + ": ERROR: unexpected Throwable:");
				t.printStackTrace(System.out);
				Error = t;
			  }
			  break;
			}
		  } while (System.currentTimeMillis() < stopTime);
		}
	  }

	  // LUCENE-1130: make sure immediate disk full on creating
	  // an IndexWriter (hit during DW.ThreadState.init()), with
	  // multiple threads, is OK:
	  public virtual void TestImmediateDiskFullWithThreads()
	  {

		int NUM_THREADS = 3;
		int numIterations = TEST_NIGHTLY ? 10 : 3;
		for (int iter = 0;iter < numIterations;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter);
		  }
		  MockDirectoryWrapper dir = newMockDirectory();
		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(4)));
		  ((ConcurrentMergeScheduler) writer.Config.MergeScheduler).setSuppressExceptions();
		  dir.MaxSizeInBytes = 4 * 1024 + 20 * iter;

		  IndexerThread[] threads = new IndexerThread[NUM_THREADS];

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i] = new IndexerThread(this, writer, true);
		  }

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i].Start();
		  }

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			// Without fix for LUCENE-1130: one of the
			// threads will hang
			threads[i].Join();
			Assert.IsTrue("hit unexpected Throwable", threads[i].Error == null);
		  }

		  // Make sure once disk space is avail again, we can
		  // cleanly close:
		  dir.MaxSizeInBytes = 0;
		  writer.close(false);
		  dir.close();
		}
	  }


	  // LUCENE-1130: make sure we can close() even while
	  // threads are trying to add documents.  Strictly
	  // speaking, this isn't valid us of Lucene's APIs, but we
	  // still want to be robust to this case:
	  public virtual void TestCloseWithThreads()
	  {
		int NUM_THREADS = 3;
		int numIterations = TEST_NIGHTLY ? 7 : 3;
		for (int iter = 0;iter < numIterations;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: iter=" + iter);
		  }
		  Directory dir = newDirectory();

		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(10).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(4)));
		  ((ConcurrentMergeScheduler) writer.Config.MergeScheduler).setSuppressExceptions();

		  IndexerThread[] threads = new IndexerThread[NUM_THREADS];

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i] = new IndexerThread(this, writer, false);
		  }

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i].Start();
		  }

		  bool done = false;
		  while (!done)
		  {
			Thread.Sleep(100);
			for (int i = 0;i < NUM_THREADS;i++)
			  // only stop when at least one thread has added a doc
			{
			  if (threads[i].AddCount > 0)
			  {
				done = true;
				break;
			  }
			  else if (!threads[i].IsAlive)
			  {
				Assert.Fail("thread failed before indexing a single document");
			  }
			}
		  }

		  if (VERBOSE)
		  {
			Console.WriteLine("\nTEST: now close");
		  }
		  writer.close(false);

		  // Make sure threads that are adding docs are not hung:
		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			// Without fix for LUCENE-1130: one of the
			// threads will hang
			threads[i].Join();
			if (threads[i].IsAlive)
			{
			  Assert.Fail("thread seems to be hung");
			}
		  }

		  // Quick test to make sure index is not corrupt:
		  IndexReader reader = DirectoryReader.open(dir);
		  DocsEnum tdocs = TestUtil.docs(random(), reader, "field", new BytesRef("aaa"), MultiFields.getLiveDocs(reader), null, 0);
		  int count = 0;
		  while (tdocs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
		  {
			count++;
		  }
		  Assert.IsTrue(count > 0);
		  reader.close();

		  dir.close();
		}
	  }

	  // Runs test, with multiple threads, using the specific
	  // failure to trigger an IOException
	  public virtual void _testMultipleThreadsFailure(MockDirectoryWrapper.Failure failure)
	  {

		int NUM_THREADS = 3;

		for (int iter = 0;iter < 2;iter++)
		{
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: iter=" + iter);
		  }
		  MockDirectoryWrapper dir = newMockDirectory();

		  IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergeScheduler(new ConcurrentMergeScheduler()).setMergePolicy(newLogMergePolicy(4)));
		  ((ConcurrentMergeScheduler) writer.Config.MergeScheduler).setSuppressExceptions();

		  IndexerThread[] threads = new IndexerThread[NUM_THREADS];

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i] = new IndexerThread(this, writer, true);
		  }

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i].Start();
		  }

		  Thread.Sleep(10);

		  dir.failOn(failure);
		  failure.setDoFail();

		  for (int i = 0;i < NUM_THREADS;i++)
		  {
			threads[i].Join();
			Assert.IsTrue("hit unexpected Throwable", threads[i].Error == null);
		  }

		  bool success = false;
		  try
		  {
			writer.close(false);
			success = true;
		  }
		  catch (IOException ioe)
		  {
			failure.clearDoFail();
			writer.close(false);
		  }
		  if (VERBOSE)
		  {
			Console.WriteLine("TEST: success=" + success);
		  }

		  if (success)
		  {
			IndexReader reader = DirectoryReader.open(dir);
			Bits delDocs = MultiFields.getLiveDocs(reader);
			for (int j = 0;j < reader.maxDoc();j++)
			{
			  if (delDocs == null || !delDocs.get(j))
			  {
				reader.document(j);
				reader.getTermVectors(j);
			  }
			}
			reader.close();
		  }

		  dir.close();
		}
	  }

	  // Runs test, with one thread, using the specific failure
	  // to trigger an IOException
	  public virtual void _testSingleThreadFailure(MockDirectoryWrapper.Failure failure)
	  {
		MockDirectoryWrapper dir = newMockDirectory();

		IndexWriter writer = new IndexWriter(dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())).setMaxBufferedDocs(2).setMergeScheduler(new ConcurrentMergeScheduler()));
		Document doc = new Document();
		FieldType customType = new FieldType(TextField.TYPE_STORED);
		customType.StoreTermVectors = true;
		customType.StoreTermVectorPositions = true;
		customType.StoreTermVectorOffsets = true;
		doc.add(newField("field", "aaa bbb ccc ddd eee fff ggg hhh iii jjj", customType));

		for (int i = 0;i < 6;i++)
		{
		  writer.addDocument(doc);
		}

		dir.failOn(failure);
		failure.setDoFail();
		try
		{
		  writer.addDocument(doc);
		  writer.addDocument(doc);
		  writer.commit();
		  Assert.Fail("did not hit exception");
		}
		catch (IOException ioe)
		{
		}
		failure.clearDoFail();
		writer.addDocument(doc);
		writer.close(false);
		dir.close();
	  }

	  // Throws IOException during FieldsWriter.flushDocument and during DocumentsWriter.abort
	  private class FailOnlyOnAbortOrFlush : MockDirectoryWrapper.Failure
	  {
		internal bool OnlyOnce;
		public FailOnlyOnAbortOrFlush(bool onlyOnce)
		{
		  this.OnlyOnce = onlyOnce;
		}
		public override void Eval(MockDirectoryWrapper dir)
		{

		  // Since we throw exc during abort, eg when IW is
		  // attempting to delete files, we will leave
		  // leftovers: 
		  dir.AssertNoUnrefencedFilesOnClose = false;

		  if (doFail)
		  {
			StackTraceElement[] trace = (new Exception()).StackTrace;
			bool sawAbortOrFlushDoc = false;
			bool sawClose = false;
			bool sawMerge = false;
			for (int i = 0; i < trace.Length; i++)
			{
			  if (sawAbortOrFlushDoc && sawMerge && sawClose)
			  {
				break;
			  }
			  if ("abort".Equals(trace[i].MethodName) || "finishDocument".Equals(trace[i].MethodName))
			  {
				sawAbortOrFlushDoc = true;
			  }
			  if ("merge".Equals(trace[i].MethodName))
			  {
				sawMerge = true;
			  }
			  if ("close".Equals(trace[i].MethodName))
			  {
				sawClose = true;
			  }
			}
			if (sawAbortOrFlushDoc && !sawClose && !sawMerge)
			{
			  if (OnlyOnce)
			  {
				doFail = false;
			  }
			  //System.out.println(Thread.currentThread().getName() + ": now fail");
			  //new Throwable().printStackTrace(System.out);
			  throw new IOException("now failing on purpose");
			}
		  }
		}
	  }



	  // LUCENE-1130: make sure initial IOException, and then 2nd
	  // IOException during rollback(), is OK:
	  public virtual void TestIOExceptionDuringAbort()
	  {
		_testSingleThreadFailure(new FailOnlyOnAbortOrFlush(false));
	  }

	  // LUCENE-1130: make sure initial IOException, and then 2nd
	  // IOException during rollback(), is OK:
	  public virtual void TestIOExceptionDuringAbortOnlyOnce()
	  {
		_testSingleThreadFailure(new FailOnlyOnAbortOrFlush(true));
	  }

	  // LUCENE-1130: make sure initial IOException, and then 2nd
	  // IOException during rollback(), with multiple threads, is OK:
	  public virtual void TestIOExceptionDuringAbortWithThreads()
	  {
		_testMultipleThreadsFailure(new FailOnlyOnAbortOrFlush(false));
	  }

	  // LUCENE-1130: make sure initial IOException, and then 2nd
	  // IOException during rollback(), with multiple threads, is OK:
	  public virtual void TestIOExceptionDuringAbortWithThreadsOnlyOnce()
	  {
		_testMultipleThreadsFailure(new FailOnlyOnAbortOrFlush(true));
	  }

	  // Throws IOException during DocumentsWriter.writeSegment
	  private class FailOnlyInWriteSegment : MockDirectoryWrapper.Failure
	  {
		internal bool OnlyOnce;
		public FailOnlyInWriteSegment(bool onlyOnce)
		{
		  this.OnlyOnce = onlyOnce;
		}
		public override void Eval(MockDirectoryWrapper dir)
		{
		  if (doFail)
		  {
			StackTraceElement[] trace = (new Exception()).StackTrace;
			for (int i = 0; i < trace.Length; i++)
			{
			  if ("flush".Equals(trace[i].MethodName) && "Lucene.Net.Index.DocFieldProcessor".Equals(trace[i].ClassName))
			  {
				if (OnlyOnce)
				{
				  doFail = false;
				}
				//System.out.println(Thread.currentThread().getName() + ": NOW FAIL: onlyOnce=" + onlyOnce);
				//new Throwable().printStackTrace(System.out);
				throw new IOException("now failing on purpose");
			  }
			}
		  }
		}
	  }

	  // LUCENE-1130: test IOException in writeSegment
	  public virtual void TestIOExceptionDuringWriteSegment()
	  {
		_testSingleThreadFailure(new FailOnlyInWriteSegment(false));
	  }

	  // LUCENE-1130: test IOException in writeSegment
	  public virtual void TestIOExceptionDuringWriteSegmentOnlyOnce()
	  {
		_testSingleThreadFailure(new FailOnlyInWriteSegment(true));
	  }

	  // LUCENE-1130: test IOException in writeSegment, with threads
	  public virtual void TestIOExceptionDuringWriteSegmentWithThreads()
	  {
		_testMultipleThreadsFailure(new FailOnlyInWriteSegment(false));
	  }

	  // LUCENE-1130: test IOException in writeSegment, with threads
	  public virtual void TestIOExceptionDuringWriteSegmentWithThreadsOnlyOnce()
	  {
		_testMultipleThreadsFailure(new FailOnlyInWriteSegment(true));
	  }

	  //  LUCENE-3365: Test adding two documents with the same field from two different IndexWriters 
	  //  that we attempt to open at the same time.  As long as the first IndexWriter completes
	  //  and closes before the second IndexWriter time's out trying to get the Lock,
	  //  we should see both documents
	  public virtual void TestOpenTwoIndexWritersOnDifferentThreads()
	  {
		 Directory dir = newDirectory();
		 CountDownLatch oneIWConstructed = new CountDownLatch(1);
		 DelayedIndexAndCloseRunnable thread1 = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed);
		 DelayedIndexAndCloseRunnable thread2 = new DelayedIndexAndCloseRunnable(dir, oneIWConstructed);

		 thread1.Start();
		 thread2.Start();
		 oneIWConstructed.@await();

		 thread1.StartIndexing();
		 thread2.StartIndexing();

		 thread1.Join();
		 thread2.Join();

		 // ensure the directory is closed if we hit the timeout and throw assume
		 // TODO: can we improve this in LuceneTestCase? I dont know what the logic would be...
		 try
		 {
		   assumeFalse("aborting test: timeout obtaining lock", thread1.Failure is LockObtainFailedException);
		   assumeFalse("aborting test: timeout obtaining lock", thread2.Failure is LockObtainFailedException);

		   Assert.IsFalse("Failed due to: " + thread1.Failure, thread1.Failed);
		   Assert.IsFalse("Failed due to: " + thread2.Failure, thread2.Failed);
		   // now verify that we have two documents in the index
		   IndexReader reader = DirectoryReader.open(dir);
		   Assert.AreEqual("IndexReader should have one document per thread running", 2, reader.numDocs());

		   reader.close();
		 }
		 finally
		 {
		   dir.close();
		 }
	  }

	  internal class DelayedIndexAndCloseRunnable : System.Threading.Thread
	  {
		internal readonly Directory Dir;
		internal bool Failed = false;
		internal Exception Failure = null;
		internal readonly CountDownLatch StartIndexing_Renamed = new CountDownLatch(1);
		internal CountDownLatch IwConstructed;

		public DelayedIndexAndCloseRunnable(Directory dir, CountDownLatch iwConstructed)
		{
		  this.Dir = dir;
		  this.IwConstructed = iwConstructed;
		}

		public virtual void StartIndexing()
		{
		  this.StartIndexing_Renamed.countDown();
		}

		public override void Run()
		{
		  try
		  {
			Document doc = new Document();
			Field field = newTextField("field", "testData", Field.Store.YES);
			doc.add(field);
			IndexWriter writer = new IndexWriter(Dir, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random())));
			IwConstructed.countDown();
			StartIndexing_Renamed.@await();
			writer.addDocument(doc);
			writer.close();
		  }
		  catch (Exception e)
		  {
			Failed = true;
			Failure = e;
			Failure.printStackTrace(System.out);
			return;
		  }
		}
	  }

	  // LUCENE-4147
	  public virtual void TestRollbackAndCommitWithThreads()
	  {
		BaseDirectoryWrapper d = newDirectory();
		if (d is MockDirectoryWrapper)
		{
		  ((MockDirectoryWrapper)d).PreventDoubleWrite = false;
		}

		int threadCount = TestUtil.Next(random(), 2, 6);

		AtomicReference<IndexWriter> writerRef = new AtomicReference<IndexWriter>();
		MockAnalyzer analyzer = new MockAnalyzer(random());
		analyzer.MaxTokenLength = TestUtil.Next(random(), 1, IndexWriter.MAX_TERM_LENGTH);

		writerRef.set(new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, analyzer)));
		LineFileDocs docs = new LineFileDocs(random());
		Thread[] threads = new Thread[threadCount];
		int iters = atLeast(100);
		AtomicBoolean failed = new AtomicBoolean();
		Lock rollbackLock = new ReentrantLock();
		Lock commitLock = new ReentrantLock();
		for (int threadID = 0;threadID < threadCount;threadID++)
		{
		  threads[threadID] = new ThreadAnonymousInnerClassHelper(this, d, writerRef, docs, iters, failed, rollbackLock, commitLock);
		  threads[threadID].Start();
		}

		for (int threadID = 0;threadID < threadCount;threadID++)
		{
		  threads[threadID].Join();
		}

		Assert.IsTrue(!failed.get());
		writerRef.get().close();
		d.close();
	  }

	  private class ThreadAnonymousInnerClassHelper : System.Threading.Thread
	  {
		  private readonly TestIndexWriterWithThreads OuterInstance;

		  private BaseDirectoryWrapper d;
		  private AtomicReference<IndexWriter> WriterRef;
		  private LineFileDocs Docs;
		  private int Iters;
		  private AtomicBoolean Failed;
		  private Lock RollbackLock;
		  private Lock CommitLock;

		  public ThreadAnonymousInnerClassHelper(TestIndexWriterWithThreads outerInstance, BaseDirectoryWrapper d, AtomicReference<IndexWriter> writerRef, LineFileDocs docs, int iters, AtomicBoolean failed, Lock rollbackLock, Lock commitLock)
		  {
			  this.OuterInstance = outerInstance;
			  this.d = d;
			  this.WriterRef = writerRef;
			  this.Docs = docs;
			  this.Iters = iters;
			  this.Failed = failed;
			  this.RollbackLock = rollbackLock;
			  this.CommitLock = commitLock;
		  }

		  public override void Run()
		  {
			for (int iter = 0;iter < Iters && !Failed.get();iter++)
			{
			  //final int x = random().nextInt(5);
			  int x = random().Next(3);
			  try
			  {
				switch (x)
				{
				case 0:
				  RollbackLock.@lock();
				  if (VERBOSE)
				  {
					Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now rollback");
				  }
				  try
				  {
					WriterRef.get().rollback();
					if (VERBOSE)
					{
					  Console.WriteLine("TEST: " + Thread.CurrentThread.Name + ": rollback done; now open new writer");
					}
					WriterRef.set(new IndexWriter(d, newIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random()))));
				  }
				  finally
				  {
					RollbackLock.unlock();
				  }
				  break;
				case 1:
				  CommitLock.@lock();
				  if (VERBOSE)
				  {
					Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now commit");
				  }
				  try
				  {
					if (random().nextBoolean())
					{
					  WriterRef.get().prepareCommit();
					}
					WriterRef.get().commit();
				  }
				  catch (AlreadyClosedException ace)
				  {
					// ok
				  }
				  catch (System.NullReferenceException npe)
				  {
					// ok
				  }
				  finally
				  {
					CommitLock.unlock();
				  }
				  break;
				case 2:
				  if (VERBOSE)
				  {
					Console.WriteLine("\nTEST: " + Thread.CurrentThread.Name + ": now add");
				  }
				  try
				  {
					WriterRef.get().addDocument(Docs.nextDoc());
				  }
				  catch (AlreadyClosedException ace)
				  {
					// ok
				  }
				  catch (System.NullReferenceException npe)
				  {
					// ok
				  }
				  catch (AssertionError ae)
				  {
					// ok
				  }
				  break;
				}
			  }
			  catch (Exception t)
			  {
				Failed.set(true);
				throw new Exception(t);
			  }
			}
		  }
	  }
	}

}