using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class RpcManager
{
    private readonly List<Action<object[]>> _rpcMethods = new List<Action<object[]>>();
    private readonly Dictionary<int, Type> _indexToType = new Dictionary<int, Type>();

    public RpcManager()
    {
        RegisterAllRpcMethods();
    }

    private void RegisterAllRpcMethods()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes();

        int index = 0;

        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                              .Where(m => m.GetCustomAttribute<RpcMethodAttribute>() != null);

            foreach (var method in methods)
            {
                Action<object[]> action = parameters =>
                {
                    var instance = Activator.CreateInstance(type); // Or use a pre-existing instance
                    method.Invoke(instance, parameters);
                };

                _rpcMethods.Add(action);
                _indexToType[index] = type;
                index++;
            }
        }
    }

    public void InvokeRpc(int index, object[] parameters)
    {
        if (index >= 0 && index < _rpcMethods.Count)
        {
            _rpcMethods[index].Invoke(parameters);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Invalid RPC index.");
        }
    }

    public class RpcHandler
    {
        private readonly RpcManager _rpcManager;

        public RpcHandler()
        {
            _rpcManager = new RpcManager();
        }

        public void HandleMessage(int rpcIndex, object[] parameters)
        {
            UnityEngine.Debug.Log("wow much message handled");
            _rpcManager.InvokeRpc(rpcIndex, parameters);
        }
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class RpcMethodAttribute : Attribute
{
}