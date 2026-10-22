using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IL2CppApi;
using IL2CppApi.Runtime;
using IL2CppApi.Wrappers;

namespace SnapLocalGameLab.Core
{
    internal sealed class GameApi : IDisposable
    {
        public readonly Il2CppRuntime Runtime = new Il2CppRuntime();
        private static readonly Dictionary<string, IL2CppClassWrapper> Classes = new Dictionary<string, IL2CppClassWrapper>();

        public IL2CppClassWrapper Type(string ns, string name)
        {
            var key = ns + "." + name;
            if (Classes.TryGetValue(key, out var result)) return result;
            foreach (var image in IL2CppDumper.GetLoadedAssemblies())
            {
                result = Runtime.Class(image.Name, ns, name, true);
                if (result != null && !result.IsNull) return Classes[key] = result;
            }
            throw new NotSupportedException("Game type not found: " + key);
        }

        public IL2CppMethodInfoWrapper Method(IL2CppClassWrapper type, string name, int count, bool isStatic = false)
        {
            for (var owner = type; owner != null && !owner.IsNull; owner = owner.Parent)
            {
                var matches = owner.GetMethods().Where(m => m.Name == name && m.ParamCount == count
                    && ((m.Attributes & MethodAttributes.Static) != 0) == isStatic).ToArray();
                if (matches.Length == 0) continue;
                if (matches.Length != 1) throw new NotSupportedException("Ambiguous method: " + type.Name + "." + name);
                return Runtime.Method(type, name, isStatic, matches[0].GetParameters().Select(p => p.Name).ToArray());
            }
            throw new MissingMethodException(type.Name, name);
        }

        public IntPtr Call(IL2CppClassWrapper type, IntPtr instance, string name, params Il2CppArgument[] args) =>
            Runtime.Invoke(Method(type, name, args.Length, instance == IntPtr.Zero), instance, args);
        public T Value<T>(IL2CppClassWrapper type, IntPtr instance, string name, params Il2CppArgument[] args) where T : unmanaged =>
            Runtime.InvokeValue<T>(Method(type, name, args.Length, instance == IntPtr.Zero), instance, args);
        public IL2CppClassWrapper ReturnType(IL2CppClassWrapper owner, string getter) =>
            Runtime.ClassFromType(Method(owner, getter, 0).ReturnType);
        public IntPtr New(IL2CppClassWrapper type)
        {
            var value = Runtime.NewObject(type);
            Call(type, value, ".ctor");
            return value;
        }

        public IntPtr[] References(IntPtr collection)
        {
            if (collection == IntPtr.Zero) return Array.Empty<IntPtr>();
            var type = Type("System.Collections", "ArrayList");
            var copy = Runtime.NewObject(type);
            Runtime.Invoke(Runtime.Method(type, ".ctor", false, "System.Collections.ICollection"), copy,
                Il2CppArgument.Ref(collection));
            return Runtime.ReferenceArray(Call(type, copy, "ToArray"));
        }

        public IntPtr[] FindObjects(IL2CppClassWrapper type)
        {
            var find = Runtime.Method(Type("UnityEngine", "Object"), "FindObjectsOfType", true, "System.Type");
            return Runtime.ReferenceArray(Runtime.Invoke(find, IntPtr.Zero,
                Il2CppArgument.Ref(Runtime.TypeObject(type))));
        }

        public IntPtr Bytes(byte[] bytes) => Runtime.Invoke(
            Runtime.Method(Type("System", "Convert"), "FromBase64String", true, "System.String"), IntPtr.Zero,
            Il2CppArgument.Ref(Runtime.String(Convert.ToBase64String(bytes))));

        public IntPtr Manager(IL2CppClassWrapper type)
        {
            var containerType = Type("CubeUnity.Infra.DependencyInjection", "DependencyContainer");
            var container = Call(containerType, IntPtr.Zero, "get_Instance");
            var field = Runtime.Field(containerType, "_instances");
            var instances = Runtime.ReadReference(container, field);
            var dictionaryType = Runtime.ClassFromType(field.Type);
            var key = Il2CppArgument.Ref(Runtime.TypeObject(type));
            if (Value<bool>(dictionaryType, instances, "ContainsKey", key))
            {
                var direct = Call(dictionaryType, instances, "get_Item", key);
                if (Runtime.IsInstance(direct, type)) return direct;
            }

            // Services may be registered under an interface rather than their concrete type.
            var values = Call(dictionaryType, instances, "get_Values");
            var valuesType = ReturnType(dictionaryType, "get_Values");
            var count = Value<int>(valuesType, values, "get_Count");
            var createArray = Runtime.Method(Type("System", "Array"), "CreateInstance", true, "System.Type", "System.Int32");
            var array = Runtime.Invoke(createArray, IntPtr.Zero,
                Il2CppArgument.Ref(Runtime.TypeObject(Type("System", "Object"))), Il2CppArgument.Value(count));
            var copy = Runtime.Method(valuesType, "CopyTo", false, "System.Object[]", "System.Int32");
            Runtime.Invoke(copy, values, Il2CppArgument.Ref(array), Il2CppArgument.Value(0));
            var matches = Runtime.ReferenceArray(array).Where(value => Runtime.IsInstance(value, type)).Distinct().ToArray();
            if (matches.Length == 1) return matches[0];
            throw new InvalidOperationException($"Expected one registered {type.Name}, found {matches.Length}. Wait for the home screen.");
        }

        public int EnumValue(IL2CppClassWrapper type, string name)
        {
            var field = type.GetFields().SingleOrDefault(f => f.Name == name && (f.Attributes & FieldAttributes.Literal) != 0);
            if (field == null) throw new NotSupportedException(type.Name + "." + name + " is unavailable.");
            return Convert.ToInt32(field.GetDefaultValue());
        }

        public Il2CppArgument Default(IL2CppTypeWrapper type)
        {
            var klass = Runtime.ClassFromType(type);
            return klass.IsValueType ? new Il2CppArgument { ValueBytes = new byte[checked((int)klass.ValueSize)] }
                : Il2CppArgument.Ref(IntPtr.Zero);
        }

        public void Dispose() => Runtime.Dispose();
    }
}
