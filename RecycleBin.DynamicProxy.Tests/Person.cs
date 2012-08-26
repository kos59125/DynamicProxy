using System;
using System.Runtime.CompilerServices;

namespace RecycleBin.DynamicProxy
{
   public interface IPerson
   {
      [ProxyMethod]
      string this[int index] { get; }

      [ProxyMethod(Target = "WrongTarget", EntityType = typeof(Person))]
      string this[int index, int length] { get; }

      [ProxyMethod]
      string Name { get; }

      [ProxyMethod(Target = "Birthday")]
      DateTime Date { get; }

      string NotProxyProperty { get; set; }

      [ProxyMethod(EntityType = typeof(AnotherType))]
      string AnotherTypeProperty { get; set; }

      [ProxyMethod(Target = "GetAge")]
      int GetValue(DateTime date);

      void NotProxyMethod(object arg);

      [ProxyMethod(EntityType = typeof(AnotherType))]
      void AnotherTypeMethod(object arg);
   }

   public class Person
   {
      private readonly DateTime birthday;

      public Person(string name, DateTime birthday)
      {
         Name = name;
         this.birthday = birthday;
      }

      [IndexerName("Subname")]
      public string this[int index]
      {
         get { return Name.Substring(index); }
      }

      [IndexerName("Subname")]
      public string this[int index, int length]
      {
         get { return Name.Substring(index, length); }
      }

      public string Name { get; set; }

      public DateTime Birthday
      {
         get { return this.birthday; }
      }

      public int GetAge(DateTime date)
      {
         if (date < this.birthday)
         {
            throw new ArgumentOutOfRangeException("date");
         }
         if (date.Date >= this.birthday.Date)
         {
            return date.Year - this.birthday.Year;
         }
         return date.Year - this.birthday.Year - 1;
      }
   }

   public class AnotherType
   {
      public string AnotherTypeProperty { get; set; }
      public void AnotherTypeMethod() { }
   }
}
