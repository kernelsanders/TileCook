﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Moq;

namespace TileCook.Test
{
    [TestClass]
    public class LayerTests
    {

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullGridSet_throws()
        {
            Layer l = new LayerBuilder()
                .Initialize()
                .SetGridSet(null);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Ctor_NullName_throws()
        {
            Layer l = new LayerBuilder()
                .Initialize()
                .SetName(null);
        }

        [TestMethod]
        public void Ctor_BareBonesArgs_DefaultsSetCorrectly()
        {
            Layer l = new LayerBuilder()
                .Initialize();

            Assert.IsTrue(l.Bounds.Equals(new Envelope(0, 0, 0, 0)));
            Assert.AreEqual(l.MaxZoom, 1);
            Assert.IsNotNull(l.Formats);
        }
    }
}
