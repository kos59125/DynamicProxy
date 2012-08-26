using System;

namespace RecycleBin.DynamicProxy
{
   /// <summary>
   /// Indicates the attributed methods declared in an interfaces are used via dynamic proxy type.
   /// </summary>
   /// <seealso cref="DynamicProxyBuilder"/>
   [Serializable]
   [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
   public class ProxyMethodAttribute : Attribute
   {
      /// <summary>
      /// Gets or sets the type declaring the method to call via proxy.
      /// </summary>
      public Type EntityType { get; set; }

      /// <summary>
      /// Gets or sets the name of the method to call via proxy.
      /// </summary>
      public string Target { get; set; }
   }
}
