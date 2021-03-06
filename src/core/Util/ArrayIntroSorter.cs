using System.Collections.Generic;

namespace Lucene.Net.Util
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

    /// <summary>
    /// An <seealso cref="IntroSorter"/> for object arrays.
    /// @lucene.internal
    /// </summary>
    public sealed class ArrayIntroSorter<T> : IntroSorter
    {
        private readonly T[] Arr;
        private readonly IComparer<T> Comparator;
        private T Pivot_Renamed;

        /// <summary>
        /// Create a new <seealso cref="ArrayInPlaceMergeSorter"/>. </summary>
        public ArrayIntroSorter(T[] arr, IComparer<T> comparator)
        {
            this.Arr = arr;
            this.Comparator = comparator;
            Pivot_Renamed = default(T);
        }

        protected internal override int Compare(int i, int j)
        {
            return Comparator.Compare(Arr[i], Arr[j]);
        }

        protected internal override void Swap(int i, int j)
        {
            ArrayUtil.Swap(Arr, i, j);
        }

        protected internal override int Pivot
        {
            set
            {
                Pivot_Renamed = Arr[value];
            }
        }

        protected internal override int ComparePivot(int i)
        {
            return Comparator.Compare(Pivot_Renamed, Arr[i]);
        }
    }
}