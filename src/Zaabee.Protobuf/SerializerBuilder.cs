﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProtoBuf.Meta;

namespace Zaabee.Protobuf
{
    public static class SerializerBuilder
    {
        private const BindingFlags Flags = BindingFlags.FlattenHierarchy | BindingFlags.Public |
                                           BindingFlags.NonPublic | BindingFlags.Instance;

        private static readonly ConcurrentDictionary<Type, HashSet<Type>> SubTypes =
            new ConcurrentDictionary<Type, HashSet<Type>>();

        private static readonly ConcurrentBag<Type> BuiltTypes = new ConcurrentBag<Type>();
        private static readonly Type ObjectType = typeof(object);

        /// <summary>
        /// Build the ProtoBuf serializer from the generic <see cref="Type">type</see>.
        /// </summary>
        /// <typeparam name="T">The type of build the serializer for.</typeparam>
        public static void Build<T>(RuntimeTypeModel runtimeTypeModel)
        {
            var type = typeof(T);
            Build(runtimeTypeModel, type);
        }

        /// <summary>
        /// Build the ProtoBuf serializer for the <see cref="Type">type</see>.
        /// </summary>
        /// <param name="runtimeTypeModel"></param>
        /// <param name="type">The type of build the serializer for.</param>
        public static void Build(RuntimeTypeModel runtimeTypeModel, Type type)
        {
            if (BuiltTypes.Contains(type))
                return;

            lock (type)
            {
                if (BuiltTypes.Contains(type))
                    return;
                
                if (runtimeTypeModel.CanSerialize(type))
                {
                    if (type.IsGenericType)
                        BuildGenerics(runtimeTypeModel,type);
                    return;
                }

                var meta = runtimeTypeModel.Add(type, false);
                var fields = type.GetFields(Flags);

                meta.Add(fields.Select(m => m.Name).ToArray());
                meta.UseConstructor = false;

                BuildBaseClasses(runtimeTypeModel, type);
                BuildGenerics(runtimeTypeModel, type);

                foreach (var memberType in fields.Select(f => f.FieldType).Where(t => !t.IsPrimitive))
                    Build(runtimeTypeModel, memberType);

                BuiltTypes.Add(type);
            }
        }

        /// <summary>
        /// Builds the base class serializers for a type.
        /// </summary>
        /// <param name="runtimeTypeModel"></param>
        /// <param name="type">The type.</param>
        private static void BuildBaseClasses(RuntimeTypeModel runtimeTypeModel, Type type)
        {
            var baseType = type.BaseType;
            var inheritingType = type;

            while (baseType != null && baseType != ObjectType)
            {
                if (!SubTypes.TryGetValue(baseType, out var baseTypeEntry))
                    SubTypes.TryAdd(baseType, baseTypeEntry = new HashSet<Type>());

                if (!baseTypeEntry.Contains(inheritingType))
                {
                    Build(runtimeTypeModel, baseType);
                    runtimeTypeModel[baseType].AddSubType(baseTypeEntry.Count + 500, inheritingType);
                    baseTypeEntry.Add(inheritingType);
                }

                inheritingType = baseType;
                baseType = baseType.BaseType;
            }
        }

        /// <summary>
        /// Builds the serializers for the generic parameters for a given type.
        /// </summary>
        /// <param name="runtimeTypeModel"></param>
        /// <param name="type">The type.</param>
        private static void BuildGenerics(RuntimeTypeModel runtimeTypeModel, Type type)
        {
            if (!type.IsGenericType && (type.BaseType is null || !type.BaseType.IsGenericType)) return;
            var generics = type.IsGenericType ? type.GetGenericArguments() : type.BaseType.GetGenericArguments();

            foreach (var generic in generics)
                Build(runtimeTypeModel, generic);
        }
    }
}