using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Perfield
{

    using Lucene.Net.Support;
    using NUnit.Framework;
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


    using BasePostingsFormatTestCase = Lucene.Net.Index.BasePostingsFormatTestCase;
    using RandomCodec = Lucene.Net.Index.RandomCodec;

	/// <summary>
	/// Basic tests of PerFieldPostingsFormat
	/// </summary>
    [TestFixture]
    public class TestPerFieldPostingsFormat : BasePostingsFormatTestCase
	{

	  protected internal override Codec Codec
	  {
		  get
		  {
			return new RandomCodec(new Random(Random().Next()), new HashSet<string>());
		  }
	  }

	  [Test]
      public override void TestMergeStability()
	  {
		  //LUCENE TO-DO
          Assume.ReferenceEquals("The MockRandom PF randomizes content on the fly, so we can't check it", false);
	  }

	}

}