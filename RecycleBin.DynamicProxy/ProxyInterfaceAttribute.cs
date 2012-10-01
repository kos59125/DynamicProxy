using System;

namespace RecycleBin.DynamicProxy
{
   /// <summary>
   /// Specifies a implemented interface by a proxy interface.
   /// </summary>
   [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
   public class ProxyInterfaceAttribute : Attribute
   {
      private readonly Type interfaceType;
      /// <summary>
      /// Gets the interfaces to be implemented.
      /// </summary>
      public Type InterfaceType
      {
         get { return this.interfaceType; }
      }

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      /// <param name="interfaceType">The interface type.</param>
      public ProxyInterfaceAttribute(Type interfaceType)
      {
         if (interfaceType == null)
         {
            throw new ArgumentNullException("interface");
         }
         this.interfaceType = interfaceType;
      }
   }
}
