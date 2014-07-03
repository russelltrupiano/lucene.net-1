﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
    public class TermToBytesRefAttribute : Attribute, ITermToBytesRefAttribute
    {
        private BytesRef Bytes;
        
        public void FillBytesRef()
        {
            throw new NotImplementedException("I'm not sure what this should do");
        }

        public BytesRef BytesRef { get; set; }
        
        public override void Clear()
        {
        }

        public override void CopyTo(Attribute target)
        {
            TermToBytesRefAttribute other = (TermToBytesRefAttribute) target;
            other.Bytes = Bytes;
        }
    }
}
