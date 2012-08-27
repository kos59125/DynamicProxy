using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace RecycleBin.DynamicProxy
{
   /// <summary>
   /// Dynamically generates proxy classes.
   /// </summary>
   public class DynamicProxyBuilder
   {
      private const MethodAttributes MethodAttribute = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual;
      private const MethodAttributes PropertyAttribute = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Virtual;
      private static readonly ConstructorInfo DefaultConstructor = typeof(object).GetConstructor(Type.EmptyTypes);

      private readonly AssemblyName assemblyName;
      private readonly AssemblyBuilder assemblyBuilder;
      private readonly ModuleBuilder moduleBuilder;
      private readonly Dictionary<Tuple<Type, Type>, Type> cache;

      /// <summary>
      /// Initializes a new instance.
      /// </summary>
      /// <param name="assemblyName">The name of assembly defining proxy types.</param>
      public DynamicProxyBuilder(string assemblyName)
      {
         this.assemblyName = new AssemblyName(assemblyName);
         this.assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(this.assemblyName, AssemblyBuilderAccess.RunAndSave);
         this.moduleBuilder = assemblyBuilder.DefineDynamicModule(this.assemblyName + ".dll");
         this.cache = new Dictionary<Tuple<Type, Type>, Type>();
      }

      /// <summary>
      /// Saves the current assembly as a file.
      /// </summary>
      public void Export()
      {
         this.assemblyBuilder.Save(this.assemblyName.Name + ".dll");
      }

      /// <summary>
      /// Creates a proxy of the specified object. 
      /// </summary>
      /// <typeparam name="TProxy">The interface type of proxy.</typeparam>
      /// <param name="entity">The instance.</param>
      /// <returns>The proxy.</returns>
      public object CreateProxy(Type proxyInterface, object entity)
      {
         if (entity == null)
         {
            throw new ArgumentNullException("entity");
         }
         return CreateProxy(proxyInterface, entity, entity.GetType());
      }

      /// <summary>
      /// Creates a proxy of the specified object. 
      /// </summary>
      /// <typeparam name="TProxy">The interface type of proxy.</typeparam>
      /// <param name="entity">The instance.</param>
      /// <param name="entityType">The static type of the instance.</param>
      /// <returns>The proxy.</returns>
      public object CreateProxy(Type proxyInterface, object entity, Type entityType)
      {
         if (entityType == null)
         {
            throw new ArgumentNullException("entityType");
         }
         var proxyType = CreateProxyType(proxyInterface, entityType);
         return Activator.CreateInstance(proxyType, entity);
      }

      /// <summary>
      /// Gets the proxy type of the entity type.
      /// </summary>
      /// <param name="proxyInterface">The interface type of proxy.</param>
      /// <param name="entityType">The static type of instance.</param>
      /// <returns>The proxy type implementing <paramref name="proxyInterface"/>.</returns>
      public Type CreateProxyType(Type proxyInterface, Type entityType)
      {
         if (entityType == null)
         {
            throw new ArgumentNullException("entityType");
         }
         if (proxyInterface == null)
         {
            throw new ArgumentNullException("proxyInterface");
         }
         if (!proxyInterface.IsInterface)
         {
            throw new ArgumentException(string.Format("{0} is not an interface type.", proxyInterface.FullName), "proxyInterface");
         }
         var tuple = Tuple.Create(entityType, proxyInterface);
         var proxyType = this.cache.TryGetValueOrCreate(tuple, () => CreateProxyTypeInternal(proxyInterface, entityType));
         return proxyType;
      }

      private Type CreateProxyTypeInternal(Type proxyInterface, Type entityType)
      {
         var attribute = proxyInterface.GetCustomAttributes(typeof(ProxyInterfaceAttribute), false) as ProxyInterfaceAttribute[];
         var implementedInterface = attribute.Length == 0 ? proxyInterface : attribute[0].InterfaceType;
         var proxyTypeName = string.Format("{0}.+{1}.+{2}.{3}Proxy", this.assemblyName.Name, implementedInterface.FullName, entityType.FullName, entityType.Name);
         var proxyTypeBuilder = this.moduleBuilder.DefineType(proxyTypeName, TypeAttributes.Public | TypeAttributes.SpecialName, typeof(object), new[] { implementedInterface });

         var self = proxyTypeBuilder.DefineField("self", entityType, FieldAttributes.Private);
         EmitConstructor(proxyTypeBuilder, self, entityType);

         foreach (var declaration in proxyInterface.GetMethods())
         {
            // Getters or setters should be handled as property.
            if (declaration.IsSpecialName)
            {
               continue;
            }
            var parameterTypes = declaration.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
            var returnType = declaration.ReturnType;
            var delegatedMethod = GetDelegateMethod(declaration, entityType, parameterTypes);
            var signature = declaration.DeclaringType == implementedInterface ? declaration : implementedInterface.GetMethod(declaration.Name, parameterTypes);
            EmitMethod(proxyTypeBuilder, self, signature, delegatedMethod, returnType, parameterTypes);
         }

         foreach (var declaration in proxyInterface.GetProperties())
         {
            var propertyType = declaration.PropertyType;
            var propertyBuilder = proxyTypeBuilder.DefineProperty(declaration.Name, PropertyAttributes.HasDefault, propertyType, null);
            var indexTypes = declaration.GetIndexParameters().Select(parameter => parameter.ParameterType).ToArray();
            var delegatedProperty = GetDelegateProperty(declaration, entityType, indexTypes);
            var signature = declaration.DeclaringType == implementedInterface ? declaration : implementedInterface.GetProperty(declaration.Name, indexTypes);
            EmitGetter(proxyTypeBuilder, propertyBuilder, self, signature, delegatedProperty, propertyType, indexTypes);
            EmitSetter(proxyTypeBuilder, propertyBuilder, self, signature, delegatedProperty, propertyType, indexTypes);
         }

         return proxyTypeBuilder.CreateType();
      }

      private static void EmitConstructor(TypeBuilder proxyTypeBuilder, FieldBuilder self, Type entityType)
      {
         var constructor = proxyTypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new[] { entityType });
         var generator = constructor.GetILGenerator();
         generator.Emit(OpCodes.Ldarg_0);
         generator.Emit(OpCodes.Call, DefaultConstructor);
         generator.Emit(OpCodes.Ldarg_0);
         generator.Emit(OpCodes.Ldarg_1);
         generator.Emit(OpCodes.Stfld, self);
         generator.Emit(OpCodes.Ret);
      }

      private static void EmitMethod(TypeBuilder proxyTypeBuilder, FieldBuilder self, MethodInfo declaration, MethodInfo delegatedMethod, Type returnType, Type[] parameterTypes)
      {
         var proxyMethod = proxyTypeBuilder.DefineMethod(declaration.Name, MethodAttribute, CallingConventions.HasThis, returnType, parameterTypes);
         var generator = proxyMethod.GetILGenerator();
         if (delegatedMethod == null)
         {
            generator.ThrowException(typeof(NotSupportedException));
         }
         else
         {
            EmitImplementation(generator, self, delegatedMethod, parameterTypes);
         }
         proxyTypeBuilder.DefineMethodOverride(proxyMethod, declaration);
      }

      private static void EmitGetter(TypeBuilder proxyTypeBuilder, PropertyBuilder propertyBuilder, FieldBuilder self, PropertyInfo declaration, PropertyInfo delegatedProperty, Type propertyType, Type[] indexTypes)
      {
         var getter = declaration.GetGetMethod();
         if (getter != null)
         {
            var getterBuilder = proxyTypeBuilder.DefineMethod("get_" + declaration.Name, PropertyAttribute, propertyType, indexTypes);
            var generator = getterBuilder.GetILGenerator();
            if (delegatedProperty == null)
            {
               generator.ThrowException(typeof(NotSupportedException));
            }
            else
            {
               var delegatedGetter = delegatedProperty.GetGetMethod();
               EmitImplementation(generator, self, delegatedGetter, indexTypes);
               propertyBuilder.SetGetMethod(getterBuilder);
            }
         }
      }

      private static void EmitSetter(TypeBuilder proxyTypeBuilder, PropertyBuilder propertyBuilder, FieldBuilder self, PropertyInfo declaration, PropertyInfo delegatedProperty, Type propertyType, Type[] indexTypes)
      {
         var setter = declaration.GetSetMethod();
         if (setter != null)
         {
            indexTypes = indexTypes.Concat(propertyType.AsSingleEnumerable()).ToArray();
            var setterBuilder = proxyTypeBuilder.DefineMethod("set_" + declaration.Name, PropertyAttribute, null, indexTypes);
            var generator = setterBuilder.GetILGenerator();
            if (delegatedProperty == null)
            {
               generator.ThrowException(typeof(NotSupportedException));
            }
            else
            {
               var delegatedSetter = delegatedProperty.GetSetMethod();
               EmitImplementation(generator, self, delegatedSetter, indexTypes);
               propertyBuilder.SetSetMethod(setterBuilder);
            }
         }
      }

      private static void EmitImplementation(ILGenerator generator, FieldBuilder self, MethodInfo delegatedMethod, Type[] parameterTypes)
      {
         generator.Emit(OpCodes.Ldarg_0);
         generator.Emit(OpCodes.Ldfld, self);
         // Indices begins from 1 (not 0).
         for (var index = 1; index <= parameterTypes.Length; index++)
         {
            generator.Emit(OpCodes.Ldarg_S, index);
         }
         generator.Emit(OpCodes.Callvirt, delegatedMethod);
         generator.Emit(OpCodes.Ret);
      }

      private static MethodInfo GetDelegateMethod(MethodInfo declaration, Type entityType, Type[] parameterTypes)
      {
         var attributes = Attribute.GetCustomAttributes(declaration, typeof(ProxyMethodAttribute));
         foreach (ProxyMethodAttribute proxyMethod in attributes)
         {
            var type = proxyMethod.EntityType;
            var method = entityType.GetMethod(proxyMethod.Target ?? declaration.Name, parameterTypes);
            if ((type == null && method != null) || type == entityType)
            {
               return method;
            }
         }
         return entityType.GetMethod(declaration.Name, parameterTypes);
      }

      private static PropertyInfo GetDelegateProperty(PropertyInfo declaration, Type entityType, Type[] indexTypes)
      {
         var attributes = Attribute.GetCustomAttributes(declaration, typeof(ProxyMethodAttribute));
         foreach (ProxyMethodAttribute proxyProperty in attributes)
         {
            var type = proxyProperty.EntityType;
            var property = entityType.GetProperty(proxyProperty.Target ?? declaration.Name, indexTypes);
            // In case of unusual indexer name and unspecified target property name.
            // see http://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.indexernameattribute.aspx
            // see http://msdn.microsoft.com/en-us/library/2549tw02.aspx
            if (indexTypes.Length != 0 && property == null && proxyProperty.Target == null)
            {
               property = entityType.GetProperties().FirstOrDefault(p => p.GetIndexParameters().Select(parameter => parameter.ParameterType).SequenceEqual(indexTypes));
            }
            if ((type == null && property != null) || type == entityType)
            {
               return property;
            }
         }
         return entityType.GetProperty(declaration.Name, indexTypes);
      }
   }
}
