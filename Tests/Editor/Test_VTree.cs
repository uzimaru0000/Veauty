using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Veauty;
using Veauty.VTree;

namespace Tests
{
    public class Test_VTree
    {
        [Test]
        public void A()
        {
            new Veauty.VTree.Node("test", new IAttribute[]{}, new IVTree[]{});
        }
    }
}
