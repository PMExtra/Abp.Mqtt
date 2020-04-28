using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.Core;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Xunit;

namespace Abp.Mqtt.Rpc.Test
{
    public class ProxyReflectionTest
    {
        public ProxyReflectionTest()
        {
            _container.Register(Component.For<LoggingInterceptor>());
            _container.Register(Component.For<IDemoInterface, DemoClass>().ImplementedBy<DemoClass>());
            _container.Register(Component.For<DemoClassWithoutInterface>());
        }

        public interface IDemoInterface
        {
            void HelloWorld();
        }

        [Interceptor(typeof(LoggingInterceptor))]
        public class DemoClass : IDemoInterface
        {
            public virtual void HelloWorld()
            {
                Console.WriteLine("Hello World!");
            }
        }

        [Interceptor(typeof(LoggingInterceptor))]
        public class DemoClassWithoutInterface
        {
            public virtual void HelloWorld()
            {
                Console.WriteLine("Hello World!");
            }
        }

        public class LoggingInterceptor : IInterceptor
        {
            public static List<string> Calls = new List<string>();

            public void Intercept(IInvocation invocation)
            {
                Calls.Add(invocation.Method.Name);
                invocation.Proceed();
            }
        }

        private readonly IWindsorContainer _container = new WindsorContainer();

        [Fact]
        public void DynamicProxyTest()
        {
            try
            {
                var demo = _container.Resolve<IDemoInterface>();
                Assert.Empty(LoggingInterceptor.Calls);
                demo.HelloWorld();
                Assert.Single(LoggingInterceptor.Calls);
                Assert.Equal(nameof(IDemoInterface.HelloWorld), LoggingInterceptor.Calls.Single());
                var type = demo.GetType();
                Assert.NotEqual(typeof(DemoClass), type);
                Assert.Equal(typeof(DemoClass), ProxyUtil.GetUnproxiedType(demo));
            }
            finally
            {
                LoggingInterceptor.Calls.Clear();
            }
        }

        [Fact]
        public void DynamicProxyWithoutInterfaceTest()
        {
            try
            {
                var demo = _container.Resolve<DemoClassWithoutInterface>();
                Assert.Empty(LoggingInterceptor.Calls);
                demo.HelloWorld();
                Assert.Single(LoggingInterceptor.Calls);
                Assert.Equal(nameof(DemoClassWithoutInterface.HelloWorld), LoggingInterceptor.Calls.Single());
                var type = demo.GetType();
                Assert.NotEqual(typeof(DemoClassWithoutInterface), type);
                Assert.Equal(typeof(DemoClassWithoutInterface), ProxyUtil.GetUnproxiedType(demo));
            }
            finally
            {
                LoggingInterceptor.Calls.Clear();
            }
        }

        [Fact]
        public void GetMethodsTest()
        {
            var demo = _container.Resolve<DemoClass>();
            var methods = ProxyUtil.GetUnproxiedType(demo)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.DeclaringType != typeof(object))
                .ToArray();
            Assert.Single(methods);
            Assert.Equal(nameof(DemoClass.HelloWorld), methods.Single().Name);
        }

        [Fact]
        public void GetMethodsWithoutInterfaceTest()
        {
            var demo = _container.Resolve<DemoClassWithoutInterface>();
            var methods = ProxyUtil.GetUnproxiedType(demo)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.DeclaringType != typeof(object))
                .ToArray();
            Assert.Single(methods);
            Assert.Equal(nameof(DemoClassWithoutInterface.HelloWorld), methods.Single().Name);
        }

        [Fact]
        public void ReflectCallDynamicProxyTest()
        {
            try
            {
                var method = typeof(DemoClass).GetMethod(nameof(DemoClass.HelloWorld));
                Assert.NotNull(method);

                var demo = _container.Resolve<DemoClass>();
                Assert.Empty(LoggingInterceptor.Calls);
                method.Invoke(demo, null);
                Assert.Single(LoggingInterceptor.Calls);
                Assert.Equal(nameof(DemoClass.HelloWorld), LoggingInterceptor.Calls.Single());
            }
            finally
            {
                LoggingInterceptor.Calls.Clear();
            }
        }

        [Fact]
        public void ReflectCallDynamicProxyWithoutInterfaceTest()
        {
            try
            {
                var method = typeof(DemoClassWithoutInterface).GetMethod(nameof(DemoClassWithoutInterface.HelloWorld));
                Assert.NotNull(method);

                var demo = _container.Resolve<DemoClassWithoutInterface>();
                Assert.Empty(LoggingInterceptor.Calls);
                method.Invoke(demo, null);
                Assert.Single(LoggingInterceptor.Calls);
                Assert.Equal(nameof(DemoClassWithoutInterface.HelloWorld), LoggingInterceptor.Calls.Single());
            }
            finally
            {
                LoggingInterceptor.Calls.Clear();
            }
        }
    }
}
