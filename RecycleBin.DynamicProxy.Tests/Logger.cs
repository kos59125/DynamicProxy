using System;
using System.IO;

namespace RecycleBin.DynamicProxy
{
   public interface IRecordable
   {
      void AppendText(string text);
   }

   public static class Logger
   {
      public static void LogMessage(string message, IRecordable recordable)
      {
         recordable.AppendText(message);
      }
   }

   [ProxyInterface(typeof(IRecordable))]
   public interface IProxyRecordable
   {
      [ProxyMethod(Target = "WriteLine", EntityType = typeof(TextWriter))]
      void AppendText(string text);
   }
}
