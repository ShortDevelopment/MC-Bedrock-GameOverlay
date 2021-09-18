using System;
using System.Runtime.InteropServices;
using FormsApplication = System.Windows.Forms.Application;
using Windows.UI.Popups;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using System.Dynamic;
using System.Reflection;
using System.Globalization;
using Microsoft.VisualBasic.CompilerServices;
using System.Linq;

namespace MC_Studio
{
    public class WinRTProxy
    {
        public WinRTProxy(CoreWebView2 webview)
        {
            this.WebView = webview;
        }

        public WinRTProxies.WindowsNSProxy Windows = new WinRTProxies.WindowsNSProxy();
        public ObjectProxy Process = new ObjectProxy(System.Diagnostics.Process.GetCurrentProcess());
        public CoreWebView2 WebView;
    }

    namespace WinRTProxies
    {
        public class WindowsNSProxy
        {
            public UINSProxy UI = new UINSProxy();

            public class UINSProxy
            {
                public PopupsNSProxy Popups = new PopupsNSProxy();

                public class PopupsNSProxy
                {
                    public ObjectProxy MessageDialog(string content)
                    {
                        MessageDialog instance = new MessageDialog(content);
                        ((IInitializeWithWindow)(object)instance).Initialize(FormsApplication.OpenForms[0].Handle);
                        return new ObjectProxy(instance);
                    }
                }
            }
        }
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDual)]
    public class NamespaceProxy : IReflect
    {
        public Assembly Assembly { get; private set; }
        public string Namespace { get; private set; }
        public NamespaceProxy(Assembly assembly, string @namespace)
        {
            this.Assembly = assembly;
            this.Namespace = @namespace;
        }

        public Type UnderlyingSystemType => typeof(Object);

        public FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new NotImplementedException();
        }

        public FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            return new[] { new FieldInfo("") };
        }

        public MemberInfo[] GetMember(string name, BindingFlags bindingAttr) => new MemberInfo[] { };

        public MemberInfo[] GetMembers(BindingFlags bindingAttr) => new MemberInfo[] { };

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr, Binder binder, Type[] types, ParameterModifier[] modifiers) => null;

        public MethodInfo GetMethod(string name, BindingFlags bindingAttr) => null;

        public MethodInfo[] GetMethods(BindingFlags bindingAttr) => new MethodInfo[] { };

        public PropertyInfo[] GetProperties(BindingFlags bindingAttr) => new PropertyInfo[] { };

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr) => null;

        public PropertyInfo GetProperty(string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers) => null;

        public object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new NotImplementedException();
        }

        private class NamespaceFieldInfo : FieldInfo
        {
            // public NamespaceFieldInfo

            public override RuntimeFieldHandle FieldHandle => throw new NotImplementedException();

            public override Type FieldType => typeof();

            public override FieldAttributes Attributes => throw new NotImplementedException();

            public override string Name => throw new NotImplementedException();

            public override Type DeclaringType => throw new NotImplementedException();

            public override Type ReflectedType => throw new NotImplementedException();

            public override object[] GetCustomAttributes(bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override object GetValue(object obj)
            {
                throw new NotImplementedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit)
            {
                throw new NotImplementedException();
            }

            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.AutoDual)]
    public class ObjectProxy : IReflect
    {

        public object BaseObject { get; private set; };

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
            if (retval.GetType().IsPrimitive || retval.GetType() == typeof(string))
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
