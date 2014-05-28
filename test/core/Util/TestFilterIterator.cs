using System.Collections.Generic;

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

namespace Lucene.Net.Util
{


	public class TestFilterIterator : LuceneTestCase
	{

	  private static readonly Set<string> Set = new SortedSet<string>(Arrays.asList("a", "b", "c"));

	  private static void assertNoMore<T1>(IEnumerator<T1> it)
	  {
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(it.hasNext());
		try
		{
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  it.next();
		  Assert.Fail("Should throw NoSuchElementException");
		}
		catch (NoSuchElementException nsee)
		{
		  // pass
		}
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsFalse(it.hasNext());
	  }

	  public virtual void TestEmpty()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper(this, Set.GetEnumerator());
		AssertNoMore(it);
	  }

	  private class FilterIteratorAnonymousInnerClassHelper : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return false;
		  }
	  }

	  public virtual void TestA1()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper2(this, Set.GetEnumerator());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("a", it.next());
		AssertNoMore(it);
	  }

	  private class FilterIteratorAnonymousInnerClassHelper2 : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper2(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return "a".Equals(s);
		  }
	  }

	  public virtual void TestA2()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper3(this, Set.GetEnumerator());
		// this time without check: Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("a", it.next());
		AssertNoMore(it);
	  }

	  private class FilterIteratorAnonymousInnerClassHelper3 : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper3(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return "a".Equals(s);
		  }
	  }

	  public virtual void TestB1()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper4(this, Set.GetEnumerator());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("b", it.next());
		AssertNoMore(it);
	  }

	  private class FilterIteratorAnonymousInnerClassHelper4 : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper4(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return "b".Equals(s);
		  }
	  }

	  public virtual void TestB2()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper5(this, Set.GetEnumerator());
		// this time without check: Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("b", it.next());
		AssertNoMore(it);
	  }

	  private class FilterIteratorAnonymousInnerClassHelper5 : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper5(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return "b".Equals(s);
		  }
	  }

	  public virtual void TestAll1()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper6(this, Set.GetEnumerator());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("a", it.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("b", it.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.IsTrue(it.hasNext());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("c", it.next());
		AssertNoMore(it);
	  }

	  private class FilterIteratorAnonymousInnerClassHelper6 : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper6(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return true;
		  }
	  }

	  public virtual void TestAll2()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper7(this, Set.GetEnumerator());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("a", it.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("b", it.next());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("c", it.next());
		AssertNoMore(it);
	  }

	  private class FilterIteratorAnonymousInnerClassHelper7 : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper7(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return true;
		  }
	  }

	  public virtual void TestUnmodifiable()
	  {
		IEnumerator<string> it = new FilterIteratorAnonymousInnerClassHelper8(this, Set.GetEnumerator());
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		Assert.AreEqual("a", it.next());
		try
		{
		  it.remove();
		  Assert.Fail("Should throw UnsupportedOperationException");
		}
		catch (System.NotSupportedException oue)
		{
		  // pass
		}
	  }

	  private class FilterIteratorAnonymousInnerClassHelper8 : FilterIterator<string>
	  {
		  private readonly TestFilterIterator OuterInstance;

		  public FilterIteratorAnonymousInnerClassHelper8(TestFilterIterator outerInstance, UnknownType iterator) : base(iterator)
		  {
			  this.OuterInstance = outerInstance;
		  }

		  protected internal override bool PredicateFunction(string s)
		  {
			return true;
		  }
	  }

	}

}