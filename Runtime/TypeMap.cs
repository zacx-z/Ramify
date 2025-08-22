using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nela.Ramify {
    public static class TypeMap {
        private static Dictionary<Type, Type[]> interfaceImplementationMap = new Dictionary<Type, Type[]>();
        public static Type[] GetViewModelInterfaces(this Type type) {
            if (!interfaceImplementationMap.TryGetValue(type, out var res)) {
                res = FindImplementedInterfaces(type).ToArray();
                interfaceImplementationMap.Add(type, res);
            }

            return res;
        }
        
        private static IEnumerable<Type> FindImplementedInterfaces(Type type) {
            return type.GetInterfaces().Where(t => typeof(IViewModel).IsAssignableFrom(t))
                .SelectMany(WithCovariantTypes);
        }

        private static IEnumerable<Type> WithCovariantTypes(Type type) {
            yield return type;
            if (type.IsGenericType) {
                var gDef = type.GetGenericTypeDefinition();
                var parameters = gDef.GetGenericArguments();
                var count = 0;
                for (int i = 0; i < parameters.Length; i++) {
                    if ((parameters[i].GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0) {
                        count++;
                    }

                    if ((parameters[i].GenericParameterAttributes & GenericParameterAttributes.Contravariant) != 0) {
                        throw new InvalidOperationException("Can't support interfaces with contravariant parameters!");
                    }
                }

                var arguments = type.GetGenericArguments();
                if (count > 1) throw new InvalidOperationException("Can't support interfaces with more than 1 covariant parameter!");
                for (int i = 0; i < arguments.Length; i++) {
                    if ((parameters[i].GenericParameterAttributes & GenericParameterAttributes.Covariant) != 0) {
                        foreach (var @interface in arguments[i].GetViewModelInterfaces()) {
                            arguments[i] = @interface;
                            yield return gDef.MakeGenericType(arguments);
                        }

                        break;
                    }
                }
            }
        }
    }
}