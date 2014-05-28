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


	using Codec = Lucene.Net.Codecs.Codec;
	using Lucene3xCodec = Lucene.Net.Codecs.Lucene3x.Lucene3xCodec;

	/// <summary>
	/// Tests with the default randomized codec. Not really redundant with
	/// other specific instantiations since we want to test some test-only impls
	/// like Asserting, as well as make it easy to write a codec and pass -Dtests.codec
	/// </summary>
	public class TestTermVectorsFormat : BaseTermVectorsFormatTestCase
	{

	  protected internal override Codec Codec
	  {
		  get
		  {
			return Codec.Default;
		  }
	  }

	  protected internal override Set<Options> ValidOptions()
	  {
		if (Codec is Lucene3xCodec)
		{
		  // payloads are not supported on vectors in 3.x indexes
		  return EnumSet.range(Options.NONE, Options.POSITIONS_AND_OFFSETS);
		}
		else
		{
		  return base.validOptions();
		}
	  }

	  public override void TestMergeStability()
	  {
		assumeTrue("The MockRandom PF randomizes content on the fly, so we can't check it", false);
	  }

	}

}