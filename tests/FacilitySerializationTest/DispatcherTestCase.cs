namespace FacilitySerializationTest
{
	using System;
	using System.Collections;
	using Castle.Core;
	using Castle.Core.Internal;
	using Castle.Facilities.ZMQ.Internals;
	using Castle.MicroKernel;
	using Castle.MicroKernel.Registration;
	using Castle.MicroKernel.SubSystems.Configuration;
	using FluentAssertions;
	using NUnit.Framework;

	public interface IFakeService
	{
		void Operation();

		string OpWithStringReturn();

		string OpWithOverload(int x);
		string OpWithOverload(string x);
	}

	public class FakeServiceImpl : IFakeService
	{
		public void Operation()
		{
		}

		public string OpWithStringReturn()
		{
			return "test";
		}

		public string OpWithOverload(int x)
		{
			return "intoverload";
		}

		public string OpWithOverload(string x)
		{
			return "stringoverload";
		}
	}

	[TestFixture]
	public class DispatcherTestCase 
	{
		private Dispatcher _dispatcher;

		[SetUp]
		public void Init()
		{
			_dispatcher = new Dispatcher(new StubKernel(Resolver));
		}

		private object Resolver(Type arg)
		{
			return new FakeServiceImpl();
		}

		[Test]
		public void Invocation_with_no_param_nor_return()
		{
			var req = new RequestMessage(
				typeof(IFakeService).AssemblyQualifiedName, 
				"Operation", 
				new ParamTuple[0] { }, 
				null);

			var result = _dispatcher.Invoke(req.TargetService, req.TargetMethod, req.Params, req.ParamTypes);

			result.Should().NotBeNull();
			result.Item1.Should().BeNull();
			result.Item2.Should().Be(typeof(void));
		}

		[Test]
		public void Invocation_with_return_string()
		{
			var req = new RequestMessage(
				typeof(IFakeService).AssemblyQualifiedName,
				"OpWithStringReturn",
				new ParamTuple[0] { },
				null);

			var result = _dispatcher.Invoke(req.TargetService, req.TargetMethod, req.Params, req.ParamTypes);

			result.Should().NotBeNull();
			result.Item1.Should().Be("test");
			result.Item2.Should().Be(typeof(string));
		}

		[Test]
		public void Invocation_for_overload_op_with_int_param()
		{
			var req = new RequestMessage(
				typeof(IFakeService).AssemblyQualifiedName,
				"OpWithOverload",
				new ParamTuple[] { TransportSerialization.serialize_param(typeof(int), 1) },
				new string[] { typeof(int).AssemblyQualifiedName });

			var result = _dispatcher.Invoke(req.TargetService, req.TargetMethod, req.Params, req.ParamTypes);

			result.Should().NotBeNull();
			result.Item1.Should().Be("intoverload");
			result.Item2.Should().Be(typeof(string));
		}

		[Test]
		public void Invocation_for_overload_op_with_string_param()
		{
			var req = new RequestMessage(
				typeof(IFakeService).AssemblyQualifiedName,
				"OpWithOverload",
				new ParamTuple[] { TransportSerialization.serialize_param(typeof(string), "1") },
				new string[] { typeof(string).AssemblyQualifiedName });

			var result = _dispatcher.Invoke(req.TargetService, req.TargetMethod, req.Params, req.ParamTypes);

			result.Should().NotBeNull();
			result.Item1.Should().Be("stringoverload");
			result.Item2.Should().Be(typeof(string));
		}

		#region StubKernel

		class StubKernel : IKernel
		{
			private readonly Func<Type, object> _resolver;

			public StubKernel(Func<Type, object> resolver)
			{
				_resolver = resolver;
			}

			public event ComponentDataDelegate ComponentRegistered;
			public event ComponentModelDelegate ComponentModelCreated;
			public event EventHandler AddedAsChildKernel;
			public event EventHandler RemovedAsChildKernel;
			public event ComponentInstanceDelegate ComponentCreated;
			public event ComponentInstanceDelegate ComponentDestroyed;
			public event HandlerDelegate HandlerRegistered;
			public event HandlersChangedDelegate HandlersChanged;
			public event DependencyDelegate DependencyResolving;
			public event EventHandler RegistrationCompleted;
			public event ServiceDelegate EmptyCollectionResolving;

			public void Dispose()
			{
				throw new NotImplementedException();
			}

			public void AddChildKernel(IKernel kernel)
			{
				throw new NotImplementedException();
			}

			public IKernel AddFacility(IFacility facility)
			{
				throw new NotImplementedException();
			}

			public IKernel AddFacility<T>() where T : IFacility, new()
			{
				throw new NotImplementedException();
			}

			public IKernel AddFacility<T>(Action<T> onCreate) where T : IFacility, new()
			{
				throw new NotImplementedException();
			}

			public void AddHandlerSelector(IHandlerSelector selector)
			{
				throw new NotImplementedException();
			}

			public void AddHandlersFilter(IHandlersFilter filter)
			{
				throw new NotImplementedException();
			}

			public void AddSubSystem(string name, ISubSystem subsystem)
			{
				throw new NotImplementedException();
			}

			public IHandler[] GetAssignableHandlers(Type service)
			{
				throw new NotImplementedException();
			}

			public IFacility[] GetFacilities()
			{
				throw new NotImplementedException();
			}

			public IHandler GetHandler(string name)
			{
				throw new NotImplementedException();
			}

			public IHandler GetHandler(Type service)
			{
				throw new NotImplementedException();
			}

			public IHandler[] GetHandlers(Type service)
			{
				throw new NotImplementedException();
			}

			public ISubSystem GetSubSystem(string name)
			{
				throw new NotImplementedException();
			}

			public bool HasComponent(string name)
			{
				throw new NotImplementedException();
			}

			public bool HasComponent(Type service)
			{
				throw new NotImplementedException();
			}

			public IKernel Register(params IRegistration[] registrations)
			{
				throw new NotImplementedException();
			}

			public void ReleaseComponent(object instance)
			{
				throw new NotImplementedException();
			}

			public void RemoveChildKernel(IKernel kernel)
			{
				throw new NotImplementedException();
			}

			public void AddComponent(string key, Type classType)
			{
				throw new NotImplementedException();
			}

			public void AddComponent(string key, Type classType, LifestyleType lifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponent(string key, Type classType, LifestyleType lifestyle, bool overwriteLifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponent(string key, Type serviceType, Type classType)
			{
				throw new NotImplementedException();
			}

			public void AddComponent(string key, Type serviceType, Type classType, LifestyleType lifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponent(string key, Type serviceType, Type classType, LifestyleType lifestyle, bool overwriteLifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponent<T>()
			{
				throw new NotImplementedException();
			}

			public void AddComponent<T>(LifestyleType lifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponent<T>(LifestyleType lifestyle, bool overwriteLifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponent<T>(Type serviceType)
			{
				throw new NotImplementedException();
			}

			public void AddComponent<T>(Type serviceType, LifestyleType lifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponent<T>(Type serviceType, LifestyleType lifestyle, bool overwriteLifestyle)
			{
				throw new NotImplementedException();
			}

			public void AddComponentInstance<T>(object instance)
			{
				throw new NotImplementedException();
			}

			public void AddComponentInstance<T>(Type serviceType, object instance)
			{
				throw new NotImplementedException();
			}

			public void AddComponentInstance(string key, object instance)
			{
				throw new NotImplementedException();
			}

			public void AddComponentInstance(string key, Type serviceType, object instance)
			{
				throw new NotImplementedException();
			}

			public void AddComponentInstance(string key, Type serviceType, Type classType, object instance)
			{
				throw new NotImplementedException();
			}

			public void AddComponentWithExtendedProperties(string key, Type classType, IDictionary extendedProperties)
			{
				throw new NotImplementedException();
			}

			public void AddComponentWithExtendedProperties(string key, Type serviceType, Type classType, IDictionary extendedProperties)
			{
				throw new NotImplementedException();
			}

			public IKernel AddFacility(string key, IFacility facility)
			{
				throw new NotImplementedException();
			}

			public IKernel AddFacility<T>(string key) where T : IFacility, new()
			{
				throw new NotImplementedException();
			}

			public IKernel AddFacility<T>(string key, Action<T> onCreate) where T : IFacility, new()
			{
				throw new NotImplementedException();
			}

			public object Resolve(string key, object argumentsAsAnonymousType)
			{
				throw new NotImplementedException();
			}

			public object Resolve(string key, IDictionary arguments)
			{
				throw new NotImplementedException();
			}

			public object Resolve(Type service)
			{
				return this._resolver(service);
			}

			public object Resolve(Type service, IDictionary arguments)
			{
				throw new NotImplementedException();
			}

			public object Resolve(Type service, object argumentsAsAnonymousType)
			{
				throw new NotImplementedException();
			}

			public object Resolve(string key, Type service)
			{
				throw new NotImplementedException();
			}

			public T Resolve<T>(IDictionary arguments)
			{
				throw new NotImplementedException();
			}

			public T Resolve<T>(object argumentsAsAnonymousType)
			{
				throw new NotImplementedException();
			}

			public T Resolve<T>()
			{
				throw new NotImplementedException();
			}

			public T Resolve<T>(string key)
			{
				throw new NotImplementedException();
			}

			public T Resolve<T>(string key, IDictionary arguments)
			{
				throw new NotImplementedException();
			}

			public object Resolve(string key, Type service, IDictionary arguments)
			{
				throw new NotImplementedException();
			}

			public Array ResolveAll(Type service)
			{
				throw new NotImplementedException();
			}

			public Array ResolveAll(Type service, IDictionary arguments)
			{
				throw new NotImplementedException();
			}

			public Array ResolveAll(Type service, object argumentsAsAnonymousType)
			{
				throw new NotImplementedException();
			}

			public TService[] ResolveAll<TService>()
			{
				throw new NotImplementedException();
			}

			public TService[] ResolveAll<TService>(IDictionary arguments)
			{
				throw new NotImplementedException();
			}

			public TService[] ResolveAll<TService>(object argumentsAsAnonymousType)
			{
				throw new NotImplementedException();
			}

			public IComponentModelBuilder ComponentModelBuilder { get; private set; }
			public IConfigurationStore ConfigurationStore { get; set; }
			public GraphNode[] GraphNodes { get; private set; }
			public IHandlerFactory HandlerFactory { get; private set; }
			public IKernel Parent { get; set; }
			public IProxyFactory ProxyFactory { get; set; }
			public IReleasePolicy ReleasePolicy { get; set; }
			public IDependencyResolver Resolver { get; private set; }

			object IKernel.this[string key]
			{
				get { throw new NotImplementedException(); }
			}

			object IKernel.this[Type service]
			{
				get { throw new NotImplementedException(); }
			}
		}

		#endregion
	}
}
