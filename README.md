DynamicProxy
============

This is a .NET Framework library providing a way to dynamically create proxy types.

Examples
--------

Suppose we have a logging interface like the following:

```csharp
public static class Logger
{
   public static void LogMessage(string message, IRecordable recordable)
   {
      recordable.AppendText(message);
   }
}

public interface IRecordable
{
   void AppendText(string text);
}
```

And, we'd like to output messages on the console like this:

```csharp
Logger.LogMessage("Hello, World.", Console.Out);
```

Of course this produces a compilation error because Console.Out does not implement IRecordable.
We can use DynamicProxy here like this:

```csharp
// Define proxy interface.
[ProxyInterface(typeof(IRecordable))]
public interface IProxyRecordable
{
   // Proxy method to be redirected.
   [ProxyMethod(Target = "WriteLine", EntityType = typeof(TextWriter))]
   void AppendText(string text);
}

// Creating a concrete proxy type and its instance.
var builder = new DynamicProxyBuilder("LoggingProxy");
var proxy = builder.CreateProxy(
   typeof(IProxyRecordable),  // The proxy type.
   Console.Out,               // The real instance to be used by the proxy.
   typeof(TextWriter)         // The instance type (identical to ProxyMethodAttribute.EntityType).
) as IRecordable;             // Can convert into the type defined in ProxyInterface.

// Use it.
Logger.LogMessage("Hello, World.", proxy);
```

