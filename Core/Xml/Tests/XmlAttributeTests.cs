﻿using System;
using NUnit.Framework;

namespace DeltaEngine.Core.Xml.Tests
{
	public class XmlAttributeTests
	{
		[Test]
		public void ConstructorWithObject()
		{
			var attribute = new XmlAttribute("name", DayOfWeek.Friday);
			Assert.AreEqual("name", attribute.Name);
			Assert.AreEqual("Friday", attribute.Value);
		}

		[Test]
		public void ConstructorWithFloat()
		{
			var attribute = new XmlAttribute("name", 2.1f);
			Assert.AreEqual("name", attribute.Name);
			Assert.AreEqual("2.1", attribute.Value);
		}

		[Test]
		public void ConstructorWithDouble()
		{
			var attribute = new XmlAttribute("name", 3.14);
			Assert.AreEqual("name", attribute.Name);
			Assert.AreEqual("3.14", attribute.Value);
		}

		[Test]
		public void Name()
		{
			var attribute = new XmlAttribute("name", "value") { Name = "new name" };
			Assert.AreEqual("new name", attribute.Name);
		}

		[Test]
		public void Value()
		{
			var attribute = new XmlAttribute("name", 3) { Value = "new value" };
			Assert.AreEqual("new value", attribute.Value);
		}
	}
}