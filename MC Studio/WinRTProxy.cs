using Microsoft.VisualBasic.CompilerServices;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Windows.UI.Popups;
using FormsApplication = System.Windows.Forms.Application;

namespace MC_Studio
{
    public class WinRTProxy
    {
        public WinRTProxy(CoreWebView2 webview)
        {
            this.WebView = webview;
        }

        public NamespaceProxy Windows = new NamespaceProxy("Windows");
        public ObjectProxy Process = new ObjectProxy(System.Diagnostics.Process.GetCurrentProcess());
        public CoreWebView2 WebView;
    }

    public class WinRTAssemblyManager
    {
        private static Dictionary<string, List<Type>> _Namespaces;
        public static string[] Namespaces
        {
            get
            {
                if (_Namespaces == null)
                {
                    _Namespaces = new Dictionary<string, List<Type>>();
                    foreach (Type type in typeof(MessageDialog).Assembly.ExportedTypes)
                    {
                        List<Type> types = new List<Type>();
                        if (_Namespaces.ContainsKey(type.Namespace))
                            types = _Namespaces[type.Namespace];
                        else
                            _Namespaces.Add(type.Namespace, types);
                        types.Add(type);
                    }
                }
                return _Namespaces.Keys.ToArray();
            }
        }

        public static Type[] GetTypesInNamespcae(string @namespace)
        {
            if (!_Namespaces.ContainsKey(@namespace))
                return new Type[] { };
            return _Namespaces[@namespace].ToArray();
        }
    }

    /// <summary>
    /// Exposes namespaces as nested objects for JS
    /// </summary>
    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDual)]
    public class NamespaceProxy : IReflect
    {
        public string Namespace { get; private set; }
        public NamespaceProxy(string @namespace)
        {
            this.Namespace = @namespace;
        }

        public Type UnderlyingSystemType => typeof(object);

        public FieldInfo GetField(string name, BindingFlags bindingAttr) => GetFields(bindingAttr).Where((x) => x.Name == name).FirstOrDefault();

        public FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            var fields = WinRTAssemblyManager.Namespaces.AsParallel().Where((x) => (x.Contains(".") ? x.Substring(0, x.LastIndexOf(".")) : x) == this.Namespace).Select((x) => new NamespaceFieldInfo(x.Split('.').Last())).ToArray();
            return fields;
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => GetMethod(name, bindingAttr);

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr) => GetMethods(bindingAttr).Where((x) => x.Name == name).FirstOrDefault();

        public MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return WinRTAssemblyManager.GetTypesInNamespcae(Namespace).Where((x) => x.GetConstructors().Length > 0).Select((x) => new ObjectConstructorMethodInfo(x, x.GetConstructors().First())).ToArray();
        }

        #region Not in use
        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => new MemberInfo[] { };

        public MemberInfo[] GetMembers(BindingFlags bindingAttr) => new MemberInfo[] { };

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr) => new PropertyInfo[] { };

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr) => null;

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => null;
        #endregion

        public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            if (invokeAttr.HasFlag(BindingFlags.GetField) || invokeAttr.HasFlag(BindingFlags.GetProperty))
            {
                FieldInfo field = GetField(name, invokeAttr);
                if (field != null)
                    return field.GetValue(this);
            }

            MethodInfo method = GetMethod(name, invokeAttr);
            if (method != null)
            {
                if (!invokeAttr.HasFlag(BindingFlags.InvokeMethod))
                    throw new MissingMethodException();
                return method.Invoke(this, args);
            }

            throw new ArgumentException();
        }

        private class NamespaceFieldInfo : FieldInfo
        {
            string _Name;
            public NamespaceFieldInfo(string name)
            {
                this._Name = name;
            }

            public override Type FieldType => typeof(NamespaceProxy);

            public override FieldAttributes Attributes => FieldAttributes.Public;

            public override string Name => _Name;

            public override Type DeclaringType => typeof(NamespaceProxy);

            public override Type ReflectedType => typeof(NamespaceProxy);

            public override object GetValue(object obj)
            {
                NamespaceProxy parent = obj as NamespaceProxy;
                return new NamespaceProxy($"{parent.Namespace}.{Name}");
            }

            #region Attributes

            public override object[] GetCustomAttributes(bool inherit) => new object[] { };

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => new object[] { };

            public override bool IsDefined(Type attributeType, bool inherit) => false;
            #endregion

            public override RuntimeFieldHandle FieldHandle => throw new NotImplementedException();

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
        private class ObjectConstructorMethodInfo : MethodInfo
        {
            public Type BaseType { get; private set; }
            public ConstructorInfo Constructor { get; private set; }

            public ObjectConstructorMethodInfo(Type baseType, ConstructorInfo constructor)
            {
                this.BaseType = baseType;
                this.Constructor = constructor;
            }

            public override MethodAttributes Attributes => MethodAttributes.Public;

            public override string Name => BaseType.Name;

            public override Type DeclaringType => typeof(NamespaceProxy);

            public override Type ReflectedType => typeof(NamespaceProxy);


            public override ParameterInfo[] GetParameters() => new ParameterInfo[] { };

            public override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture)
            {
                object instance = Activator.CreateInstance(BaseType, parameters);

                IInitializeWithWindow castedObject = instance as IInitializeWithWindow;
                if (castedObject != null)
                {
                    castedObject.Initialize(FormsApplication.OpenForms[0].Handle);
                }

                return new ObjectProxy(instance);
            }

            public override MethodImplAttributes GetMethodImplementationFlags() => MethodImplAttributes.Runtime;

            #region Attributes
            public override object[] GetCustomAttributes(bool inherit) => new object[] { };

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) => new object[] { };

            public override bool IsDefined(Type attributeType, bool inherit) => false;
            #endregion

            #region NotImplemented
            public override MethodInfo GetBaseDefinition()
            {
                throw new NotImplementedException();
            }

            public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotImplementedException();

            public override RuntimeMethodHandle MethodHandle => throw new NotImplementedException();
            #endregion
        }
    }

    /// <summary>
    /// Exposes members of an WinRT or .Net object via IDispatch COM
    /// </summary>
    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDual)]
    public class ObjectProxy : IReflect
    {

        public object BaseObject { get; private set; }

        public ObjectProxy(object obj)
        {
            if (obj == null)
                throw new ArgumentException();

            this.BaseObject = obj;
        }

        public Type UnderlyingSystemType => BaseObject.GetType();

        public FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetField(name, bindingAttr);
        }

        public FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetFields(bindingAttr);
        }

        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetMember(name, bindingAttr);
        }

        public MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetMembers(bindingAttr);
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers)
        {
            return BaseObject.GetType().GetMethod(name, bindingAttr, binder, types, modifiers);
        }

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetMethod(name, bindingAttr);
        }

        public MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetMethods(bindingAttr);
        }

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetProperties(bindingAttr);
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr)
        {
            return BaseObject.GetType().GetProperty(name, bindingAttr);
        }

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
        {
            return BaseObject.GetType().GetProperty(name, bindingAttr, binder, returnType, types, modifiers);
        }

        public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            object retval = null;
            try
            {
                retval = UnderlyingSystemType.InvokeMember(name, invokeAttr, binder, BaseObject, args, modifiers, culture, namedParameters);
            }
            catch (TargetInvocationException)
            {
                var member = UnderlyingSystemType.GetMember(name).FirstOrDefault();
                switch (member.MemberType)
                {
                    case MemberTypes.Method:
                        if (!invokeAttr.HasFlag(BindingFlags.InvokeMethod))
                            throw new MissingMethodException();
                        else
                            retval = NewLateBinding.LateCall(BaseObject, null, name, args, null, null, null, false);
                        break;
                    case MemberTypes.Property:
                        if (invokeAttr.HasFlag(BindingFlags.GetProperty))
                            retval = NewLateBinding.LateGet(BaseObject, null, name, args, null, null, null);
                        else
                            NewLateBinding.LateSet(BaseObject, null, name, args, null, null);
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            if (retval == null || retval.GetType().IsPrimitive || retval.GetType() == typeof(string))
                return retval;
            else
                return new ObjectProxy(retval);
        }
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("3E68D4BD-7135-4D10-8018-9FB6D9F33FA1")]
    internal interface IInitializeWithWindow
    {
        void Initialize(IntPtr hwnd);
    }
}
