using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace RecycleBin.DynamicProxy
{
   [TestFixture]
   internal class DynamicProxyBuilderTest
   {
      [Test]
      public void TestExport()
      {
         const string name = "RecycleBin.DynamicProxy.Proxy";
         const string dll = name + ".dll";
         try
         {
            Assert.That(File.Exists(dll), Is.False);
            var builder = new DynamicProxyBuilder(name);
            builder.CreateProxyType(typeof(IPerson), typeof(Person));
            builder.Export();
            Assert.That(File.Exists(dll), Is.True);
         }
         finally
         {
            try
            {
               File.Delete(dll);
            }
            catch (FileNotFoundException)
            {
            }
         }
      }

      [Test]
      public void TestCreateTypeWithoutProxyInterfaceAttribute()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var proxyType = builder.CreateProxyType(typeof(IPerson), typeof(Person));
         Assert.That(typeof(IPerson).IsAssignableFrom(proxyType), Is.True);
      }

      [Test]
      public void TestCreateTypeWithProxyInterfaceAttribute()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var proxyType = builder.CreateProxyType(typeof(IProxyRecordable), typeof(TextWriter));
         Assert.That(typeof(IRecordable).IsAssignableFrom(proxyType), Is.True);
      }

      [Test]
      [ExpectedException(typeof(NotSupportedException))]
      public void TestAnotherTypeGetter()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         Assert.That(proxy.AnotherTypeProperty, Throws.TypeOf<NotSupportedException>());
      }

      [Test]
      [ExpectedException(typeof(NotSupportedException))]
      public void TestAnotherTypeMethod()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         proxy.AnotherTypeMethod(null);
      }

      [Test]
      [ExpectedException(typeof(NotSupportedException))]
      public void TestAnotherTypeSetter()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         Assert.That(proxy.AnotherTypeProperty = null, Throws.TypeOf<NotSupportedException>());
      }

      [Test]
      [ExpectedException(typeof(NotSupportedException))]
      public void TestNotProxyGetter()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         Assert.That(proxy.NotProxyProperty, Throws.TypeOf<NotSupportedException>());
      }

      [Test]
      [ExpectedException(typeof(NotSupportedException))]
      public void TestNotProxyMethod()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         proxy.NotProxyMethod(null);
      }

      [Test]
      [ExpectedException(typeof(NotSupportedException))]
      public void TestNotProxySetter()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         Assert.That(proxy.NotProxyProperty = null, Throws.TypeOf<NotSupportedException>());
      }

      [Test]
      public void TestProxy()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         Assert.That(proxy, Is.Not.Null);
         Assert.That(proxy.Name, Is.EqualTo(person.Name));
         Assert.That(proxy.Date, Is.EqualTo(person.Birthday));
         Assert.That(proxy[1], Is.EqualTo(person[1]));
         var today = DateTime.Now;
         Assert.That(proxy.GetValue(today), Is.EqualTo(person.GetAge(today)));
         person.Name = "Bob";
         Assert.That(proxy.Name, Is.EqualTo("Bob"));
      }

      [Test]
      [ExpectedException(typeof(NotSupportedException))]
      public void TestWrongTarget()
      {
         var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
         var person = new Person("Alice", new DateTime(2000, 10, 2));
         var proxy = (IPerson)builder.CreateProxy(typeof(IPerson), person);
         Assert.That(proxy[1, 2], Throws.TypeOf<NotSupportedException>());
      }

      [Test]
      public void TestProxyInterface()
      {
         string log = string.Format("[{0}] something", DateTime.Now);
         var console = new StringBuilder();
         using (var stdout = new StringWriter(console))
         {
            Console.SetOut(stdout);
            var builder = new DynamicProxyBuilder("RecycleBin.DynamicProxy.Proxy");
            var proxy = (IRecordable)builder.CreateProxy(typeof(IProxyRecordable), Console.Out, typeof(TextWriter));
            Logger.LogMessage(log, proxy);
         }
         var expected = log + Environment.NewLine;
         Assert.That(console.ToString(), Is.EqualTo(expected));
      }
   }
}
