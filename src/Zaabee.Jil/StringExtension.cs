using System;
using Jil;

namespace Zaabee.Jil
{
    public static class StringExtension
    {
        public static T FromJson<T>(this string str, Options options = null) =>
            JilHelper.Deserialize<T>(str, options);

        public static object FromJson(this string str, Type type, Options options = null) =>
            JilHelper.Deserialize(type, str, options);
    }
}